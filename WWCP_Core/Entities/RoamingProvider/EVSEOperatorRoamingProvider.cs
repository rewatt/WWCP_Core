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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace org.GraphDefined.WWCP
{

    /// <summary>
    /// A EVSE operator roaming provider.
    /// </summary>
    public class EVSEOperatorRoamingProvider : ARoamingProvider,
                                               IOperatorRoamingService
    {

        #region Data

        private readonly        Object                  ServiceCheckLock;
        private readonly        Timer                   ServiceCheckTimer;
        public  readonly static TimeSpan                DefaultServiceCheckEvery = TimeSpan.FromSeconds(5);

        private readonly        HashSet<EVSE>           EVSEsToAddQueue;
        private readonly        HashSet<EVSE>           EVSEDataUpdatesQueue;
        private readonly        List<EVSEStatusChange>  EVSEStatusChangesQueue;
        private readonly        HashSet<EVSE>           EVSEsToRemoveQueue;

        #endregion

        #region Properties

        #region OperatorRoamingService

        private readonly IOperatorRoamingService _OperatorRoamingService;

        public IOperatorRoamingService OperatorRoamingService
        {
            get
            {
                return _OperatorRoamingService;
            }
        }

        #endregion


        #region RunId

        private UInt64 _RunId;

        public UInt64 RunId
        {
            get
            {
                return _RunId;
            }
        }

        #endregion



        public Func<EVSE, Boolean> IncludeEVSEs;

        #region DisableAutoUploads

        private volatile Boolean _DisableAutoUploads;

        public Boolean DisableAutoUploads
        {

            get
            {
                return _DisableAutoUploads;
            }

            set
            {
                _DisableAutoUploads = value;
            }

        }

        #endregion

        #region ServiceCheckEvery

        private UInt32 _ServiceCheckEvery;

        public TimeSpan ServiceCheckEvery {

            get
            {
                return TimeSpan.FromSeconds(_ServiceCheckEvery);
            }

            set
            {
                _ServiceCheckEvery = (UInt32) value.TotalSeconds;
            }

        }

        #endregion

        #endregion

        #region Events

        // Client methods (logging)

        #region OnEVSEDataPush/-Pushed

        /// <summary>
        /// An event fired whenever new EVSE data will be send upstream.
        /// </summary>
        public event OnEVSEDataPushDelegate OnEVSEDataPush
        {

            add
            {
                _OperatorRoamingService.OnEVSEDataPush += value;
            }

            remove
            {
                _OperatorRoamingService.OnEVSEDataPush -= value;
            }

        }

        /// <summary>
        /// An event fired whenever new EVSE data had been sent upstream.
        /// </summary>
        public event OnEVSEDataPushedDelegate OnEVSEDataPushed
        {

            add
            {
                _OperatorRoamingService.OnEVSEDataPushed += value;
            }

            remove
            {
                _OperatorRoamingService.OnEVSEDataPushed -= value;
            }

        }

        #endregion

        #region OnEVSEStatusPush/-Pushed

        /// <summary>
        /// An event fired whenever new EVSE status will be send upstream.
        /// </summary>
        public event OnEVSEStatusPushDelegate OnEVSEStatusPush
        {

            add
            {
                _OperatorRoamingService.OnEVSEStatusPush += value;
            }

            remove
            {
                _OperatorRoamingService.OnEVSEStatusPush -= value;
            }

        }

        /// <summary>
        /// An event fired whenever new EVSE status had been sent upstream.
        /// </summary>
        public event OnEVSEStatusPushedDelegate OnEVSEStatusPushed
        {

            add
            {
                _OperatorRoamingService.OnEVSEStatusPushed += value;
            }

            remove
            {
                _OperatorRoamingService.OnEVSEStatusPushed -= value;
            }

        }

        #endregion

        #region OnAuthorizeStart/-Started

        /// <summary>
        /// An event fired whenever an authentication token will be verified for charging.
        /// </summary>
        public event OnAuthorizeStartDelegate OnAuthorizeStart
        {

            add
            {
                _OperatorRoamingService.OnAuthorizeStart += value;
            }

            remove
            {
                _OperatorRoamingService.OnAuthorizeStart -= value;
            }

        }

        /// <summary>
        /// An event fired whenever an authentication token had been verified for charging.
        /// </summary>
        public event OnAuthorizeStartedDelegate OnAuthorizeStarted
        {

            add
            {
                _OperatorRoamingService.OnAuthorizeStarted += value;
            }

            remove
            {
                _OperatorRoamingService.OnAuthorizeStarted -= value;
            }

        }

        /// <summary>
        /// An event fired whenever an authentication token will be verified for charging at the given EVSE.
        /// </summary>
        public event OnAuthorizeEVSEStartDelegate OnAuthorizeEVSEStart
        {

            add
            {
                _OperatorRoamingService.OnAuthorizeEVSEStart += value;
            }

            remove
            {
                _OperatorRoamingService.OnAuthorizeEVSEStart -= value;
            }

        }

        /// <summary>
        /// An event fired whenever an authentication token had been verified for charging at the given EVSE.
        /// </summary>
        public event OnAuthorizeEVSEStartedDelegate OnAuthorizeEVSEStarted
        {

            add
            {
                _OperatorRoamingService.OnAuthorizeEVSEStarted += value;
            }

            remove
            {
                _OperatorRoamingService.OnAuthorizeEVSEStarted -= value;
            }

        }

        /// <summary>
        /// An event fired whenever an authentication token will be verified for charging at the given charging station.
        /// </summary>
        public event OnAuthorizeChargingStationStartDelegate OnAuthorizeChargingStationStart
        {

            add
            {
                _OperatorRoamingService.OnAuthorizeChargingStationStart += value;
            }

            remove
            {
                _OperatorRoamingService.OnAuthorizeChargingStationStart -= value;
            }

        }

        /// <summary>
        /// An event fired whenever an authentication token had been verified for charging at the given charging station.
        /// </summary>
        public event OnAuthorizeChargingStationStartedDelegate OnAuthorizeChargingStationStarted
        {

            add
            {
                _OperatorRoamingService.OnAuthorizeChargingStationStarted += value;
            }

            remove
            {
                _OperatorRoamingService.OnAuthorizeChargingStationStarted -= value;
            }

        }

        #endregion

        #region OnAuthorizeStop/-Stopped

        /// <summary>
        /// An event fired whenever an authentication token will be verified to stop a charging process.
        /// </summary>
        public event OnAuthorizeStopDelegate OnAuthorizeStop
        {

            add
            {
                _OperatorRoamingService.OnAuthorizeStop += value;
            }

            remove
            {
                _OperatorRoamingService.OnAuthorizeStop -= value;
            }

        }

        /// <summary>
        /// An event fired whenever an authentication token had been verified to stop a charging process.
        /// </summary>
        public event OnAuthorizeStoppedDelegate OnAuthorizeStopped
        {

            add
            {
                _OperatorRoamingService.OnAuthorizeStopped += value;
            }

            remove
            {
                _OperatorRoamingService.OnAuthorizeStopped -= value;
            }

        }

        /// <summary>
        /// An event fired whenever an authentication token will be verified to stop a charging process at the given EVSE.
        /// </summary>
        public event OnAuthorizeEVSEStopDelegate OnAuthorizeEVSEStop
        {

            add
            {
                _OperatorRoamingService.OnAuthorizeEVSEStop += value;
            }

            remove
            {
                _OperatorRoamingService.OnAuthorizeEVSEStop -= value;
            }

        }

        /// <summary>
        /// An event fired whenever an authentication token had been verified to stop a charging process at the given EVSE.
        /// </summary>
        public event OnAuthorizeEVSEStoppedDelegate OnAuthorizeEVSEStopped
        {

            add
            {
                _OperatorRoamingService.OnAuthorizeEVSEStopped += value;
            }

            remove
            {
                _OperatorRoamingService.OnAuthorizeEVSEStopped -= value;
            }

        }

        /// <summary>
        /// An event fired whenever an authentication token will be verified to stop a charging process at the given charging station.
        /// </summary>
        public event OnAuthorizeChargingStationStopDelegate OnAuthorizeChargingStationStop
        {

            add
            {
                _OperatorRoamingService.OnAuthorizeChargingStationStop += value;
            }

            remove
            {
                _OperatorRoamingService.OnAuthorizeChargingStationStop -= value;
            }

        }

        /// <summary>
        /// An event fired whenever an authentication token had been verified to stop a charging process at the given charging station.
        /// </summary>
        public event OnAuthorizeChargingStationStoppedDelegate OnAuthorizeChargingStationStopped
        {

            add
            {
                _OperatorRoamingService.OnAuthorizeChargingStationStopped += value;
            }

            remove
            {
                _OperatorRoamingService.OnAuthorizeChargingStationStopped -= value;
            }

        }

        #endregion

        #region OnChargeDetailRecordSend/-Sent

        /// <summary>
        /// An event fired whenever a charge detail record will be send.
        /// </summary>
        public event OnChargeDetailRecordSendDelegate OnChargeDetailRecordSend
        {

            add
            {
                _OperatorRoamingService.OnChargeDetailRecordSend += value;
            }

            remove
            {
                _OperatorRoamingService.OnChargeDetailRecordSend -= value;
            }

        }

        /// <summary>
        /// An event fired whenever a charge detail record had been sent.
        /// </summary>
        public event OnChargeDetailRecordSentDelegate OnChargeDetailRecordSent
        {

            add
            {
                _OperatorRoamingService.OnChargeDetailRecordSent += value;
            }

            remove
            {
                _OperatorRoamingService.OnChargeDetailRecordSent -= value;
            }

        }

        #endregion


        // Server methods

        #region OnRemoteStart/-Stop

        /// <summary>
        /// An event sent whenever a remote start command was received.
        /// </summary>
        public event OnRemoteStartEVSEDelegate OnRemoteStart
        {

            add
            {
                _OperatorRoamingService.OnRemoteStart += value;
            }

            remove
            {
                _OperatorRoamingService.OnRemoteStart -= value;
            }

        }

        /// <summary>
        /// An event sent whenever a remote stop command was received.
        /// </summary>
        public event OnRemoteStopEVSEDelegate OnRemoteStop
        {

            add
            {
                _OperatorRoamingService.OnRemoteStop += value;
            }

            remove
            {
                _OperatorRoamingService.OnRemoteStop -= value;
            }

        }

        #endregion

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create an EVSE operator roaming provider.
        /// </summary>
        /// <param name="Id">The unique identification of the roaming provider.</param>
        /// <param name="Name">The offical (multi-language) name of the roaming provider.</param>
        /// <param name="RoamingNetwork">The associated roaming network.</param>
        /// <param name="OperatorRoamingService">The attached local or remote EVSE operator roaming service.</param>
        public EVSEOperatorRoamingProvider(RoamingProvider_Id       Id,
                                           I18NString               Name,
                                           RoamingNetwork           RoamingNetwork,
                                           IOperatorRoamingService  OperatorRoamingService,
                                           Func<EVSE, Boolean>      IncludeEVSEs        = null,
                                           TimeSpan?                ServiceCheckEvery   = null,
                                           Boolean                  DisableAutoUploads  = false)

            : base(Id, Name, RoamingNetwork)

        {

            #region Initial Checks

            if (OperatorRoamingService == null)
                throw new ArgumentNullException(nameof(OperatorRoamingService),  "The given EVSE operator roaming service must not be null!");

            #endregion


            this._OperatorRoamingService  = OperatorRoamingService;

            this.EVSEsToAddQueue          = new HashSet<EVSE>();
            this.EVSEDataUpdatesQueue     = new HashSet<EVSE>();
            this.EVSEStatusChangesQueue   = new List<EVSEStatusChange>();
            this.EVSEsToRemoveQueue       = new HashSet<EVSE>();


            #region Link RemoteStart/-Stop to the roaming network

            this.OnRemoteStart += (Timestamp,
                                   CancellationToken,
                                   EventTrackingId,
                                   EVSEId,
                                   ChargingProductId,
                                   ReservationId,
                                   SessionId,
                                   ProviderId,
                                   eMAId,
                                   QueryTimeout) => RoamingNetwork.RemoteStart(Timestamp,
                                                                               CancellationToken,
                                                                               EventTrackingId,
                                                                               EVSEId,
                                                                               ChargingProductId,
                                                                               ReservationId,
                                                                               SessionId,
                                                                               ProviderId,
                                                                               eMAId,
                                                                               QueryTimeout);

            this.OnRemoteStop += (Timestamp,
                                  CancellationToken,
                                  EventTrackingId,
                                  ReservationHandling,
                                  SessionId,
                                  ProviderId,
                                  eMAId,
                                  EVSEId,
                                  QueryTimeout) => RoamingNetwork.RemoteStop(Timestamp,
                                                                             CancellationToken,
                                                                             EventTrackingId,
                                                                             EVSEId,
                                                                             SessionId,
                                                                             ReservationHandling.Close,
                                                                             ProviderId,
                                                                             eMAId,
                                                                             QueryTimeout);

            #endregion


            this.IncludeEVSEs             = IncludeEVSEs;

            this._ServiceCheckEvery       = (UInt32) (ServiceCheckEvery.HasValue
                                                          ? ServiceCheckEvery.Value.TotalMilliseconds
                                                          : DefaultServiceCheckEvery.TotalMilliseconds);

            this.ServiceCheckLock         = new Object();
            this.ServiceCheckTimer        = new Timer(ServiceCheck, null, 0, _ServiceCheckEvery);

            this._DisableAutoUploads      = DisableAutoUploads;

        }

        #endregion


        #region (timer) ServiceCheck(State)

        private void ServiceCheck(Object State)
        {

            if (!_DisableAutoUploads)
            {

                FlushQueues().Wait();

                //ToDo: Handle errors!

            }

        }

        #endregion


        #region PushEVSEData...

        #region PushEVSEData(GroupedEVSEs,     ActionType = fullLoad, OperatorId = null, OperatorName = null,                      QueryTimeout = null)

        /// <summary>
        /// Upload the EVSE data of the given lookup of EVSEs grouped by their EVSE operator.
        /// </summary>
        /// <param name="GroupedEVSEs">A lookup of EVSEs grouped by their EVSE operator.</param>
        /// <param name="ActionType">The server-side data management operation.</param>
        /// <param name="OperatorId">An optional unique identification of the EVSE operator.</param>
        /// <param name="OperatorName">The optional name of the EVSE operator.</param>
        /// <param name="QueryTimeout">An optional timeout of the HTTP client [default 60 sec.]</param>
        public async Task<Acknowledgement>

            PushEVSEData(ILookup<EVSEOperator, EVSE>  GroupedEVSEs,
                         ActionType                   ActionType    = WWCP.ActionType.fullLoad,
                         EVSEOperator_Id              OperatorId    = null,
                         String                       OperatorName  = null,
                         TimeSpan?                    QueryTimeout  = null)

        {

            return await _OperatorRoamingService.PushEVSEData(GroupedEVSEs,
                                                              ActionType,
                                                              OperatorId,
                                                              OperatorName,
                                                              QueryTimeout);

        }

        #endregion

        #region PushEVSEData(EVSE,             ActionType = insert,   OperatorId = null, OperatorName = null,                      QueryTimeout = null)

        /// <summary>
        /// Upload the EVSE data of the given EVSE.
        /// </summary>
        /// <param name="EVSE">An EVSE.</param>
        /// <param name="ActionType">The server-side data management operation.</param>
        /// <param name="OperatorId">An optional unique identification of the EVSE operator.</param>
        /// <param name="OperatorName">The optional name of the EVSE operator.</param>
        /// <param name="QueryTimeout">An optional timeout of the HTTP client [default 60 sec.]</param>
        public async Task<Acknowledgement>

            PushEVSEData(EVSE             EVSE,
                         ActionType       ActionType    = WWCP.ActionType.insert,
                         EVSEOperator_Id  OperatorId    = null,
                         String           OperatorName  = null,
                         TimeSpan?        QueryTimeout  = null)

        {

            return await _OperatorRoamingService.PushEVSEData(EVSE,
                                                              ActionType,
                                                              OperatorId,
                                                              OperatorName,
                                                              QueryTimeout);

        }

        #endregion

        #region PushEVSEData(EVSEs,            ActionType = fullLoad, OperatorId = null, OperatorName = null, IncludeEVSEs = null, QueryTimeout = null)

        /// <summary>
        /// Upload the EVSE data of the given enumeration of EVSEs.
        /// </summary>
        /// <param name="EVSEs">An enumeration of EVSEs.</param>
        /// <param name="ActionType">The server-side data management operation.</param>
        /// <param name="OperatorId">An optional unique identification of the EVSE operator.</param>
        /// <param name="OperatorName">The optional name of the EVSE operator.</param>
        /// <param name="IncludeEVSEs">Only upload the EVSEs returned by the given filter delegate.</param>
        /// <param name="QueryTimeout">An optional timeout of the HTTP client [default 60 sec.]</param>
        public async Task<Acknowledgement>

            PushEVSEData(IEnumerable<EVSE>    EVSEs,
                         ActionType           ActionType    = WWCP.ActionType.fullLoad,
                         EVSEOperator_Id      OperatorId    = null,
                         String               OperatorName  = null,
                         Func<EVSE, Boolean>  IncludeEVSEs  = null,
                         TimeSpan?            QueryTimeout  = null)

        {

            return await _OperatorRoamingService.PushEVSEData(EVSEs,
                                                              ActionType,
                                                              OperatorId,
                                                              OperatorName,
                                                              IncludeEVSEs,
                                                              QueryTimeout);

        }

        #endregion

        #region PushEVSEData(ChargingStation,  ActionType = fullLoad, OperatorId = null, OperatorName = null, IncludeEVSEs = null, QueryTimeout = null)

        /// <summary>
        /// Upload the EVSE data of the given charging station.
        /// </summary>
        /// <param name="ChargingStation">A charging station.</param>
        /// <param name="ActionType">The server-side data management operation.</param>
        /// <param name="OperatorId">An optional unique identification of the EVSE operator.</param>
        /// <param name="OperatorName">The optional name of the EVSE operator.</param>
        /// <param name="IncludeEVSEs">Only upload the EVSEs returned by the given filter delegate.</param>
        /// <param name="QueryTimeout">An optional timeout of the HTTP client [default 60 sec.]</param>
        public async Task<Acknowledgement>

            PushEVSEData(ChargingStation      ChargingStation,
                         ActionType           ActionType    = WWCP.ActionType.fullLoad,
                         EVSEOperator_Id      OperatorId    = null,
                         String               OperatorName  = null,
                         Func<EVSE, Boolean>  IncludeEVSEs  = null,
                         TimeSpan?            QueryTimeout  = null)

        {

            return await _OperatorRoamingService.PushEVSEData(ChargingStation,
                                                              ActionType,
                                                              OperatorId,
                                                              OperatorName,
                                                              IncludeEVSEs,
                                                              QueryTimeout);

        }

        #endregion

        #region PushEVSEData(ChargingStations, ActionType = fullLoad, OperatorId = null, OperatorName = null, IncludeEVSEs = null, QueryTimeout = null)

        /// <summary>
        /// Upload the EVSE data of the given charging stations.
        /// </summary>
        /// <param name="ChargingStations">An enumeration of charging stations.</param>
        /// <param name="ActionType">The server-side data management operation.</param>
        /// <param name="OperatorId">An optional unique identification of the EVSE operator.</param>
        /// <param name="OperatorName">The optional name of the EVSE operator.</param>
        /// <param name="IncludeEVSEs">Only upload the EVSEs returned by the given filter delegate.</param>
        /// <param name="QueryTimeout">An optional timeout of the HTTP client [default 60 sec.]</param>
        public async Task<Acknowledgement>

            PushEVSEData(IEnumerable<ChargingStation>  ChargingStations,
                         ActionType                    ActionType    = WWCP.ActionType.fullLoad,
                         EVSEOperator_Id               OperatorId    = null,
                         String                        OperatorName  = null,
                         Func<EVSE, Boolean>           IncludeEVSEs  = null,
                         TimeSpan?                     QueryTimeout  = null)

        {

            return await _OperatorRoamingService.PushEVSEData(ChargingStations,
                                                              ActionType,
                                                              OperatorId,
                                                              OperatorName,
                                                              IncludeEVSEs,
                                                              QueryTimeout);

        }

        #endregion

        #region PushEVSEData(ChargingPool,     ActionType = fullLoad, OperatorId = null, OperatorName = null, IncludeEVSEs = null, QueryTimeout = null)

        /// <summary>
        /// Upload the EVSE data of the given charging pool.
        /// </summary>
        /// <param name="ChargingPool">A charging pool.</param>
        /// <param name="ActionType">The server-side data management operation.</param>
        /// <param name="OperatorId">An optional unique identification of the EVSE operator.</param>
        /// <param name="OperatorName">The optional name of the EVSE operator.</param>
        /// <param name="IncludeEVSEs">Only upload the EVSEs returned by the given filter delegate.</param>
        /// <param name="QueryTimeout">An optional timeout of the HTTP client [default 60 sec.]</param>
        public async Task<Acknowledgement>

            PushEVSEData(ChargingPool         ChargingPool,
                         ActionType           ActionType    = WWCP.ActionType.fullLoad,
                         EVSEOperator_Id      OperatorId    = null,
                         String               OperatorName  = null,
                         Func<EVSE, Boolean>  IncludeEVSEs  = null,
                         TimeSpan?            QueryTimeout  = null)

        {

            return await _OperatorRoamingService.PushEVSEData(ChargingPool,
                                                              ActionType,
                                                              OperatorId,
                                                              OperatorName,
                                                              IncludeEVSEs,
                                                              QueryTimeout);

        }

        #endregion

        #region PushEVSEData(ChargingPools,    ActionType = fullLoad, OperatorId = null, OperatorName = null, IncludeEVSEs = null, QueryTimeout = null)

        /// <summary>
        /// Upload the EVSE data of the given charging pools.
        /// </summary>
        /// <param name="ChargingPools">An enumeration of charging pools.</param>
        /// <param name="ActionType">The server-side data management operation.</param>
        /// <param name="OperatorId">An optional unique identification of the EVSE operator.</param>
        /// <param name="OperatorName">The optional name of the EVSE operator.</param>
        /// <param name="IncludeEVSEs">Only upload the EVSEs returned by the given filter delegate.</param>
        /// <param name="QueryTimeout">An optional timeout of the HTTP client [default 60 sec.]</param>
        public async Task<Acknowledgement>

            PushEVSEData(IEnumerable<ChargingPool>  ChargingPools,
                         ActionType                 ActionType    = WWCP.ActionType.fullLoad,
                         EVSEOperator_Id            OperatorId    = null,
                         String                     OperatorName  = null,
                         Func<EVSE, Boolean>        IncludeEVSEs  = null,
                         TimeSpan?                  QueryTimeout  = null)

        {

            return await _OperatorRoamingService.PushEVSEData(ChargingPools,
                                                              ActionType,
                                                              OperatorId,
                                                              OperatorName,
                                                              IncludeEVSEs,
                                                              QueryTimeout);

        }

        #endregion

        #region PushEVSEData(EVSEOperator,     ActionType = fullLoad, OperatorId = null, OperatorName = null, IncludeEVSEs = null, QueryTimeout = null)

        /// <summary>
        /// Upload the EVSE data of the given EVSE operator.
        /// </summary>
        /// <param name="EVSEOperator">An EVSE operator.</param>
        /// <param name="ActionType">The server-side data management operation.</param>
        /// <param name="OperatorId">An optional unique identification of the EVSE operator.</param>
        /// <param name="OperatorName">The optional name of the EVSE operator.</param>
        /// <param name="IncludeEVSEs">Only upload the EVSEs returned by the given filter delegate.</param>
        /// <param name="QueryTimeout">An optional timeout of the HTTP client [default 60 sec.]</param>
        public async Task<Acknowledgement>

            PushEVSEData(EVSEOperator         EVSEOperator,
                         ActionType           ActionType    = WWCP.ActionType.fullLoad,
                         EVSEOperator_Id      OperatorId    = null,
                         String               OperatorName  = null,
                         Func<EVSE, Boolean>  IncludeEVSEs  = null,
                         TimeSpan?            QueryTimeout  = null)

        {

            return await _OperatorRoamingService.PushEVSEData(EVSEOperator,
                                                              ActionType,
                                                              OperatorId,
                                                              OperatorName,
                                                              IncludeEVSEs,
                                                              QueryTimeout);

        }

        #endregion

        #region PushEVSEData(EVSEOperators,    ActionType = fullLoad, OperatorId = null, OperatorName = null, IncludeEVSEs = null, QueryTimeout = null)

        /// <summary>
        /// Upload the EVSE data of the given EVSE operators.
        /// </summary>
        /// <param name="EVSEOperators">An enumeration of EVSE operators.</param>
        /// <param name="ActionType">The server-side data management operation.</param>
        /// <param name="OperatorId"></param>
        /// <param name="OperatorName">An optional alternative EVSE operator name used for uploading all EVSEs.</param>
        /// <param name="IncludeEVSEs">Only upload the EVSEs returned by the given filter delegate.</param>
        /// <param name="QueryTimeout">An optional timeout for this query.</param>
        public async Task<Acknowledgement>

            PushEVSEData(IEnumerable<EVSEOperator>  EVSEOperators,
                         ActionType                 ActionType    = WWCP.ActionType.fullLoad,
                         EVSEOperator_Id            OperatorId    = null,
                         String                     OperatorName  = null,
                         Func<EVSE, Boolean>        IncludeEVSEs  = null,
                         TimeSpan?                  QueryTimeout  = null)

        {

            return await _OperatorRoamingService.PushEVSEData(EVSEOperators,
                                                              ActionType,
                                                              OperatorId,
                                                              OperatorName,
                                                              IncludeEVSEs,
                                                              QueryTimeout);

        }

        #endregion

        #region PushEVSEData(RoamingNetwork,   ActionType = fullLoad, OperatorId = null, OperatorName = null, IncludeEVSEs = null, QueryTimeout = null)

        /// <summary>
        /// Upload the EVSE data of the given roaming network.
        /// </summary>
        /// <param name="RoamingNetwork">A roaming network.</param>
        /// <param name="ActionType">The server-side data management operation.</param>
        /// <param name="OperatorId">An optional unique identification of the EVSE operator.</param>
        /// <param name="OperatorName">The optional name of the EVSE operator.</param>
        /// <param name="IncludeEVSEs">Only upload the EVSEs returned by the given filter delegate.</param>
        /// <param name="QueryTimeout">An optional timeout of the HTTP client [default 60 sec.]</param>
        public async Task<Acknowledgement>

            PushEVSEData(RoamingNetwork       RoamingNetwork,
                         ActionType           ActionType    = WWCP.ActionType.fullLoad,
                         EVSEOperator_Id      OperatorId    = null,
                         String               OperatorName  = null,
                         Func<EVSE, Boolean>  IncludeEVSEs  = null,
                         TimeSpan?            QueryTimeout  = null)

        {

            return await _OperatorRoamingService.PushEVSEData(RoamingNetwork,
                                                              ActionType,
                                                              OperatorId,
                                                              OperatorName,
                                                              IncludeEVSEs,
                                                              QueryTimeout);

        }

        #endregion

        #endregion

        #region PushEVSEStatus...

        #region PushEVSEStatus(GroupedEVSEStatus, ActionType = update, OperatorId = null, OperatorName = null,                      QueryTimeout = null)

        /// <summary>
        /// Upload the given lookup of EVSE status grouped by their EVSE operator.
        /// </summary>
        /// <param name="GroupedEVSEStatus">A lookup of EVSE status grouped by their EVSE operator.</param>
        /// <param name="ActionType">The server-side data management operation.</param>
        /// <param name="OperatorId">An optional unique identification of the EVSE operator.</param>
        /// <param name="OperatorName">The optional name of the EVSE operator.</param>
        /// <param name="QueryTimeout">An optional timeout of the HTTP client [default 60 sec.]</param>
        public async Task<Acknowledgement>

            PushEVSEStatus(ILookup<EVSEOperator_Id, EVSEStatus>  GroupedEVSEStatus,
                           ActionType                            ActionType    = WWCP.ActionType.update,
                           EVSEOperator_Id                       OperatorId    = null,
                           String                                OperatorName  = null,
                           TimeSpan?                             QueryTimeout  = null)

        {

            return await _OperatorRoamingService.PushEVSEStatus(GroupedEVSEStatus,
                                                                ActionType,
                                                                OperatorId,
                                                                OperatorName,
                                                                QueryTimeout);

        }

        #endregion

        #region PushEVSEStatus(EVSEStatus,        ActionType = update, OperatorId = null, OperatorName = null,                      QueryTimeout = null)

        /// <summary>
        /// Upload the given EVSE status.
        /// </summary>
        /// <param name="EVSEStatus">An EVSE status.</param>
        /// <param name="ActionType">The server-side data management operation.</param>
        /// <param name="OperatorId">An optional unique identification of the EVSE operator.</param>
        /// <param name="OperatorName">The optional name of the EVSE operator.</param>
        /// <param name="QueryTimeout">An optional timeout of the HTTP client [default 60 sec.]</param>
        public async Task<Acknowledgement>

            PushEVSEStatus(EVSEStatus       EVSEStatus,
                           ActionType       ActionType    = WWCP.ActionType.update,
                           EVSEOperator_Id  OperatorId    = null,
                           String           OperatorName  = null,
                           TimeSpan?        QueryTimeout  = null)

        {

            return await _OperatorRoamingService.PushEVSEStatus(EVSEStatus,
                                                                ActionType,
                                                                OperatorId,
                                                                OperatorName,
                                                                QueryTimeout);

        }

        #endregion

        #region PushEVSEStatus(EVSEStatus,        ActionType = update, OperatorId = null, OperatorName = null,                      QueryTimeout = null)

        /// <summary>
        /// Upload the given enumeration of EVSE status.
        /// </summary>
        /// <param name="EVSEStatus">An enumeration of EVSE status.</param>
        /// <param name="ActionType">The server-side data management operation.</param>
        /// <param name="OperatorId">An optional unique identification of the EVSE operator.</param>
        /// <param name="OperatorName">The optional name of the EVSE operator.</param>
        /// <param name="QueryTimeout">An optional timeout of the HTTP client [default 60 sec.]</param>
        public async Task<Acknowledgement>

            PushEVSEStatus(IEnumerable<EVSEStatus>  EVSEStatus,
                           ActionType               ActionType    = WWCP.ActionType.update,
                           EVSEOperator_Id          OperatorId    = null,
                           String                   OperatorName  = null,
                           TimeSpan?                QueryTimeout  = null)

        {

            return await _OperatorRoamingService.PushEVSEStatus(EVSEStatus,
                                                                ActionType,
                                                                OperatorId,
                                                                OperatorName,
                                                                QueryTimeout);

        }

        #endregion

        #region PushEVSEStatus(EVSE,              ActionType = update, OperatorId = null, OperatorName = null, IncludeEVSEs = null, QueryTimeout = null)

        /// <summary>
        /// Upload the EVSE status of the given EVSE.
        /// </summary>
        /// <param name="EVSE">An EVSE.</param>
        /// <param name="ActionType">The server-side data management operation.</param>
        /// <param name="OperatorId">An optional unique identification of the EVSE operator.</param>
        /// <param name="OperatorName">The optional name of the EVSE operator.</param>
        /// <param name="IncludeEVSEs">Only upload the EVSEs returned by the given filter delegate.</param>
        /// <param name="QueryTimeout">An optional timeout of the HTTP client [default 60 sec.]</param>
        public async Task<Acknowledgement>

            PushEVSEStatus(EVSE      EVSE,
                           WWCP.ActionType      ActionType    = WWCP.ActionType.update,
                           EVSEOperator_Id      OperatorId    = null,
                           String               OperatorName  = null,
                           Func<EVSE, Boolean>  IncludeEVSEs  = null,
                           TimeSpan?            QueryTimeout  = null)

        {

            return await _OperatorRoamingService.PushEVSEStatus(EVSE,
                                                                ActionType,
                                                                OperatorId,
                                                                OperatorName,
                                                                IncludeEVSEs,
                                                                QueryTimeout);

        }

        #endregion

        #region PushEVSEStatus(EVSEs,             ActionType = update, OperatorId = null, OperatorName = null, IncludeEVSEs = null, QueryTimeout = null)

        /// <summary>
        /// Upload all EVSE status of the given enumeration of EVSEs.
        /// </summary>
        /// <param name="EVSEs">An enumeration of EVSEs.</param>
        /// <param name="ActionType">The server-side data management operation.</param>
        /// <param name="OperatorId">An optional unique identification of the EVSE operator.</param>
        /// <param name="OperatorName">The optional name of the EVSE operator.</param>
        /// <param name="IncludeEVSEs">Only upload the EVSEs returned by the given filter delegate.</param>
        /// <param name="QueryTimeout">An optional timeout of the HTTP client [default 60 sec.]</param>
        public async Task<Acknowledgement>

            PushEVSEStatus(IEnumerable<EVSE>    EVSEs,
                           ActionType           ActionType    = WWCP.ActionType.update,
                           EVSEOperator_Id      OperatorId    = null,
                           String               OperatorName  = null,
                           Func<EVSE, Boolean>  IncludeEVSEs  = null,
                           TimeSpan?            QueryTimeout  = null)

        {

            return await _OperatorRoamingService.PushEVSEStatus(EVSEs,
                                                                ActionType,
                                                                OperatorId,
                                                                OperatorName,
                                                                IncludeEVSEs,
                                                                QueryTimeout);

        }

        #endregion

        #region PushEVSEStatus(ChargingStation,   ActionType = update, OperatorId = null, OperatorName = null, IncludeEVSEs = null, QueryTimeout = null)

        /// <summary>
        /// Upload all EVSE status of the given charging station.
        /// </summary>
        /// <param name="ChargingStation">A charging station.</param>
        /// <param name="ActionType">The server-side data management operation.</param>
        /// <param name="OperatorId">An optional unique identification of the EVSE operator.</param>
        /// <param name="OperatorName">The optional name of the EVSE operator.</param>
        /// <param name="IncludeEVSEs">Only upload the EVSEs returned by the given filter delegate.</param>
        /// <param name="QueryTimeout">An optional timeout of the HTTP client [default 60 sec.]</param>
        public async Task<Acknowledgement>

            PushEVSEStatus(ChargingStation      ChargingStation,
                           WWCP.ActionType      ActionType    = WWCP.ActionType.update,
                           EVSEOperator_Id      OperatorId    = null,
                           String               OperatorName  = null,
                           Func<EVSE, Boolean>  IncludeEVSEs  = null,
                           TimeSpan?            QueryTimeout  = null)

        {

            return await _OperatorRoamingService.PushEVSEStatus(ChargingStation,
                                                                ActionType,
                                                                OperatorId,
                                                                OperatorName,
                                                                IncludeEVSEs,
                                                                QueryTimeout);

        }

        #endregion

        #region PushEVSEStatus(ChargingStations,  ActionType = update, OperatorId = null, OperatorName = null, IncludeEVSEs = null, QueryTimeout = null)

        /// <summary>
        /// Upload all EVSE status of the given enumeration of charging stations.
        /// </summary>
        /// <param name="ChargingStations">An enumeration of charging stations.</param>
        /// <param name="ActionType">The server-side data management operation.</param>
        /// <param name="OperatorId">An optional unique identification of the EVSE operator.</param>
        /// <param name="OperatorName">The optional name of the EVSE operator.</param>
        /// <param name="IncludeEVSEs">Only upload the EVSEs returned by the given filter delegate.</param>
        /// <param name="QueryTimeout">An optional timeout of the HTTP client [default 60 sec.]</param>
        public async Task<Acknowledgement>

            PushEVSEStatus(IEnumerable<ChargingStation>  ChargingStations,
                           ActionType                    ActionType    = WWCP.ActionType.update,
                           EVSEOperator_Id               OperatorId    = null,
                           String                        OperatorName  = null,
                           Func<EVSE, Boolean>           IncludeEVSEs  = null,
                           TimeSpan?                     QueryTimeout  = null)

        {

            return await _OperatorRoamingService.PushEVSEStatus(ChargingStations,
                                                                ActionType,
                                                                OperatorId,
                                                                OperatorName,
                                                                IncludeEVSEs,
                                                                QueryTimeout);

        }

        #endregion

        #region PushEVSEStatus(ChargingPool,      ActionType = update, OperatorId = null, OperatorName = null, IncludeEVSEs = null, QueryTimeout = null)

        /// <summary>
        /// Upload all EVSE status of the given charging pool.
        /// </summary>
        /// <param name="ChargingPool">A charging pool.</param>
        /// <param name="ActionType">The server-side data management operation.</param>
        /// <param name="OperatorId">An optional unique identification of the EVSE operator.</param>
        /// <param name="OperatorName">The optional name of the EVSE operator.</param>
        /// <param name="IncludeEVSEs">Only upload the EVSEs returned by the given filter delegate.</param>
        /// <param name="QueryTimeout">An optional timeout of the HTTP client [default 60 sec.]</param>
        public async Task<Acknowledgement>

            PushEVSEStatus(ChargingPool         ChargingPool,
                           ActionType           ActionType    = WWCP.ActionType.update,
                           EVSEOperator_Id      OperatorId    = null,
                           String               OperatorName  = null,
                           Func<EVSE, Boolean>  IncludeEVSEs  = null,
                           TimeSpan?            QueryTimeout  = null)

        {

            return await _OperatorRoamingService.PushEVSEStatus(ChargingPool,
                                                                ActionType,
                                                                OperatorId,
                                                                OperatorName,
                                                                IncludeEVSEs,
                                                                QueryTimeout);

        }

        #endregion

        #region PushEVSEStatus(ChargingPools,     ActionType = update, OperatorId = null, OperatorName = null, IncludeEVSEs = null, QueryTimeout = null)

        /// <summary>
        /// Upload all EVSE status of the given enumeration of charging pools.
        /// </summary>
        /// <param name="ChargingPools">An enumeration of charging pools.</param>
        /// <param name="ActionType">The server-side data management operation.</param>
        /// <param name="OperatorId">An optional unique identification of the EVSE operator.</param>
        /// <param name="OperatorName">The optional name of the EVSE operator.</param>
        /// <param name="IncludeEVSEs">Only upload the EVSEs returned by the given filter delegate.</param>
        /// <param name="QueryTimeout">An optional timeout of the HTTP client [default 60 sec.]</param>
        public async Task<Acknowledgement>

            PushEVSEStatus(IEnumerable<ChargingPool>  ChargingPools,
                           ActionType                 ActionType    = WWCP.ActionType.update,
                           EVSEOperator_Id            OperatorId    = null,
                           String                     OperatorName  = null,
                           Func<EVSE, Boolean>        IncludeEVSEs  = null,
                           TimeSpan?                  QueryTimeout  = null)

        {

            return await _OperatorRoamingService.PushEVSEStatus(ChargingPools,
                                                                ActionType,
                                                                OperatorId,
                                                                OperatorName,
                                                                IncludeEVSEs,
                                                                QueryTimeout);

        }

        #endregion

        #region PushEVSEStatus(EVSEOperator,      ActionType = update, OperatorId = null, OperatorName = null, IncludeEVSEs = null, QueryTimeout = null)

        /// <summary>
        /// Upload all EVSE status of the given EVSE operator.
        /// </summary>
        /// <param name="EVSEOperator">An EVSE operator.</param>
        /// <param name="ActionType">The server-side data management operation.</param>
        /// <param name="OperatorId">An optional unique identification of the EVSE operator.</param>
        /// <param name="OperatorName">The optional name of the EVSE operator.</param>
        /// <param name="IncludeEVSEs">Only upload the EVSEs returned by the given filter delegate.</param>
        /// <param name="QueryTimeout">An optional timeout of the HTTP client [default 60 sec.]</param>
        public async Task<Acknowledgement>

            PushEVSEStatus(EVSEOperator         EVSEOperator,
                           ActionType           ActionType    = WWCP.ActionType.update,
                           EVSEOperator_Id      OperatorId    = null,
                           String               OperatorName  = null,
                           Func<EVSE, Boolean>  IncludeEVSEs  = null,
                           TimeSpan?            QueryTimeout  = null)

        {

            return await _OperatorRoamingService.PushEVSEStatus(EVSEOperator,
                                                                ActionType,
                                                                OperatorId,
                                                                OperatorName,
                                                                IncludeEVSEs,
                                                                QueryTimeout);

        }

        #endregion

        #region PushEVSEStatus(EVSEOperators,     ActionType = update, OperatorId = null, OperatorName = null, IncludeEVSEs = null, QueryTimeout = null)

        /// <summary>
        /// Upload all EVSE status of the given enumeration of EVSE operators.
        /// </summary>
        /// <param name="EVSEOperators">An enumeration of EVSES operators.</param>
        /// <param name="ActionType">The server-side data management operation.</param>
        /// <param name="OperatorId">An optional unique identification of the EVSE operator.</param>
        /// <param name="OperatorName">The optional name of the EVSE operator.</param>
        /// <param name="IncludeEVSEs">Only upload the EVSEs returned by the given filter delegate.</param>
        /// <param name="QueryTimeout">An optional timeout of the HTTP client [default 60 sec.]</param>
        public async Task<Acknowledgement>

            PushEVSEStatus(IEnumerable<EVSEOperator>  EVSEOperators,
                           ActionType                 ActionType    = WWCP.ActionType.update,
                           EVSEOperator_Id            OperatorId    = null,
                           String                     OperatorName  = null,
                           Func<EVSE, Boolean>        IncludeEVSEs  = null,
                           TimeSpan?                  QueryTimeout  = null)

        {

            return await _OperatorRoamingService.PushEVSEStatus(EVSEOperators,
                                                                ActionType,
                                                                OperatorId,
                                                                OperatorName,
                                                                IncludeEVSEs,
                                                                QueryTimeout);

        }

        #endregion

        #region PushEVSEStatus(RoamingNetwork,    ActionType = update, OperatorId = null, OperatorName = null, IncludeEVSEs = null, QueryTimeout = null)

        /// <summary>
        /// Upload all EVSE status of the given roaming network.
        /// </summary>
        /// <param name="RoamingNetwork">A roaming network.</param>
        /// <param name="ActionType">The server-side data management operation.</param>
        /// <param name="OperatorId">An optional unique identification of the EVSE operator.</param>
        /// <param name="OperatorName">The optional name of the EVSE operator.</param>
        /// <param name="IncludeEVSEs">Only upload the EVSEs returned by the given filter delegate.</param>
        /// <param name="QueryTimeout">An optional timeout of the HTTP client [default 60 sec.]</param>
        public async Task<Acknowledgement>

            PushEVSEStatus(RoamingNetwork       RoamingNetwork,
                           ActionType           ActionType    = WWCP.ActionType.update,
                           EVSEOperator_Id      OperatorId    = null,
                           String               OperatorName  = null,
                           Func<EVSE, Boolean>  IncludeEVSEs  = null,
                           TimeSpan?            QueryTimeout  = null)

        {

            return await _OperatorRoamingService.PushEVSEStatus(RoamingNetwork,
                                                                ActionType,
                                                                OperatorId,
                                                                OperatorName,
                                                                IncludeEVSEs,
                                                                QueryTimeout);

        }

        #endregion

        #region PushEVSEStatus(EVSEStatusDiff, QueryTimeout = null)

        /// <summary>
        /// Send EVSE status updates.
        /// </summary>
        /// <param name="EVSEStatusDiff">An EVSE status diff.</param>
        /// <param name="QueryTimeout">An optional timeout for this query.</param>
        public async Task PushEVSEStatus(EVSEStatusDiff  EVSEStatusDiff,
                                         TimeSpan?       QueryTimeout  = null)

        {

            await _OperatorRoamingService.PushEVSEStatus(EVSEStatusDiff,
                                                         QueryTimeout);

        }

        #endregion

        #endregion

        #region EnqueueChargingPoolDataUpdate(ChargingPool, PropertyName, OldValue, NewValue)

        /// <summary>
        /// Enqueue the given EVSE data for a delayed upload.
        /// </summary>
        /// <param name="ChargingPool">A charging station.</param>
        public Task<Acknowledgement>

            EnqueueChargingPoolDataUpdate(ChargingPool  ChargingPool,
                                          String        PropertyName,
                                          Object        OldValue,
                                          Object        NewValue)

        {

            #region Initial checks

            if (ChargingPool == null)
                throw new ArgumentNullException(nameof(ChargingPool), "The given charging station must not be null!");

            #endregion

            lock (ServiceCheckLock)
            {

                foreach (var EVSE in ChargingPool.SelectMany(station => station))
                {

                    if (IncludeEVSEs == null ||
                       (IncludeEVSEs != null && IncludeEVSEs(EVSE)))
                    {

                        EVSEDataUpdatesQueue.Add(EVSE);

                        ServiceCheckTimer.Change(_ServiceCheckEvery, Timeout.Infinite);

                    }

                }

            }

            return Task.FromResult(new Acknowledgement(true));

        }

        #endregion

        #region EnqueueChargingStationDataUpdate(ChargingStation, PropertyName, OldValue, NewValue)

        /// <summary>
        /// Enqueue the given EVSE data for a delayed upload.
        /// </summary>
        /// <param name="ChargingStation">A charging station.</param>
        public Task<Acknowledgement>

            EnqueueChargingStationDataUpdate(ChargingStation  ChargingStation,
                                             String           PropertyName,
                                             Object           OldValue,
                                             Object           NewValue)

        {

            #region Initial checks

            if (ChargingStation == null)
                throw new ArgumentNullException(nameof(ChargingStation), "The given charging station must not be null!");

            #endregion

            lock (ServiceCheckLock)
            {

                foreach (var EVSE in ChargingStation)
                {

                    if (IncludeEVSEs == null ||
                       (IncludeEVSEs != null && IncludeEVSEs(EVSE)))
                    {

                        EVSEDataUpdatesQueue.Add(EVSE);

                        ServiceCheckTimer.Change(_ServiceCheckEvery, Timeout.Infinite);

                    }

                }

            }

            return Task.FromResult(new Acknowledgement(true));

        }

        #endregion

        #region EnqueueEVSEAdddition(Timestamp, ChargingStation, EVSE)

        public Task EnqueueEVSEAdddition(DateTime         Timestamp,
                                         ChargingStation  ChargingStation,
                                         EVSE             EVSE)

        {

            #region Initial checks

            if (ChargingStation == null)
                throw new ArgumentNullException(nameof(ChargingStation),  "The given charging station must not be null!");

            if (EVSE            == null)
                throw new ArgumentNullException(nameof(EVSE),             "The given EVSE must not be null!");

            #endregion

            lock (ServiceCheckLock)
            {

                if (IncludeEVSEs == null ||
                   (IncludeEVSEs != null && IncludeEVSEs(EVSE)))
                {

                    EVSEsToAddQueue.Add(EVSE);

                    ServiceCheckTimer.Change(_ServiceCheckEvery, Timeout.Infinite);

                }

            }

            return Task.FromResult(new Acknowledgement(true));

        }

        #endregion

        #region EnqueueEVSERemoval(Timestamp, ChargingStation, EVSE)

        public Task EnqueueEVSERemoval(DateTime         Timestamp,
                                       ChargingStation  ChargingStation,
                                       EVSE             EVSE)

        {

            #region Initial checks

            if (ChargingStation == null)
                throw new ArgumentNullException(nameof(ChargingStation),  "The given charging station must not be null!");

            if (EVSE            == null)
                throw new ArgumentNullException(nameof(EVSE),             "The given EVSE must not be null!");

            #endregion

            lock (ServiceCheckLock)
            {

                if (IncludeEVSEs == null ||
                   (IncludeEVSEs != null && IncludeEVSEs(EVSE)))
                {

                    EVSEsToRemoveQueue.Add(EVSE);

                    ServiceCheckTimer.Change(_ServiceCheckEvery, Timeout.Infinite);

                }

            }

            return Task.FromResult(new Acknowledgement(true));

        }

        #endregion

        #region EnqueueEVSEDataUpdate(EVSE, PropertyName, OldValue, NewValue)

        /// <summary>
        /// Enqueue the given EVSE data for a delayed upload.
        /// </summary>
        /// <param name="EVSE">An EVSE.</param>
        public Task<Acknowledgement>

            EnqueueEVSEDataUpdate(EVSE    EVSE,
                                  String  PropertyName,
                                  Object  OldValue,
                                  Object  NewValue)

        {

            #region Initial checks

            if (EVSE == null)
                throw new ArgumentNullException(nameof(EVSE), "The given EVSE must not be null!");

            #endregion

            lock (ServiceCheckLock)
            {

                if (IncludeEVSEs == null ||
                   (IncludeEVSEs != null && IncludeEVSEs(EVSE)))
                {

                    EVSEDataUpdatesQueue.Add(EVSE);

                    ServiceCheckTimer.Change(_ServiceCheckEvery, Timeout.Infinite);

                }

            }

            return Task.FromResult(new Acknowledgement(true));

        }

        #endregion

        #region EnqueueEVSEStatusUpdate(EVSE, OldStatus, NewStatus)

        /// <summary>
        /// Enqueue the given EVSE status for a delayed upload.
        /// </summary>
        /// <param name="EVSE">An EVSE.</param>
        /// <param name="OldStatus">The old status of the EVSE.</param>
        /// <param name="NewStatus">The new status of the EVSE.</param>
        public Task<Acknowledgement>

            EnqueueEVSEStatusUpdate(EVSE                         EVSE,
                                    Timestamped<EVSEStatusType>  OldStatus,
                                    Timestamped<EVSEStatusType>  NewStatus)

        {

            #region Initial checks

            if (EVSE == null)
                throw new ArgumentNullException(nameof(EVSE), "The given EVSE must not be null!");

            #endregion

            lock (ServiceCheckLock)
            {

                if (IncludeEVSEs == null ||
                   (IncludeEVSEs != null && IncludeEVSEs(EVSE)))
                {

                    EVSEStatusChangesQueue.Add(new EVSEStatusChange(EVSE.Id, OldStatus, NewStatus));

                    ServiceCheckTimer.Change(_ServiceCheckEvery, Timeout.Infinite);

                }

            }

            return Task.FromResult(new Acknowledgement(true));

        }

        #endregion


        #region FlushQueues()

        public async Task FlushQueues()
        {

            #region Make a thread local copy of all data

            //ToDo: AsyncLocal is currently not implemented in Mono!
            //var EVSEDataQueueCopy   = new AsyncLocal<HashSet<EVSE>>();
            //var EVSEStatusQueueCopy = new AsyncLocal<List<EVSEStatusChange>>();

            var EVSEsToAddQueueCopy     = new ThreadLocal<HashSet<EVSE>>();
            var EVSEDataQueueCopy       = new ThreadLocal<HashSet<EVSE>>();
            var EVSEStatusQueueCopy     = new ThreadLocal<List<EVSEStatusChange>>();
            var EVSEsToRemoveQueueCopy  = new ThreadLocal<HashSet<EVSE>>();

            if (Monitor.TryEnter(ServiceCheckLock))
            {

                try
                {

                    if (EVSEsToAddQueue.       Count == 0 &&
                        EVSEDataUpdatesQueue.  Count == 0 &&
                        EVSEStatusChangesQueue.Count == 0 &&
                        EVSEsToRemoveQueue.    Count == 0)
                        return;

                    _RunId++;

                    // Copy 'EVSEs to add', remove originals...
                    EVSEsToAddQueueCopy.Value     = new HashSet<EVSE>(EVSEsToAddQueue);
                    EVSEsToAddQueue.Clear();

                    // Copy 'EVSEs to update', remove originals...
                    EVSEDataQueueCopy.Value       = new HashSet<EVSE>(EVSEDataUpdatesQueue);
                    EVSEDataUpdatesQueue.Clear();

                    // Copy 'EVSE status changes', remove originals...
                    EVSEStatusQueueCopy.Value     = new List<EVSEStatusChange>(EVSEStatusChangesQueue);
                    EVSEStatusChangesQueue.Clear();

                    // Copy 'EVSEs to remove', remove originals...
                    EVSEsToRemoveQueueCopy.Value  = new HashSet<EVSE>(EVSEsToRemoveQueue);
                    EVSEsToRemoveQueue.Clear();


                    // Stop the timer. Will be rescheduled by next EVSE data/status change...
                    ServiceCheckTimer.Change(Timeout.Infinite, Timeout.Infinite);

                }
                catch (Exception e)
                {

                    while (e.InnerException != null)
                        e = e.InnerException;

                    DebugX.LogT("EVSEOperatorRoamingProvider '" + Id + "' led to an exception: " + e.Message + Environment.NewLine + e.StackTrace);

                }

                finally
                {
                    Monitor.Exit(ServiceCheckLock);
                }

            }

            #endregion

            // Upload status changes...
            if (EVSEsToAddQueueCopy.   Value != null ||
                EVSEDataQueueCopy.     Value != null ||
                EVSEStatusQueueCopy.   Value != null ||
                EVSEsToRemoveQueueCopy.Value != null)
            {

                // Use the events to evaluate if something went wrong!

                #region Send new EVSE data

                if (EVSEsToAddQueueCopy.Value.Count > 0)
                {

                    var EVSEsToAddTask = PushEVSEData(EVSEsToAddQueueCopy.Value,
                                                      _RunId == 1
                                                          ? ActionType.fullLoad
                                                          : ActionType.insert);

                    EVSEsToAddTask.Wait();

                }

                #endregion

                #region Send changed EVSE data

                if (EVSEDataQueueCopy.Value.Count > 0)
                {

                    // Surpress EVSE data updates for all newly added EVSEs
                    var EVSEsWithoutNewEVSEs = EVSEDataQueueCopy.Value.
                                                   Where(evse => !EVSEsToAddQueueCopy.Value.Contains(evse)).
                                                   ToArray();


                    if (EVSEsWithoutNewEVSEs.Length > 0)
                    {

                        var PushEVSEDataTask = PushEVSEData(EVSEsWithoutNewEVSEs, ActionType.update);

                        PushEVSEDataTask.Wait();

                    }

                }

                #endregion

                #region Send changed EVSE status

                if (EVSEStatusQueueCopy.Value.Count > 0)
                {

                    var PushEVSEStatusTask = PushEVSEStatus(EVSEStatusQueueCopy.Value.Select(statuschange => statuschange.CurrentStatus).ToArray(),
                                                            _RunId == 1
                                                                ? ActionType.fullLoad
                                                                : ActionType.update);

                    PushEVSEStatusTask.Wait();

                }

                #endregion

                //ToDo: Send removed EVSE data!

            }

            return;

        }

        #endregion


        #region AuthorizeStart/-Stop...

        #region AuthorizeStart(...OperatorId, AuthToken, ChargingProductId = null, SessionId = null, ...)

        /// <summary>
        /// Create an authorize start request.
        /// </summary>
        /// <param name="Timestamp">The timestamp of the request.</param>
        /// <param name="CancellationToken">A token to cancel this request.</param>
        /// <param name="EventTrackingId">An unique event tracking identification for correlating this request with other events.</param>
        /// <param name="OperatorId">An EVSE operator identification.</param>
        /// <param name="AuthToken">A (RFID) user identification.</param>
        /// <param name="ChargingProductId">An optional charging product identification.</param>
        /// <param name="SessionId">An optional session identification.</param>
        /// <param name="QueryTimeout">An optional timeout for this request.</param>
        public async Task<AuthStartResult>

            AuthorizeStart(DateTime             Timestamp,
                           CancellationToken    CancellationToken,
                           EventTracking_Id     EventTrackingId,
                           EVSEOperator_Id      OperatorId,
                           Auth_Token           AuthToken,
                           ChargingProduct_Id   ChargingProductId  = null,
                           ChargingSession_Id   SessionId          = null,
                           TimeSpan?            QueryTimeout       = null)

        {

            return await _OperatorRoamingService.AuthorizeStart(Timestamp,
                                                                CancellationToken,
                                                                EventTrackingId,
                                                                OperatorId,
                                                                AuthToken,
                                                                ChargingProductId,
                                                                SessionId,
                                                                QueryTimeout);

        }

        #endregion

        #region AuthorizeStart(...OperatorId, AuthToken, EVSEId, ChargingProductId = null, SessionId = null, ...)

        /// <summary>
        /// Create an authorize start request.
        /// </summary>
        /// <param name="Timestamp">The timestamp of the request.</param>
        /// <param name="CancellationToken">A token to cancel this request.</param>
        /// <param name="EventTrackingId">An unique event tracking identification for correlating this request with other events.</param>
        /// <param name="OperatorId">An EVSE operator identification.</param>
        /// <param name="AuthToken">A (RFID) user identification.</param>
        /// <param name="EVSEId">The unique identification of an EVSE.</param>
        /// <param name="ChargingProductId">An optional charging product identification.</param>
        /// <param name="SessionId">An optional session identification.</param>
        /// <param name="QueryTimeout">An optional timeout for this request.</param>
        public async Task<AuthStartEVSEResult>

            AuthorizeStart(DateTime             Timestamp,
                           CancellationToken    CancellationToken,
                           EventTracking_Id     EventTrackingId,
                           EVSEOperator_Id      OperatorId,
                           Auth_Token           AuthToken,
                           EVSE_Id              EVSEId,
                           ChargingProduct_Id   ChargingProductId  = null,
                           ChargingSession_Id   SessionId          = null,
                           TimeSpan?            QueryTimeout       = null)

        {

            return await _OperatorRoamingService.AuthorizeStart(Timestamp,
                                                                CancellationToken,
                                                                EventTrackingId,
                                                                OperatorId,
                                                                AuthToken,
                                                                EVSEId,
                                                                ChargingProductId,
                                                                SessionId,
                                                                QueryTimeout);

        }

        #endregion

        #region AuthorizeStart(...OperatorId, AuthToken, ChargingStationId, ChargingProductId = null, SessionId = null, ...)

        /// <summary>
        /// Create an authorize start request.
        /// </summary>
        /// <param name="Timestamp">The timestamp of the request.</param>
        /// <param name="CancellationToken">A token to cancel this request.</param>
        /// <param name="EventTrackingId">An unique event tracking identification for correlating this request with other events.</param>
        /// <param name="OperatorId">An EVSE operator identification.</param>
        /// <param name="AuthToken">A (RFID) user identification.</param>
        /// <param name="ChargingStationId">The unique identification of a charging station.</param>
        /// <param name="ChargingProductId">An optional charging product identification.</param>
        /// <param name="SessionId">An optional session identification.</param>
        /// <param name="QueryTimeout">An optional timeout for this request.</param>
        public async Task<AuthStartChargingStationResult>

            AuthorizeStart(DateTime             Timestamp,
                           CancellationToken    CancellationToken,
                           EventTracking_Id     EventTrackingId,
                           EVSEOperator_Id      OperatorId,
                           Auth_Token           AuthToken,
                           ChargingStation_Id   ChargingStationId,
                           ChargingProduct_Id   ChargingProductId  = null,
                           ChargingSession_Id   SessionId          = null,
                           TimeSpan?            QueryTimeout       = null)

        {

            return await _OperatorRoamingService.AuthorizeStart(Timestamp,
                                                                CancellationToken,
                                                                EventTrackingId,
                                                                OperatorId,
                                                                AuthToken,
                                                                ChargingStationId,
                                                                ChargingProductId,
                                                                SessionId,
                                                                QueryTimeout);

        }

        #endregion


        #region AuthorizeStop(...OperatorId, SessionId, AuthToken, ...)

        // UID => Not everybody can stop any session, but maybe another
        //        UID than the UID which started the session!
        //        (e.g. car sharing)

        /// <summary>
        /// Create an authorize stop request.
        /// </summary>
        /// <param name="Timestamp">The timestamp of the request.</param>
        /// <param name="CancellationToken">A token to cancel this request.</param>
        /// <param name="EventTrackingId">An unique event tracking identification for correlating this request with other events.</param>
        /// <param name="OperatorId">An EVSE Operator identification.</param>
        /// <param name="SessionId">The OICP session identification from the AuthorizeStart request.</param>
        /// <param name="AuthToken">A (RFID) user identification.</param>
        /// <param name="QueryTimeout">An optional timeout for this request.</param>
        public async Task<AuthStopResult>

            AuthorizeStop(DateTime            Timestamp,
                          CancellationToken   CancellationToken,
                          EventTracking_Id    EventTrackingId,
                          EVSEOperator_Id     OperatorId,
                          ChargingSession_Id  SessionId,
                          Auth_Token          AuthToken,
                          TimeSpan?           QueryTimeout  = null)

        {

            return await _OperatorRoamingService.AuthorizeStop(Timestamp,
                                                               CancellationToken,
                                                               EventTrackingId,
                                                               OperatorId,
                                                               SessionId,
                                                               AuthToken,
                                                               QueryTimeout);

        }

        #endregion

        #region AuthorizeStop(...OperatorId, EVSEId, SessionId, AuthToken, ...)

        // UID => Not everybody can stop any session, but maybe another
        //        UID than the UID which started the session!
        //        (e.g. car sharing)

        /// <summary>
        /// Create an authorize stop request.
        /// </summary>
        /// <param name="Timestamp">The timestamp of the request.</param>
        /// <param name="CancellationToken">A token to cancel this request.</param>
        /// <param name="EventTrackingId">An unique event tracking identification for correlating this request with other events.</param>
        /// <param name="OperatorId">An EVSE Operator identification.</param>
        /// <param name="EVSEId">The unique identification of an EVSE.</param>
        /// <param name="SessionId">The OICP session identification from the AuthorizeStart request.</param>
        /// <param name="AuthToken">A (RFID) user identification.</param>
        /// <param name="QueryTimeout">An optional timeout for this request.</param>
        public async Task<AuthStopEVSEResult>

            AuthorizeStop(DateTime            Timestamp,
                          CancellationToken   CancellationToken,
                          EventTracking_Id    EventTrackingId,
                          EVSEOperator_Id     OperatorId,
                          EVSE_Id             EVSEId,
                          ChargingSession_Id  SessionId,
                          Auth_Token          AuthToken,
                          TimeSpan?           QueryTimeout  = null)

        {

            return await _OperatorRoamingService.AuthorizeStop(Timestamp,
                                                               CancellationToken,
                                                               EventTrackingId,
                                                               OperatorId,
                                                               EVSEId,
                                                               SessionId,
                                                               AuthToken,
                                                               QueryTimeout);

        }

        #endregion

        #region AuthorizeStop(...OperatorId, ChargingStationId, SessionId, AuthToken, ...)

        // UID => Not everybody can stop any session, but maybe another
        //        UID than the UID which started the session!
        //        (e.g. car sharing)

        /// <summary>
        /// Create an authorize stop request.
        /// </summary>
        /// <param name="Timestamp">The timestamp of the request.</param>
        /// <param name="CancellationToken">A token to cancel this request.</param>
        /// <param name="EventTrackingId">An unique event tracking identification for correlating this request with other events.</param>
        /// <param name="OperatorId">An EVSE Operator identification.</param>
        /// <param name="ChargingStationId">The unique identification of a charging station.</param>
        /// <param name="SessionId">The OICP session identification from the AuthorizeStart request.</param>
        /// <param name="AuthToken">A (RFID) user identification.</param>
        /// <param name="QueryTimeout">An optional timeout for this request.</param>
        public async Task<AuthStopChargingStationResult>

            AuthorizeStop(DateTime            Timestamp,
                          CancellationToken   CancellationToken,
                          EventTracking_Id    EventTrackingId,
                          EVSEOperator_Id     OperatorId,
                          ChargingStation_Id  ChargingStationId,
                          ChargingSession_Id  SessionId,
                          Auth_Token          AuthToken,
                          TimeSpan?           QueryTimeout  = null)

        {

            return await _OperatorRoamingService.AuthorizeStop(Timestamp,
                                                               CancellationToken,
                                                               EventTrackingId,
                                                               OperatorId,
                                                               ChargingStationId,
                                                               SessionId,
                                                               AuthToken,
                                                               QueryTimeout);

        }

        #endregion

        #endregion

        #region SendChargeDetailRecord...

        #region SendChargeDetailRecord(...ChargeDetailRecord, ...)

        /// <summary>
        /// Send a charge detail record.
        /// </summary>
        /// <param name="Timestamp">The timestamp of the request.</param>
        /// <param name="CancellationToken">A token to cancel this request.</param>
        /// <param name="EventTrackingId">An unique event tracking identification for correlating this request with other events.</param>
        /// <param name="ChargeDetailRecord">A charge detail record.</param>
        /// <param name="QueryTimeout">An optional timeout for this request.</param>
        public async Task<SendCDRResult>

            SendChargeDetailRecord(DateTime            Timestamp,
                                   CancellationToken   CancellationToken,
                                   EventTracking_Id    EventTrackingId,
                                   ChargeDetailRecord  ChargeDetailRecord,
                                   TimeSpan?           QueryTimeout = null)

        {

            return await _OperatorRoamingService.SendChargeDetailRecord(Timestamp,
                                                                        CancellationToken,
                                                                        EventTrackingId,
                                                                        ChargeDetailRecord,
                                                                        QueryTimeout);

        }

        #endregion

        #region SendChargeDetailRecord(...EVSEId, SessionId, PartnerProductId, SessionStart, SessionEnd, Identification, ...)

        /// <summary>
        /// Send a charge detail record.
        /// </summary>
        /// <param name="Timestamp">The timestamp of the request.</param>
        /// <param name="CancellationToken">A token to cancel this request.</param>
        /// <param name="EventTrackingId">An unique event tracking identification for correlating this request with other events.</param>
        /// <param name="EVSEId">The EVSE identification.</param>
        /// <param name="SessionId">The OICP session identification from the Authorize Start request.</param>
        /// <param name="PartnerProductId">The ev charging product identification.</param>
        /// <param name="SessionStart">The session start timestamp.</param>
        /// <param name="SessionEnd">The session end timestamp.</param>
        /// <param name="Identification">An identification.</param>
        /// <param name="ChargingStart">An optional charging start timestamp.</param>
        /// <param name="ChargingEnd">An optional charging end timestamp.</param>
        /// <param name="MeterValueStart">An optional initial value of the energy meter.</param>
        /// <param name="MeterValueEnd">An optional final value of the energy meter.</param>
        /// <param name="MeterValuesInBetween">An optional enumeration of meter values during the charging session.</param>
        /// <param name="ConsumedEnergy">The optional amount of consumed energy.</param>
        /// <param name="MeteringSignature">An optional signature for the metering values.</param>
        /// <param name="QueryTimeout">An optional timeout for this request.</param>
        public async Task<SendCDRResult>

            SendChargeDetailRecord(DateTime             Timestamp,
                                   CancellationToken    CancellationToken,
                                   EventTracking_Id     EventTrackingId,
                                   EVSE_Id              EVSEId,
                                   ChargingSession_Id   SessionId,
                                   ChargingProduct_Id   PartnerProductId,
                                   DateTime             SessionStart,
                                   DateTime             SessionEnd,
                                   AuthInfo             Identification,
                                   DateTime?            ChargingStart         = null,
                                   DateTime?            ChargingEnd           = null,
                                   Double?              MeterValueStart       = null,
                                   Double?              MeterValueEnd         = null,
                                   IEnumerable<Double>  MeterValuesInBetween  = null,
                                   Double?              ConsumedEnergy        = null,
                                   String               MeteringSignature     = null,
                                   TimeSpan?            QueryTimeout          = null)

        {

            return await _OperatorRoamingService.SendChargeDetailRecord(Timestamp,
                                                                        CancellationToken,
                                                                        EventTrackingId,
                                                                        EVSEId,
                                                                        SessionId,
                                                                        PartnerProductId,
                                                                        SessionStart,
                                                                        SessionEnd,
                                                                        Identification,
                                                                        ChargingStart,
                                                                        ChargingEnd,
                                                                        MeterValueStart,
                                                                        MeterValueEnd,
                                                                        MeterValuesInBetween,
                                                                        ConsumedEnergy,
                                                                        MeteringSignature,
                                                                        QueryTimeout);

        }

        #endregion

        #endregion


        public void RemoveChargingStations(DateTime                      Timestamp,
                                           IEnumerable<ChargingStation>  ChargingStations)
        {

            _OperatorRoamingService.RemoveChargingStations(Timestamp, ChargingStations);

        }


        #region IComparable<RoamingProvider> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="Object">An object to compare with.</param>
        public Int32 CompareTo(Object Object)
        {

            if (Object == null)
                throw new ArgumentNullException("The given object must not be null!");

            // Check if the given object is a roaming provider.
            var RoamingProvider = Object as EMPRoamingProvider;
            if ((Object) RoamingProvider == null)
                throw new ArgumentException("The given object is not a roaming provider!");

            return CompareTo(RoamingProvider);

        }

        #endregion

        #region CompareTo(RoamingProvider)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="RoamingProvider">A roaming provider object to compare with.</param>
        public Int32 CompareTo(EMPRoamingProvider RoamingProvider)
        {

            if ((Object) RoamingProvider == null)
                throw new ArgumentNullException("The given roaming provider must not be null!");

            return Id.CompareTo(RoamingProvider.Id);

        }

        #endregion

        #endregion

        #region IEquatable<RoamingProvider> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="Object">An object to compare with.</param>
        /// <returns>true|false</returns>
        public override Boolean Equals(Object Object)
        {

            if (Object == null)
                return false;

            // Check if the given object is a roaming provider.
            var RoamingProvider = Object as EMPRoamingProvider;
            if ((Object) RoamingProvider == null)
                return false;

            return this.Equals(RoamingProvider);

        }

        #endregion

        #region Equals(RoamingProvider)

        /// <summary>
        /// Compares two roaming provider for equality.
        /// </summary>
        /// <param name="RoamingProvider">A roaming provider to compare with.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public Boolean Equals(EMPRoamingProvider RoamingProvider)
        {

            if ((Object) RoamingProvider == null)
                return false;

            return Id.Equals(RoamingProvider.Id);

        }

        #endregion

        #endregion

        #region GetHashCode()

        /// <summary>
        /// Get the hashcode of this object.
        /// </summary>
        public override Int32 GetHashCode()
        {
            return Id.GetHashCode();
        }

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a string representation of this object.
        /// </summary>
        public override String ToString()
        {
            return Id.ToString();
        }

        #endregion

    }

}

