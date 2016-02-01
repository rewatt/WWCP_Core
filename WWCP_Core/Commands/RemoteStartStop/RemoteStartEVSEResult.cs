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

#endregion

namespace org.GraphDefined.WWCP
{

    /// <summary>
    /// The result of a remote start operation at an EVSE.
    /// </summary>
    public class RemoteStartEVSEResult
    {

        #region Properties

        #region Result

        private readonly RemoteStartEVSEResultType _Result;

        /// <summary>
        /// The result of a remote start operation.
        /// </summary>
        public RemoteStartEVSEResultType Result
        {
            get
            {
                return _Result;
            }
        }

        #endregion

        #region Session

        private readonly ChargingSession _Session;

        /// <summary>
        /// The charging session for the remote start operation.
        /// </summary>
        public ChargingSession Session
        {
            get
            {
                return _Session;
            }
        }

        #endregion

        #region ErrorMessage

        private readonly String _ErrorMessage;

        /// <summary>
        /// An optional error message.
        /// </summary>
        public String ErrorMessage
        {
            get
            {
                return _ErrorMessage;
            }
        }

        #endregion

        #endregion

        #region Constructor(s)

        #region (private) RemoteStartResult(Result)

        /// <summary>
        /// Create a new remote start result.
        /// </summary>
        /// <param name="Result">The result of the remote start operation.</param>
        private RemoteStartEVSEResult(RemoteStartEVSEResultType  Result)
        {

            if (Result == RemoteStartEVSEResultType.Success ||
                Result == RemoteStartEVSEResultType.Error)
                throw new ArgumentException("Invalid parameter!");

            this._Result        = Result;
            this._Session       = null;
            this._ErrorMessage  = null;

        }

        #endregion

        #region (private) RemoteStartResult(Session)

        /// <summary>
        /// Create a new successful remote start result.
        /// </summary>
        /// <param name="Session">The charging session (mandatory for successful session starts).</param>
        private RemoteStartEVSEResult(ChargingSession  Session)
        {

            this._Result        = RemoteStartEVSEResultType.Success;
            this._Session       = Session;
            this._ErrorMessage  = null;

        }

        #endregion

        #region (private) RemoteStartResult(ErrorMessage = null)

        /// <summary>
        /// Create a new remote start result.
        /// </summary>
        /// <param name="ErrorMessage">An error message.</param>
        private RemoteStartEVSEResult(String ErrorMessage = null)
        {

            this._Result        = RemoteStartEVSEResultType.Error;
            this._ErrorMessage  = ErrorMessage != null ? ErrorMessage : String.Empty;

        }

        #endregion

        #endregion


        #region (static) Unspecified

        /// <summary>
        /// The result is unknown and/or should be ignored.
        /// </summary>
        public static RemoteStartEVSEResult Unspecified
        {
            get
            {
                return new RemoteStartEVSEResult(RemoteStartEVSEResultType.Unspecified);
            }
        }

        #endregion

        #region (static) UnknownEVSEOperator

        /// <summary>
        /// The EVSE operator is unknown.
        /// </summary>
        public static RemoteStartEVSEResult UnknownEVSEOperator
        {
            get
            {
                return new RemoteStartEVSEResult(RemoteStartEVSEResultType.UnknownEVSEOperator);
            }
        }

        #endregion

        #region (static) UnknownEVSE

        /// <summary>
        /// The EVSE is unknown.
        /// </summary>
        public static RemoteStartEVSEResult UnknownEVSE
        {
            get
            {
                return new RemoteStartEVSEResult(RemoteStartEVSEResultType.UnknownEVSE);
            }
        }

        #endregion

        #region (static) InvalidSessionId

        /// <summary>
        /// The given charging session identification is unknown or invalid.
        /// </summary>
        public static RemoteStartEVSEResult InvalidSessionId
        {
            get
            {
                return new RemoteStartEVSEResult(RemoteStartEVSEResultType.InvalidSessionId);
            }
        }

        #endregion

        #region (static) AlreadyInUse

        /// <summary>
        /// The EVSE is already in use.
        /// </summary>
        public static RemoteStartEVSEResult AlreadyInUse
        {
            get
            {
                return new RemoteStartEVSEResult(RemoteStartEVSEResultType.AlreadyInUse);
            }
        }

        #endregion

        #region (static) Reserved

        /// <summary>
        /// The EVSE is reserved.
        /// </summary>
        public static RemoteStartEVSEResult Reserved
        {
            get
            {
                return new RemoteStartEVSEResult(RemoteStartEVSEResultType.Reserved);
            }
        }

        #endregion

        #region (static) OutOfService

        /// <summary>
        /// The EVSE is out of service.
        /// </summary>
        public static RemoteStartEVSEResult OutOfService
        {
            get
            {
                return new RemoteStartEVSEResult(RemoteStartEVSEResultType.OutOfService);
            }
        }

        #endregion

        #region (static) Offline

        /// <summary>
        /// The EVSE is offline.
        /// </summary>
        public static RemoteStartEVSEResult Offline
        {
            get
            {
                return new RemoteStartEVSEResult(RemoteStartEVSEResultType.Offline);
            }
        }

        #endregion

        #region (static) Success(Session)

        /// <summary>
        /// The remote start was successful.
        /// </summary>
        /// <param name="Session">The charging session.</param>
        public static RemoteStartEVSEResult Success(ChargingSession Session)
        {

            #region Initial checks

            if (Session == null)
                throw new ArgumentNullException("Session", "The given charging session must not be null!");

            #endregion

            return new RemoteStartEVSEResult(Session);

        }

        #endregion

        #region (static) Timeout

        /// <summary>
        /// The remote stop ran into a timeout.
        /// </summary>
        public static RemoteStartEVSEResult Timeout
        {
            get
            {
                return new RemoteStartEVSEResult(RemoteStartEVSEResultType.Timeout);
            }
        }

        #endregion

        #region (static) Error(ErrorMessage = null)

        /// <summary>
        /// The remote stop led to an error.
        /// </summary>
        /// <param name="ErrorMessage">An error message.</param>
        public static RemoteStartEVSEResult Error(String ErrorMessage = null)
        {
            return new RemoteStartEVSEResult(ErrorMessage);
        }

        #endregion


        #region (override) ToString()

        /// <summary>
        /// Return a string representation of this object.
        /// </summary>
        public override String ToString()
        {
            return _Result.ToString();
        }

        #endregion

    }


    /// <summary>
    /// The result types of a remote start operation at an EVSE.
    /// </summary>
    public enum RemoteStartEVSEResultType
    {

        /// <summary>
        /// The result is unknown and/or should be ignored.
        /// </summary>
        Unspecified,

        /// <summary>
        /// The EVSE operator is unknown.
        /// </summary>
        UnknownEVSEOperator,

        /// <summary>
        /// The EVSE is unknown.
        /// </summary>
        UnknownEVSE,

        /// <summary>
        /// The given charging session identification is unknown or invalid.
        /// </summary>
        InvalidSessionId,

        /// <summary>
        /// The EVSE is already in use.
        /// </summary>
        AlreadyInUse,

        /// <summary>
        /// The EVSE is reserved.
        /// </summary>
        Reserved,

        /// <summary>
        /// The EVSE is out of service.
        /// </summary>
        OutOfService,

        /// <summary>
        /// The EVSE is offline.
        /// </summary>
        Offline,

        /// <summary>
        /// The remote stop was successful.
        /// </summary>
        Success,

        /// <summary>
        /// The remote stop ran into a timeout.
        /// </summary>
        Timeout,

        /// <summary>
        /// The remote stop led to an error.
        /// </summary>
        Error

    }

}
