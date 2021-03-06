﻿/*
 * Copyright (c) 2014-2016 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP Core <https://github.com/GraphDefined/WWCP_Core>
 *
 * Licensed under the Affero GPL license, Version 3.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.gnu.org/licenses/agpl.html
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#region Usings

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;
using org.GraphDefined.Vanaheimr.Hermod.DNS;

#endregion

namespace org.GraphDefined.WWCP.Importer
{

    public delegate Task<HTTPResponse<T>> DownloadData<T>(DateTime LastRunTimestamp, String LastRunId, DNSClient DNSClient);


    /// <summary>
    /// Import data into the WWCP in-memory datastructures.
    /// </summary>
    /// <typeparam name="T">The type of data which will be processed on every update run.</typeparam>
    public class WWCPImporter<T>
    {

        #region Data

        private                 Boolean                                         Started = false;
        private readonly        Object                                          UpdateEVSEDataAndStatusLock;
        private readonly        Timer                                           UpdateEVSEStatusTimer;

        private readonly        DownloadData<T>                                 DownloadData;
        private readonly        Action<WWCPImporter<T>, Task<HTTPResponse<T>>>  OnFirstRun;
        private readonly        Action<WWCPImporter<T>, Task<HTTPResponse<T>>>  OnEveryRun;

        private readonly static TimeSpan                                        DefaultImportEvery  = TimeSpan.FromMinutes(1);

        public  const           UInt16                                          DefaultMaxNumberOfCachedXMLExports = 100;

        #endregion

        #region Properties

        #region Id

        private readonly String _Id;

        public String Id
        {
            get
            {
                return _Id;
            }
        }

        #endregion

        #region ConfigFilenamePrefix

        private readonly String _ConfigFilenamePrefix;

        public String ConfigFilenamePrefix
        {
            get
            {
                return _ConfigFilenamePrefix;
            }
        }

        #endregion

        #region DNSClient

        private readonly DNSClient _DNSClient;

        public DNSClient DNSClient
        {
            get
            {
                return _DNSClient;
            }
        }

        #endregion

        #region EVSEOperators

        private readonly List<EVSEOperator> _EVSEOperators;

        public IEnumerable<EVSEOperator> EVSEOperators
        {
            get
            {
                return _EVSEOperators;
            }
        }

        #endregion

        #region RoamingNetworkIds

        public IEnumerable<RoamingNetwork_Id> RoamingNetworkIds
        {
            get
            {
                return _EVSEOperators.Select(EVSEOperator => EVSEOperator.RoamingNetwork.Id);
            }
        }

        #endregion

        #region AllForwardingInfos

        private readonly Dictionary<ChargingStation_Id, ImporterForwardingInfo> _AllForwardingInfos;

        public IEnumerable<ImporterForwardingInfo> AllForwardingInfos
        {
            get
            {

                return _AllForwardingInfos.
                           OrderBy(AllForwardingInfos => AllForwardingInfos.Key).
                           Select (AllForwardingInfos => AllForwardingInfos.Value);

            }
        }

        #endregion

        #region XMLExports

        private List<Timestamped<T>> _ImportedData;

        public IEnumerable<Timestamped<T>> XMLExports
        {
            get
            {
                return _ImportedData;
            }
        }

        #endregion

        #region MaxNumberOfCachedXMLExports

        private UInt16 _MaxNumberOfCachedDataImports;

        public UInt16 MaxNumberOfCachedXMLExports
        {
            get
            {
                return _MaxNumberOfCachedDataImports;
            }
        }

        #endregion

        #region UpdateEvery

        private readonly TimeSpan _UpdateEvery;

        public TimeSpan UpdateEvery
        {
            get
            {
                return _UpdateEvery;
            }
        }

        #endregion

        #region HTTPImportEvents

        private HTTPEventSource _HTTPImportEvents;

        public HTTPEventSource HTTPImportEvents
        {

            get
            {
                return _HTTPImportEvents;
            }

            set
            {
                if (value != null)
                    _HTTPImportEvents = value;
            }

        }

        #endregion

        #endregion

        #region Events

        #region OnForwardingInformationAdded

        /// <summary>
        /// A delegate called whenever a new forwarding information was added.
        /// </summary>
        public delegate void OnForwardingInformationAddedDelegate(DateTime Timestamp, WWCPImporter<T> Sender, IEnumerable<ImporterForwardingInfo> ForwardingInfos);

        /// <summary>
        /// An event fired whenever a new forwarding information was added.
        /// </summary>
        public event OnForwardingInformationAddedDelegate OnForwardingInformationAdded;

        #endregion

        #region OnForwardingChanged

        /// <summary>
        /// A delegate called whenever a new forwarding information was changed.
        /// </summary>
        public delegate void OnForwardingChangedDelegate(DateTime Timestamp, WWCPImporter<T> Sender, ImporterForwardingInfo ForwardingInfo, RoamingNetwork_Id OldRN, RoamingNetwork_Id NewRN);

        /// <summary>
        /// An event fired whenever a new forwarding information was changed.
        /// </summary>
        public event OnForwardingChangedDelegate OnForwardingChanged;

        #endregion

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new WWCP importer.
        /// </summary>
        public WWCPImporter(String                                          Id,
                            String                                          ConfigFilenamePrefix,
                            DNSClient                                       DNSClient                    = null,
                            TimeSpan?                                       UpdateEvery                  = null,

                            DownloadData<T>                                 DownloadData                 = null,
                            Action<WWCPImporter<T>, Task<HTTPResponse<T>>>  OnFirstRun                   = null,
                            Action<WWCPImporter<T>, Task<HTTPResponse<T>>>  OnEveryRun                   = null,

                            UInt16                                          MaxNumberOfCachedXMLExports  = DefaultMaxNumberOfCachedXMLExports)

        {

            #region Initial checks

            if (Id.IsNullOrEmpty())
                throw new ArgumentNullException(nameof(Id),                    "The given config file name must not be null or empty!");

            if (ConfigFilenamePrefix.IsNullOrEmpty())
                throw new ArgumentNullException(nameof(ConfigFilenamePrefix),  "The given config file name must not be null or empty!");

            if (DownloadData == null)
                throw new ArgumentNullException(nameof(DownloadData),          "The given delegate must not be null or empty!");

            if (OnFirstRun == null)
                throw new ArgumentNullException(nameof(OnFirstRun),            "The given delegate must not be null or empty!");

            if (OnEveryRun == null)
                throw new ArgumentNullException(nameof(OnEveryRun),            "The given delegate must not be null or empty!");

            #endregion

            this._Id                            = Id;
            this._ConfigFilenamePrefix          = ConfigFilenamePrefix;
            this._DNSClient                     = DNSClient   != null ? DNSClient         : new DNSClient();
            this._UpdateEvery                   = UpdateEvery != null ? UpdateEvery.Value : DefaultImportEvery;

            this.DownloadData                   = DownloadData;
            this.OnFirstRun                     = OnFirstRun;
            this.OnEveryRun                     = OnEveryRun;

            this._MaxNumberOfCachedDataImports  = MaxNumberOfCachedXMLExports;

            this._EVSEOperators                 = new List<EVSEOperator>();
            this._AllForwardingInfos            = new Dictionary<ChargingStation_Id, ImporterForwardingInfo>();
            this._ImportedData                  = new List<Timestamped<T>>();

            // Start not now but veeeeery later!
            UpdateEVSEDataAndStatusLock         = new Object();
            UpdateEVSEStatusTimer               = new Timer(ImportEvery, null, TimeSpan.FromDays(30), _UpdateEvery);

        }

        #endregion



        //ToDo: Store on disc after OnChargingStationAdminStatusChanged!!!

        #region RegisterEVSEOperator(EVSEOperator)

        public WWCPImporter<T> RegisterEVSEOperator(EVSEOperator EVSEOperator)
        {

            _EVSEOperators.Add(EVSEOperator);

            EVSEOperator.OnChargingStationAdminStatusChanged += async (Timestamp, ChargingStation, OldStatus, NewStatus) => {

                var fwd = AllForwardingInfos.FirstOrDefault(fwdinfo => fwdinfo.StationId == ChargingStation.Id);
                if (fwd != null)
                {

                    fwd.AdminStatus = ChargingStation.AdminStatus;

                    //ToDo: Store on disc!

                }

            };

            return this;

        }

        #endregion

        #region LoadForwardingDataFromFile()

        public WWCPImporter<T> LoadForwardingDataFromFile()
        {

            lock (_EVSEOperators)
            {

                var CurrentDirectory  = Directory.GetCurrentDirectory();
                var ConfigFilename    = Directory.EnumerateFiles(CurrentDirectory).
                                                  Select           (file => file.Remove(0, CurrentDirectory.Length + 1)).
                                                  Where            (file => file.StartsWith(_ConfigFilenamePrefix)).
                                                  OrderByDescending(file => file).
                                                  FirstOrDefault();

                var InputFile         = ConfigFilename != null ? ConfigFilename : ConfigFilenamePrefix + ".json";

                if (File.Exists(InputFile))
                {

                    #region Try to read JSON from file...

                    JObject JSONConfig;

                    try
                    {
                        JSONConfig = JObject.Parse(File.ReadAllText(InputFile));
                    }
                    catch (Exception)
                    {
                        throw new ApplicationException("Could not read '" + InputFile + "'!");
                    }

                    #endregion

                    try
                    {

                        foreach (var CurrentRoamingNetwork in JSONConfig)
                        {

                            var CurrentRoamingNetworkId  = RoamingNetwork_Id.Parse(CurrentRoamingNetwork.Key);

                            var CurrenteVSEOperator      = _EVSEOperators.
                                                               Where(evseoperator => evseoperator.RoamingNetwork.Id == CurrentRoamingNetworkId).
                                                               FirstOrDefault();

                            if (CurrenteVSEOperator == null)
                                throw new ApplicationException("Could not find any EVSE operator for roaming network '" + CurrentRoamingNetworkId + "'!");

                            var CurrentRoamingNetworkJObject = CurrentRoamingNetwork.Value as JObject;

                            if (CurrentRoamingNetworkJObject != null)
                            {
                                foreach (var ChargingStationGroups in CurrentRoamingNetworkJObject)
                                {

                                    switch (ChargingStationGroups.Key.ToLower())
                                    {

                                        #region ValidChargingStations

                                        case "validchargingstations":

                                            (ChargingStationGroups.Value as JObject).GetEnumerator().
                                                ConsumeAll().
                                                OrderBy(KVP => KVP.Key).
                                                ForEach(StationConfig => {

                                                    ChargingStation_Id ChargingStationId = null;

                                                    if (ChargingStation_Id.TryParse(StationConfig.Key, out ChargingStationId))
                                                    {

                                                        JToken JSONToken2;
                                                        String PhoneNumber = null;
                                                        var CurrentSettings = StationConfig.Value as JObject;

                                                        #region PhoneNumber

                                                        if (CurrentSettings.TryGetValue("PhoneNumber", out JSONToken2))
                                                        {
                                                            PhoneNumber = JSONToken2.Value<String>();
                                                        }

                                                        #endregion

                                                        #region AdminStatus

                                                        var AdminStatus = ChargingStationAdminStatusType.Operational;

                                                        if (CurrentSettings.TryGetValue("Adminstatus", out JSONToken2))
                                                            if (!Enum.TryParse<ChargingStationAdminStatusType>(JSONToken2.Value<String>(), true, out AdminStatus))
                                                                DebugX.Log("Invalid admin status '" + JSONToken2.Value<String>() + "' for charging station '" + ChargingStationId.ToString() + "'!");

                                                        #endregion

                                                        #region Group

                                                        if (CurrentSettings.TryGetValue("Group", out JSONToken2))
                                                        {

                                                            var JV = JSONToken2 as JValue;
                                                            var JA = JSONToken2 as JArray;

                                                            var GroupList = JV != null
                                                                                ? new String[] { JV.Value<String>() }
                                                                                : JA != null
                                                                                    ? JA.AsEnumerable().Select(v => v.Value<String>())
                                                                                    : null;

                                                            if (GroupList != null)
                                                            {
                                                                foreach (var GroupId in GroupList)
                                                                {
                                                                    CurrenteVSEOperator.
                                                                        GetOrCreateChargingStationGroup(ChargingStationGroup_Id.Parse(CurrenteVSEOperator.Id, GroupId)).
                                                                        Add(ChargingStationId);
                                                                }
                                                            }

                                                        }

                                                        #endregion

                                                        if (!_AllForwardingInfos.ContainsKey(ChargingStationId))
                                                        {

                                                            _AllForwardingInfos.Add(ChargingStationId,
                                                                                    new ImporterForwardingInfo(
                                                                                        OnChangedCallback:       SendOnForwardingChanged,
                                                                                        EVSEOperators:           _EVSEOperators,
                                                                                        StationId:               ChargingStationId,
                                                                                        StationName:             "",
                                                                                        StationServiceTag:       "",
                                                                                        StationAddress:          new Address(),
                                                                                        StationGeoCoordinate:    null,
                                                                                        PhoneNumber:             PhoneNumber,
                                                                                        AdminStatus:             AdminStatus,
                                                                                        Created:                 DateTime.Now,
                                                                                        OutOfService:            true,
                                                                                        ForwardedToEVSEOperator: CurrenteVSEOperator)
                                                                                   );

                                                        }

                                                    }

                                                });

                                            break;

                                        #endregion

                                    }

                                }

                            }

                        }

                    }

                    catch (Exception e)
                    {
                        DebugX.Log("LoadForwardingDataFromFile failed: " + e.Message);
                    }

                }

                else
                    throw new ApplicationException("Config file '" + _ConfigFilenamePrefix + "' does not exist!");

            }

            return this;

        }

        #endregion

        #region SaveForwardingDataToFile()

        public WWCPImporter<T> SaveForwardingDataToFile()
        {

            lock (_EVSEOperators)
            {

                var Now             = DateTime.Now;

                var _ConfigFilename = String.Concat(_ConfigFilenamePrefix,     "_",
                                                    Now.Year,                  "-",
                                                    Now.Month. ToString("D2"), "-",
                                                    Now.Day.   ToString("D2"), "_",
                                                    Now.Hour.  ToString("D2"), "-",
                                                    Now.Minute.ToString("D2"), "-",
                                                    Now.Second.ToString("D2"),
                                                    ".json");

                try
                {

                    var JSON = new JObject(

                                   _AllForwardingInfos.

                                       GroupBy(kvp     => kvp.Value.ForwardedToRoamingNetworkId,
                                               kvp     => kvp.Value).

                                       Where  (RNGroup => RNGroup.Key.ToString() != "-").
                                       Select (RNGroup => new JProperty(RNGroup.Key.ToString(),
                                                                  new JObject(

                                                                      new JProperty("ValidChargingStations", new JObject(
                                                                          RNGroup.Select(FwdInfo =>
                                                                              new JProperty(FwdInfo.StationId.ToString(), new JObject(
                                                                                      new JProperty("PhoneNumber", FwdInfo.PhoneNumber),
                                                                                      new JProperty("AdminStatus", FwdInfo.AdminStatus.Value.ToString())
                                                                                  ))
                                                                          )
                                                                      ))

                                                                ))).
                                       ToArray()
                               );



                    //var JSON = new JObject(
                    //    RoamingNetworkIds.Select(RNId => new JProperty(RNId.ToString(),
                    //                                          new JObject(

                    //                                              new JProperty("ValidChargingStations", new JObject(
                    //                                                        EVSEOperators.
                    //                                                            Where(evseoperator => evseoperator.RoamingNetwork.Id == RNId).
                    //                                                            First().
                    //                                                            ValidEVSEIds.
                    //                                                            Select(EVSEId => new JProperty(EVSEId.ToString(), new JObject()))
                    //                                                  )),

                    //                                              new JProperty("ValidEVSEIds",   new JObject(
                    //                                                        EVSEOperators.
                    //                                                            Where(evseoperator => evseoperator.RoamingNetwork.Id == RNId).
                    //                                                            First().
                    //                                                            ValidEVSEIds.
                    //                                                            Select(EVSEId => new JProperty(EVSEId.ToString(), new JObject()))
                    //                                                  )),

                    //                                              new JProperty("InvalidEVSEIds", new JObject(
                    //                                                        EVSEOperators.
                    //                                                            Where(evseoperator => evseoperator.RoamingNetwork.Id == RNId).
                    //                                                            First().
                    //                                                            InvalidEVSEIds.
                    //                                                            Select(EVSEId => new JProperty(EVSEId.ToString(), new JObject()))
                    //                                                  ))

                    //                                          )))

                    //);

                    File.WriteAllText(_ConfigFilename, JSON.ToString(), Encoding.UTF8);

                }

                catch (Exception e)
                {
                    DebugX.Log("SaveForwardingDataInFile failed: " + e.Message);
                }

            }

            return this;

        }

        #endregion


        #region AddOrUpdateForwardingInfos(ForwardingInfos)

        public WWCPImporter<T> AddOrUpdateForwardingInfos(IEnumerable<ImporterForwardingInfo> GivenForwardingInfos)
        {

            lock (_AllForwardingInfos)
            {

                var NewForwardingInfos = new List<ImporterForwardingInfo>();

                GivenForwardingInfos.ForEach(GivenForwardingInfo => {

                    #region An existing forwarding information was found... search for changes...

                    ImporterForwardingInfo ExistingForwardingInfo;

                    if (_AllForwardingInfos.TryGetValue(GivenForwardingInfo.StationId, out ExistingForwardingInfo))
                    {

                        #region StationNameChanged...

                        if (ExistingForwardingInfo.StationName          != GivenForwardingInfo.StationName)
                        {

                            _HTTPImportEvents.SubmitSubEvent("StationNameChanged",
                                                             new JObject(
                                                                 new JProperty("Timestamp",          DateTime.Now.ToIso8601()),
                                                                 new JProperty("ChargingStationId",  ExistingForwardingInfo.StationId.ToString()),
                                                                 new JProperty("OldValue",           ExistingForwardingInfo.StationName),
                                                                 new JProperty("NewValue",           GivenForwardingInfo. StationName)
                                                             ).ToString().
                                                               Replace(Environment.NewLine, ""));

                            ExistingForwardingInfo.StationName = GivenForwardingInfo.StationName;

                        }

                        #endregion

                        #region StationServiceTag changed...

                        if (ExistingForwardingInfo.StationServiceTag    != GivenForwardingInfo.StationServiceTag)
                        {

                            _HTTPImportEvents.SubmitSubEvent("StationServiceTagChanged",
                                                             new JObject(
                                                                 new JProperty("Timestamp",          DateTime.Now.ToIso8601()),
                                                                 new JProperty("ChargingStationId",  ExistingForwardingInfo.StationId.ToString()),
                                                                 new JProperty("OldValue",           ExistingForwardingInfo.StationServiceTag),
                                                                 new JProperty("NewValue",           GivenForwardingInfo. StationServiceTag)
                                                             ).ToString().
                                                               Replace(Environment.NewLine, ""));

                            ExistingForwardingInfo.StationServiceTag = GivenForwardingInfo.StationServiceTag;

                        }

                        #endregion

                        #region StationAddress changed...

                        if (ExistingForwardingInfo.StationAddress       != GivenForwardingInfo.StationAddress)
                        {

                            _HTTPImportEvents.SubmitSubEvent("StationAddressChanged",
                                                             new JObject(
                                                                 new JProperty("Timestamp",          DateTime.Now.ToIso8601()),
                                                                 new JProperty("ChargingStationId",  ExistingForwardingInfo.StationId.ToString()),
                                                                 new JProperty("OldValue",           ExistingForwardingInfo.StationAddress.ToString()),
                                                                 new JProperty("NewValue",           GivenForwardingInfo. StationAddress.ToString())
                                                             ).ToString().
                                                               Replace(Environment.NewLine, ""));

                            ExistingForwardingInfo.StationAddress = GivenForwardingInfo.StationAddress;

                        }

                        #endregion

                        #region StationGeoCoordinate changed...

                        if (ExistingForwardingInfo.StationGeoCoordinate != GivenForwardingInfo.StationGeoCoordinate)
                        {

                            _HTTPImportEvents.SubmitSubEvent("StationGeoCoordinateChanged",
                                                             new JObject(
                                                                 new JProperty("Timestamp",          DateTime.Now.ToIso8601()),
                                                                 new JProperty("ChargingStationId",  ExistingForwardingInfo.StationId.ToString()),
                                                                 new JProperty("OldValue",           ExistingForwardingInfo.StationGeoCoordinate.ToString()),
                                                                 new JProperty("NewValue",           GivenForwardingInfo. StationGeoCoordinate.ToString())
                                                             ).ToString().
                                                               Replace(Environment.NewLine, ""));

                            ExistingForwardingInfo.StationGeoCoordinate = GivenForwardingInfo.StationGeoCoordinate;

                        }

                        #endregion

                        ExistingForwardingInfo.UpdateTimestamp();

                    }

                    #endregion

                    #region ...or a new one was created.

                    else
                    {

                        NewForwardingInfos.Add(GivenForwardingInfo);

                        _AllForwardingInfos.Add(GivenForwardingInfo.StationId, GivenForwardingInfo);

                    }

                    #endregion

                });

                if (NewForwardingInfos.Any())
                {

                    //ToDo: Implement me!
                    _HTTPImportEvents.SubmitSubEvent("NewForwardingInfos",
                                                     new JObject(
                                                         new JProperty("Timestamp",        DateTime.Now.ToIso8601()),
                                                         new JProperty("ForwardingInfos",  new JArray())
                                                     ).ToString().
                                                       Replace(Environment.NewLine, ""));

                    var OnForwardingInformationAddedLocal = OnForwardingInformationAdded;
                    if (OnForwardingInformationAddedLocal != null)
                        OnForwardingInformationAddedLocal(DateTime.Now, this, NewForwardingInfos);

                }

            }

            return this;

        }

        #endregion

        #region Start()

        /// <summary>
        /// Start the WWCP importer.
        /// </summary>
        public WWCPImporter<T> Start()
        {

            if (Monitor.TryEnter(UpdateEVSEDataAndStatusLock))
            {

                try
                {

                    if (!Started)
                    {

                        OnFirstRun(this, DownloadData(DateTime.Now, "1", _DNSClient));

                        DebugX.Log("WWCP importer '" + Id + "' Initital import finished!");

                        UpdateEVSEStatusTimer.Change(TimeSpan.FromSeconds(1), UpdateEvery);

                        Started = true;

                    }

                }
                catch (Exception e)
                {
                    DebugX.Log("Starting the WWCP Importer '" + Id + "' led to an exception: " + e.Message + Environment.NewLine + e.StackTrace);
                }

                finally
                {
                    Monitor.Exit(UpdateEVSEDataAndStatusLock);
                }

            }

            SaveForwardingDataToFile();
            return this;

        }

        #endregion


        #region SendOnForwardingChanged(Timestamp, ForwardingInfo, OldRN, NewRN)

        public void SendOnForwardingChanged(DateTime                Timestamp,
                                            ImporterForwardingInfo  ForwardingInfo,
                                            RoamingNetwork_Id       OldRN,
                                            RoamingNetwork_Id       NewRN)
        {

            SaveForwardingDataToFile();

            var OnForwardingChangedLocal = OnForwardingChanged;
            if (OnForwardingChangedLocal != null)
                OnForwardingChangedLocal(Timestamp, this, ForwardingInfo, OldRN, NewRN);

        }

        #endregion


        #region (private, Timer) ImportEvery(Status)

        private void ImportEvery(Object Status)
        {

            Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;

            if (Monitor.TryEnter(UpdateEVSEDataAndStatusLock))
            {

                #region Debug info

                #if DEBUG

             //   DebugX.LogT("WWCP importer '" + Id + "' started!");

                var StopWatch = new Stopwatch();
                StopWatch.Start();

                #endif

                #endregion

                try
                {

                    #region Remove ForwardingInfos older than 7 days...

                    //var Now       = DateTime.Now;

                    //var ToRemove  = _AllForwardingInfos.
                    //                    Where (ForwardingInfo => ForwardingInfo.Value.LastTimeSeen + TimeSpan.FromDays(7) < Now).
                    //                    Select(ForwardingInfo => ForwardingInfo.Key).
                    //                    ToList();

                    //ToRemove.ForEach(ForwardingInfo => _AllForwardingInfos.Remove(ForwardingInfo));

                    #endregion

                    DownloadData(DateTime.Now, "1", _DNSClient).
                        ContinueWith(ImporterTask => {

                        // Save the imported data for later review...
                        _ImportedData.Add(new Timestamped<T>(ImporterTask.Result.Content));

                        if (_ImportedData.Count > _MaxNumberOfCachedDataImports)
                            _ImportedData.Remove(_ImportedData.First());

                        // Mark ForwardingInfos as 'OutOfService', to detect which are no longer within the XML...
                        lock (_AllForwardingInfos)
                        {
                            _AllForwardingInfos.Values.ForEach(FwdInfo => FwdInfo.OutOfService = true);
                        }

                        // Update ForwardingInfos
                        var OnEveryRunLocal = OnEveryRun;
                        if (OnEveryRunLocal != null)
                            OnEveryRunLocal(this, ImporterTask);

                    }).
                    Wait();

                    #region Debug info

                    #if DEBUG

                        StopWatch.Stop();

           //             DebugX.LogT("WWCP importer '" + Id + "' finished after " + StopWatch.Elapsed.TotalSeconds + " seconds!");

                    #endif

                    #endregion

                }
                catch (Exception e)
                {

                    while (e.InnerException != null)
                        e = e.InnerException;

                    DebugX.LogT("WWCP importer '" + Id + "' led to an exception: " + e.Message + Environment.NewLine + e.StackTrace);

                }

                finally
                {
                    Monitor.Exit(UpdateEVSEDataAndStatusLock);
                }

            }

          //  else
          //      DebugX.LogT("WWCP importer '" + Id + "' skipped!");

        }

        #endregion


        #region (override) ToString()

        /// <summary>
        /// Return a string representation of this object.
        /// </summary>
        public override String ToString()
        {
            return String.Concat("WWCP importer '", Id, "': ", _AllForwardingInfos.Count, " forwarding infos");
        }

        #endregion

    }

}
