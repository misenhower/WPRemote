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
using System.IO;
using System.Linq;

namespace Komodex.DACP
{
    public partial class DACPServer
    {
        #region HTTP Management

        /// <summary>
        /// Submits a HTTP request to the DACP server
        /// </summary>
        /// <param name="url">The URL request (e.g., "/server-info").</param>
        /// <param name="callback">If no callback is specified, the default HTTPByteCallback will be used.</param>
        protected void SubmitHTTPRequest(string url, AsyncCallback callback = null)
        {
            Utility.DebugWrite("Submitting HTTP request for: " + url);

            // Set up callback if none was specified
            if (callback == null)
                callback = new AsyncCallback(HTTPByteCallback);

            try
            {
                // Set up HTTPWebRequest
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(HTTPPrefix + url);
                webRequest.Method = "POST";
                webRequest.Headers["Viewer-Only-Client"] = "1";

                // Create a new HTTPRequestState object
                HTTPRequestInfo requestInfo = new HTTPRequestInfo(webRequest);

                // Send HTTP request
                webRequest.BeginGetResponse(callback, requestInfo);
            }
            catch (Exception e)
            {
                Utility.DebugWrite("Caught exception: " + e.Message);
                // TODO: Signal the main application
                // Just exiting for now
                throw e;
            }
        }

        protected void HTTPByteCallback(IAsyncResult result)
        {
            try
            {
                // Get the HTTPRequestState object
                HTTPRequestInfo requestInfo = (HTTPRequestInfo)result.AsyncState;

                WebResponse response = requestInfo.WebRequest.EndGetResponse(result);
                Stream responseStream = response.GetResponseStream();
                BinaryReader br = new BinaryReader(responseStream);
                MemoryStream data = new MemoryStream();
                byte[] buffer;

                do
                {
                    buffer = br.ReadBytes(8192);
                    data.Write(buffer, 0, buffer.Length);
                } while (buffer.Length > 0);

                data.Flush();

                byte[] byteResult = data.GetBuffer();

                var parsedResponse = Utility.GetResponseNodes(byteResult, true)[0];

                string responseType = parsedResponse.Key;
                requestInfo.ResponseBody = parsedResponse.Value;

                // Determine the type of response
                switch (responseType)
                {
                    case "mlog": // Login response
                        ProcessLoginResponse(requestInfo);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception e)
            {
                Utility.DebugWrite("Caught exception: " + e.Message);
                // TODO: Signal the main application
                // Just exiting for now
                throw e;
            }
        }

        protected void HTTPImageCallback(IAsyncResult result)
        {

        }

        #endregion

        #region Requests and Responses

        #region Login

        protected void SubmitLoginRequest()
        {
            string url = "/login?pairing-guid=0x" + PairingKey;
            SubmitHTTPRequest(url);
        }

        protected void ProcessLoginResponse(HTTPRequestInfo requestState)
        {
            foreach (var kvp in requestState.ResponseNodes)
            {
                if (kvp.Key == "mlid")
                    SessionID = kvp.Value.GetInt32Value();
            }
        }

        #endregion

        #region Play Status

        protected void SubmitPlayStatusRequest()
        {

        }

        protected void ProcessPlayStatusRequest(byte[] responseBody)
        {

        }

        #endregion

        #endregion

    }
}
