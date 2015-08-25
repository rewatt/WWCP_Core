﻿/*
 * Copyright (c) 2014-2015 GraphDefined GmbH
 * This file is part of WWCP Core <https://github.com/WorldWideCharging/WWCP_Core>
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
    /// The type of plugs.
    /// </summary>
    public enum PlugTypes
    {

        Unspecified,
        SmallPaddleInductive,
        LargePaddleInductive,
        AVCONConnector,
        TeslaConnector,
        NEMA5_20,
        TypeEFrenchStandard,
        TypeFSchuko,
        TypeGBritishStandard,
        TypeJSwissStandard,
        Type1Connector_CableAttached,
        Type2Outlet,
        Type2Connector_CableAttached,
        Type3Outlet,
        IEC60309SinglePhase,
        IEC60309ThreePhase,
        CCSCombo2Plug_CableAttached,
        CCSCombo1Plug_CableAttached,
        CHAdeMO_DC_CHAdeMOConnector

    }

}