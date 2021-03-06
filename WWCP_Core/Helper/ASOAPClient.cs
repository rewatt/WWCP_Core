﻿/*
 * Copyright (c) 2014-2016 GraphDefined GmbH
 * This file is part of WWCP OICP <https://github.com/GraphDefined/WWCP_OICP>
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
using System.Xml.Linq;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.DNS;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace org.GraphDefined.WWCP
{

    /// <summary>
    /// An abstract base class for all SOAP clients.
    /// </summary>
    public abstract class ASOAPClient : IDisposable
    {

        #region Data

        /// <summary>
        /// The default timeout for upstream queries.
        /// </summary>
        public static readonly TimeSpan DefaultQueryTimeout  = TimeSpan.FromSeconds(180);

        #endregion

        #region Properties

        #region ClientId

        protected readonly String _ClientId;

        /// <summary>
        /// A unqiue identification of this client.
        /// </summary>
        public String ClientId
        {
            get
            {
                return _ClientId;
            }
        }

        #endregion

        #region Hostname

        protected readonly String _Hostname;

        public String Hostname
        {
            get
            {
                return _Hostname;
            }
        }

        #endregion

        #region TCPPort

        protected readonly IPPort _TCPPort;

        public IPPort TCPPort
        {
            get
            {
                return _TCPPort;
            }
        }

        #endregion

        #region HTTPVirtualHost

        protected readonly String _HTTPVirtualHost;

        public String HTTPVirtualHost
        {
            get
            {
                return _HTTPVirtualHost;
            }
        }

        #endregion

        #region UserAgent

        protected readonly String _UserAgent;

        public String UserAgent
        {
            get
            {
                return _UserAgent;
            }
        }

        #endregion

        #region QueryTimeout

        protected readonly TimeSpan _QueryTimeout;

        /// <summary>
        /// The timeout for upstream queries.
        /// </summary>
        public TimeSpan QueryTimeout
        {
            get
            {
                return _QueryTimeout;
            }
        }

        #endregion

        #region DNSClient

        protected readonly DNSClient _DNSClient;

        /// <summary>
        /// The DNS client defines which DNS servers to use.
        /// </summary>
        public DNSClient DNSClient
        {
            get
            {
                return _DNSClient;
            }
        }

        #endregion

        public X509Certificate ClientCert { get; set; }

        public X509Certificate2 ServerCert { get; set; }

        #region RemoteCertificateValidator

        protected readonly RemoteCertificateValidationCallback _RemoteCertificateValidator;

        public RemoteCertificateValidationCallback RemoteCertificateValidator
        {
            get
            {
                return _RemoteCertificateValidator;
            }
        }

        #endregion

        public LocalCertificateSelectionCallback ClientCertificateSelector { get; set; }

        public Boolean UseTLS { get; set; }

        #endregion

        #region Events

        #region OnException

        /// <summary>
        /// An event fired whenever an exception occured.
        /// </summary>
        public event OnExceptionDelegate OnException;

        #endregion

        #region OnHTTPError

        /// <summary>
        /// A delegate called whenever a HTTP error occured.
        /// </summary>
        public delegate void OnHTTPErrorDelegate(DateTime Timestamp, Object Sender, HTTPResponse HttpResponse);

        /// <summary>
        /// An event fired whenever a HTTP error occured.
        /// </summary>
        public event OnHTTPErrorDelegate OnHTTPError;

        #endregion

        #region OnSOAPError

        /// <summary>
        /// A delegate called whenever a SOAP error occured.
        /// </summary>
        public delegate void OnSOAPErrorDelegate(DateTime Timestamp, Object Sender, XElement SOAPXML);

        /// <summary>
        /// An event fired whenever a SOAP error occured.
        /// </summary>
        public event OnSOAPErrorDelegate OnSOAPError;

        #endregion

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create an abstract OICP v2.0 client.
        /// </summary>
        /// <param name="ClientId">A unqiue identification of this client.</param>
        /// <param name="Hostname">The OICP hostname to connect to.</param>
        /// <param name="TCPPort">The OICP TCP port to connect to.</param>
        /// <param name="HTTPVirtualHost">An optional HTTP virtual host name to use.</param>
        /// <param name="RemoteCertificateValidator">A delegate to verify the remote TLS certificate.</param>
        /// <param name="UserAgent">An optional HTTP user agent to use.</param>
        /// <param name="QueryTimeout">An optional timeout for upstream queries.</param>
        /// <param name="DNSClient">An optional DNS client.</param>
        public ASOAPClient(String                               ClientId,
                           String                               Hostname,
                           IPPort                               TCPPort,
                           String                               HTTPVirtualHost             = null,
                           RemoteCertificateValidationCallback  RemoteCertificateValidator  = null,
                           String                               UserAgent                   = "GraphDefined eMobility",
                           TimeSpan?                            QueryTimeout                = null,
                           DNSClient                            DNSClient                   = null)
        {

            #region Initial checks

            if (Hostname.IsNullOrEmpty())
                throw new ArgumentNullException(nameof(Hostname), "The given parameter must not be null or empty!");

            if (TCPPort == null)
                throw new ArgumentNullException(nameof(TCPPort),  "The given parameter must not be null!");

            #endregion

            this._Hostname         = Hostname;
            this._TCPPort          = TCPPort;

            this._HTTPVirtualHost  = (HTTPVirtualHost != null)
                                         ? HTTPVirtualHost
                                         : Hostname;

            this._RemoteCertificateValidator = RemoteCertificateValidator;

            this._UserAgent        = UserAgent;

            this._QueryTimeout     = QueryTimeout != null
                                         ? QueryTimeout.Value
                                         : DefaultQueryTimeout;

            this._DNSClient        = (DNSClient == null)
                                         ? new DNSClient()
                                         : DNSClient;

        }

        #endregion


        #region (protected) SendSOAPError(Timestamp, Sender, SOAPXML)

        /// <summary>
        /// Notify that an HTTP error occured.
        /// </summary>
        /// <param name="Timestamp">The timestamp of the error received.</param>
        /// <param name="Sender">The sender of this error message.</param>
        /// <param name="SOAPXML">The SOAP fault/error.</param>
        protected void SendSOAPError(DateTime  Timestamp,
                                     Object    Sender,
                                     XElement  SOAPXML)
        {

            DebugX.Log("AOICPUpstreamService => SOAP Fault: " + SOAPXML != null ? SOAPXML.ToString() : "<null>");

            var OnSOAPErrorLocal = OnSOAPError;
            if (OnSOAPErrorLocal != null)
                OnSOAPErrorLocal(Timestamp, Sender, SOAPXML);

        }

        #endregion

        #region (protected) SendHTTPError(Timestamp, Sender, HttpResponse)

        /// <summary>
        /// Notify that an HTTP error occured.
        /// </summary>
        /// <param name="Timestamp">The timestamp of the error received.</param>
        /// <param name="Sender">The sender of this error message.</param>
        /// <param name="HttpResponse">The HTTP response related to this error message.</param>
        protected void SendHTTPError(DateTime      Timestamp,
                                     Object        Sender,
                                     HTTPResponse  HttpResponse)
        {

            DebugX.Log("AOICPUpstreamService => HTTP Status Code: " + HttpResponse != null ? HttpResponse.HTTPStatusCode.ToString() : "<null>");

            var OnHTTPErrorLocal = OnHTTPError;
            if (OnHTTPErrorLocal != null)
                OnHTTPErrorLocal(Timestamp, Sender, HttpResponse);

        }

        #endregion

        #region (protected) SendException(Timestamp, Sender, Exception)

        /// <summary>
        /// Notify that an exception occured.
        /// </summary>
        /// <param name="Timestamp">The timestamp of the exception.</param>
        /// <param name="Sender">The sender of this exception.</param>
        /// <param name="Exception">The exception itself.</param>
        protected void SendException(DateTime   Timestamp,
                                     Object     Sender,
                                     Exception  Exception)
        {

            DebugX.Log("AOICPUpstreamService => Exception: " + Exception.Message);

            var OnExceptionLocal = OnException;
            if (OnExceptionLocal != null)
                OnExceptionLocal(Timestamp, Sender, Exception);

        }

        #endregion


        #region IsHubjectError(XML)

        //protected Boolean IsHubjectError(XElement                             XML,
        //                                 out OICPException                    OICPException,
        //                                 Action<DateTime, Object, Exception>  OnError)
        //{

        //    #region Initial checks

        //    if (OnError == null)
        //        throw new ArgumentNullException("The given OnError-delegate must not be null!");

        //    #endregion

        //    StatusCode _StatusCode = null;

        //    if (StatusCode.TryParse(XML, out _StatusCode))
        //    {
        //        OICPException = new OICPException(_StatusCode);
        //        OnError(DateTime.Now, XML, OICPException);
        //        return true;
        //    }

        //    OICPException = null;

        //    return false;

        //}

        #endregion


        #region Dispose()

        /// <summary>
        /// Dispose this object.
        /// </summary>
        public virtual void Dispose()
        { }

        #endregion

    }

}
