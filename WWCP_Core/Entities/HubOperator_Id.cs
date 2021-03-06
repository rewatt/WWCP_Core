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

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace org.GraphDefined.WWCP
{

    /// <summary>
    /// The unique identification of an e-mobility hub operator.
    /// </summary>
    public class HubOperator_Id : IId,
                                  IEquatable<HubOperator_Id>,
                                  IComparable<HubOperator_Id>

    {

        #region Data

        /// <summary>
        /// The internal identification.
        /// </summary>
        protected readonly String _Id;

        #endregion

        #region Properties

        #region New

        /// <summary>
        /// Generate a new unique identification of an Electric Vehicle Roaming Network (EVRN Id).
        /// </summary>
        public static HubOperator_Id New
        {
            get
            {
                return new HubOperator_Id(Guid.NewGuid().ToString());
            }
        }

        #endregion

        #region Length

        /// <summary>
        /// Returns the length of the identificator.
        /// </summary>
        public UInt64 Length
        {
            get
            {
                return (UInt64) _Id.Length;
            }
        }

        #endregion

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Generate a new e-mobility hub operator identification.
        /// based on the given string.
        /// </summary>
        private HubOperator_Id(String Text)
        {

            #region Initial checks

            if (Text.IsNullOrEmpty())
                throw new ArgumentException("The parameter must not be null or empty!", "Text");

            #endregion

            _Id = Text.Trim();

        }

        #endregion


        #region Parse(Text)

        /// <summary>
        /// Parse the given string as an Electric Vehicle Roaming Network (EVRN Id).
        /// </summary>
        /// <param name="Text">A text representation of an Electric Vehicle Roaming Network identification.</param>
        public static HubOperator_Id Parse(String Text)
        {
            return new HubOperator_Id(Text);
        }

        #endregion

        #region TryParse(Text, out RoamingNetworkId)

        /// <summary>
        /// Parse the given string as an Electric Vehicle Roaming Network (EVRN Id).
        /// </summary>
        /// <param name="Text">A text representation of an Electric Vehicle Roaming Network identification.</param>
        /// <param name="RoamingNetworkId">The parsed Electric Vehicle Roaming Network identification.</param>
        public static Boolean TryParse(String Text, out HubOperator_Id RoamingNetworkId)
        {

            #region Initial checks

            if (Text.IsNullOrEmpty())
            {
                RoamingNetworkId = null;
                return false;
            }

            #endregion

            try
            {
                RoamingNetworkId = new HubOperator_Id(Text);
                return true;
            }
            catch (Exception)
            {
                RoamingNetworkId = null;
                return false;
            }

        }

        #endregion

        #region Clone

        /// <summary>
        /// Clone this Electric Vehicle Roaming Network identification.
        /// </summary>
        public HubOperator_Id Clone
        {
            get
            {
                return new HubOperator_Id(new String(_Id.ToCharArray()));
            }
        }

        #endregion


        #region Operator overloading

        #region Operator == (RoamingNetworkId1, RoamingNetworkId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="RoamingNetworkId1">A HubOperator_Id.</param>
        /// <param name="RoamingNetworkId2">Another HubOperator_Id.</param>
        /// <returns>true|false</returns>
        public static Boolean operator == (HubOperator_Id RoamingNetworkId1, HubOperator_Id RoamingNetworkId2)
        {

            // If both are null, or both are same instance, return true.
            if (Object.ReferenceEquals(RoamingNetworkId1, RoamingNetworkId2))
                return true;

            // If one is null, but not both, return false.
            if (((Object) RoamingNetworkId1 == null) || ((Object) RoamingNetworkId2 == null))
                return false;

            return RoamingNetworkId1.Equals(RoamingNetworkId2);

        }

        #endregion

        #region Operator != (RoamingNetworkId1, RoamingNetworkId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="RoamingNetworkId1">A HubOperator_Id.</param>
        /// <param name="RoamingNetworkId2">Another HubOperator_Id.</param>
        /// <returns>true|false</returns>
        public static Boolean operator != (HubOperator_Id RoamingNetworkId1, HubOperator_Id RoamingNetworkId2)
        {
            return !(RoamingNetworkId1 == RoamingNetworkId2);
        }

        #endregion

        #region Operator <  (RoamingNetworkId1, RoamingNetworkId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="RoamingNetworkId1">A HubOperator_Id.</param>
        /// <param name="RoamingNetworkId2">Another HubOperator_Id.</param>
        /// <returns>true|false</returns>
        public static Boolean operator < (HubOperator_Id RoamingNetworkId1, HubOperator_Id RoamingNetworkId2)
        {

            if ((Object) RoamingNetworkId1 == null)
                throw new ArgumentNullException("The given RoamingNetworkId1 must not be null!");

            return RoamingNetworkId1.CompareTo(RoamingNetworkId2) < 0;

        }

        #endregion

        #region Operator <= (RoamingNetworkId1, RoamingNetworkId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="RoamingNetworkId1">A HubOperator_Id.</param>
        /// <param name="RoamingNetworkId2">Another HubOperator_Id.</param>
        /// <returns>true|false</returns>
        public static Boolean operator <= (HubOperator_Id RoamingNetworkId1, HubOperator_Id RoamingNetworkId2)
        {
            return !(RoamingNetworkId1 > RoamingNetworkId2);
        }

        #endregion

        #region Operator >  (RoamingNetworkId1, RoamingNetworkId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="RoamingNetworkId1">A HubOperator_Id.</param>
        /// <param name="RoamingNetworkId2">Another HubOperator_Id.</param>
        /// <returns>true|false</returns>
        public static Boolean operator > (HubOperator_Id RoamingNetworkId1, HubOperator_Id RoamingNetworkId2)
        {

            if ((Object) RoamingNetworkId1 == null)
                throw new ArgumentNullException("The given RoamingNetworkId1 must not be null!");

            return RoamingNetworkId1.CompareTo(RoamingNetworkId2) > 0;

        }

        #endregion

        #region Operator >= (RoamingNetworkId1, RoamingNetworkId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="RoamingNetworkId1">A HubOperator_Id.</param>
        /// <param name="RoamingNetworkId2">Another HubOperator_Id.</param>
        /// <returns>true|false</returns>
        public static Boolean operator >= (HubOperator_Id RoamingNetworkId1, HubOperator_Id RoamingNetworkId2)
        {
            return !(RoamingNetworkId1 < RoamingNetworkId2);
        }

        #endregion

        #endregion

        #region IComparable<HubOperator_Id> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="Object">An object to compare with.</param>
        public Int32 CompareTo(Object Object)
        {

            if (Object == null)
                throw new ArgumentNullException("The given object must not be null!");

            // Check if the given object is an RoamingNetworkId.
            var RoamingNetworkId = Object as HubOperator_Id;
            if ((Object) RoamingNetworkId == null)
                throw new ArgumentException("The given object is not a RoamingNetworkId!");

            return CompareTo(RoamingNetworkId);

        }

        #endregion

        #region CompareTo(RoamingNetworkId)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="RoamingNetworkId">An object to compare with.</param>
        public Int32 CompareTo(HubOperator_Id RoamingNetworkId)
        {

            if ((Object) RoamingNetworkId == null)
                throw new ArgumentNullException("The given RoamingNetworkId must not be null!");

            return _Id.CompareTo(RoamingNetworkId._Id);

        }

        #endregion

        #endregion

        #region IEquatable<HubOperator_Id> Members

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

            // Check if the given object is an RoamingNetworkId.
            var RoamingNetworkId = Object as HubOperator_Id;
            if ((Object) RoamingNetworkId == null)
                return false;

            return this.Equals(RoamingNetworkId);

        }

        #endregion

        #region Equals(RoamingNetworkId)

        /// <summary>
        /// Compares two RoamingNetworkIds for equality.
        /// </summary>
        /// <param name="RoamingNetworkId">A RoamingNetworkId to compare with.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public Boolean Equals(HubOperator_Id RoamingNetworkId)
        {

            if ((Object) RoamingNetworkId == null)
                return false;

            return _Id.Equals(RoamingNetworkId._Id);

        }

        #endregion

        #endregion

        #region GetHashCode()

        /// <summary>
        /// Return the HashCode of this object.
        /// </summary>
        /// <returns>The HashCode of this object.</returns>
        public override Int32 GetHashCode()
        {
            return _Id.GetHashCode();
        }

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a string representation of this object.
        /// </summary>
        public override String ToString()
        {
            return _Id.ToString();
        }

        #endregion

    }

}
