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
using System.Collections.Generic;
using System.Collections.Concurrent;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Illias.Votes;
using org.GraphDefined.Vanaheimr.Styx.Arrows;
using org.GraphDefined.Vanaheimr.Aegir;

#endregion

namespace org.GraphDefined.WWCP
{

    /// <summary>
    /// A electric vehicle parking spot (EVPS).
    /// </summary>
    public class ParkingSpot : AEMobilityEntity<ParkingSpot_Id>,
                               IEquatable<ParkingSpot>, IComparable<ParkingSpot>, IComparable
    {

        #region Data

        #endregion

        #region Properties

        #region Name

        private I18NString _Name;

        /// <summary>
        /// The offical (multi-language) name of the parking spot.
        /// </summary>
        [Mandatory]
        public I18NString Name
        {

            get
            {
                return _Name;
            }

            set
            {
                SetProperty<I18NString>(ref _Name, value);
            }

        }

        #endregion

        #region Description

        private I18NString _Description;

        /// <summary>
        /// An optional additional (multi-language) description of the parking spot.
        /// </summary>
        [Optional]
        public I18NString Description
        {

            get
            {
                return _Description;
            }

            set
            {
                SetProperty<I18NString>(ref _Description, value);
            }

        }

        #endregion

        #region OSM_WayId

        private String _OSM_WayId;

        /// <summary>
        /// OSM Node Id.
        /// </summary>
        [Optional]
        public String OSM_WayId
        {

            get
            {
                return _OSM_WayId;
            }

            set
            {
                SetProperty<String>(ref _OSM_WayId, value);
            }

        }

        #endregion

        #region Geometry

        private List<GeoCoordinate> _Geometry;

        /// <summary>
        /// An optional polygon geometry of the parking spot.
        /// </summary>
        [Optional]
        public List<GeoCoordinate> Geometry
        {
            get
            {
                return _Geometry;
            }
        }

        #endregion

        #region ChargingStations

        private List<ChargingStation> _ChargingStations;

        /// <summary>
        /// Charging stations reachable from this parking spot.
        /// </summary>
        [Optional]
        public List<ChargingStation> ChargingStations
        {
            get
            {
                return _ChargingStations;
            }
        }

        #endregion

        // status := free, ocupied, reserved, not accessible

        // fee := double

        // fee unit := "€/h"

        // Opening hours

        // restrictions := EV only, must be plugged in, disabled persons only

        #endregion

        #region Events

        #endregion

        #region Constructor(s)

        #region (internal) ParkingSpot()

        /// <summary>
        /// Create a new electric vehicle parking spot having a random identification.
        /// </summary>
        internal ParkingSpot()
            : this(ParkingSpot_Id.New)
        { }

        #endregion

        #region (internal) ParkingSpot(Id)

        /// <summary>
        /// Create a new electric vehicle parking spot having the given identification.
        /// </summary>
        /// <param name="Id">The unique identification of the parking spot.</param>
        internal ParkingSpot(ParkingSpot_Id  Id)
            : base(Id)
        {

            #region Initial checks

            if (Id == null)
                throw new ArgumentNullException(nameof(Id), "The unique identification of the parking spot must not be null!");

            #endregion

            #region Init data and properties

            this._Name              = new I18NString(Languages.en, Id.ToString());
            this._Description       = new I18NString();
            this._Geometry          = new List<GeoCoordinate>();
            this._ChargingStations  = new List<ChargingStation>();

            #endregion

        }

        #endregion

        #endregion


        #region IComparable<ParkingSpot> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="Object">An object to compare with.</param>
        public Int32 CompareTo(Object Object)
        {

            if (Object == null)
                throw new ArgumentNullException("The given object must not be null!");

            // Check if the given object is a service plan.
            var ServicePlan = Object as ParkingSpot;
            if ((Object) ServicePlan == null)
                throw new ArgumentException("The given object is not a service plan!");

            return CompareTo(ServicePlan);

        }

        #endregion

        #region CompareTo(ParkingSpot)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParkingSpot">A service plan object to compare with.</param>
        public Int32 CompareTo(ParkingSpot ParkingSpot)
        {

            if ((Object) ParkingSpot == null)
                throw new ArgumentNullException("The given service plan must not be null!");

            return Id.CompareTo(ParkingSpot.Id);

        }

        #endregion

        #endregion

        #region IEquatable<ParkingSpot> Members

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

            // Check if the given object is a service plan.
            var ParkingSpot = Object as ParkingSpot;
            if ((Object) ParkingSpot == null)
                return false;

            return this.Equals(ParkingSpot);

        }

        #endregion

        #region Equals(ParkingSpot)

        /// <summary>
        /// Compares two service plans for equality.
        /// </summary>
        /// <param name="ParkingSpot">A service plan to compare with.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public Boolean Equals(ParkingSpot ParkingSpot)
        {

            if ((Object) ParkingSpot == null)
                return false;

            return Id.Equals(ParkingSpot.Id);

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
            return "eMI3 charging service plan: " + Id.ToString();
        }

        #endregion

    }

}
