using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Komodex.DACP
{
    public class DACPServer
    {
        private DACPServer() { }

        public DACPServer(string hostName, string pairingKey)
        {
            HostName = hostName;
            PairingKey = pairingKey;
        }

        #region Fields


        #endregion

        #region Properties

        private string _HostName = null;
        public string HostName
        {
            get { return _HostName; }
            protected set
            {
                _HostName = value;
                _HTTPPrefix = null;
            }
        }

        public string PairingKey { get; protected set; }

        private string _HTTPPrefix = null;
        protected string HTTPPrefix
        {
            get
            {
                if (string.IsNullOrEmpty(_HTTPPrefix))
                    _HTTPPrefix = "http://" + HostName + ":3689";
                return _HTTPPrefix;
            }
        }


        #endregion

        #region Public Methods

        public void Connect()
        {

        }

        #endregion

        #region HTTP Management

        /// <summary>
        /// Submits a HTTP request to the DACP server
        /// </summary>
        /// <param name="url">The URL request (e.g., "/server-info").</param>
        /// <param name="callback">If no callback is specified, the default HTTPByteCallback will be used.</param>
        protected void SubmitHTTPRequest(string url, AsyncCallback callback = null)
        {
            // Set up callback if none was specified
            if (callback == null)
                callback = new AsyncCallback(HTTPByteCallback);

            // Set up HTTPWebRequest
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(HTTPPrefix + url);
            webRequest.Method = "POST";
            webRequest.Headers["Viewer-Only-Client"] = "1";

            // Send HTTP request
            webRequest.BeginGetResponse(callback, webRequest);
        }

        protected void HTTPByteCallback(IAsyncResult result)
        {

        }

        protected void HTTPImageCallback(IAsyncResult result)
        {

        }

        #endregion

        #region Static Methods


        #endregion
    }
}
