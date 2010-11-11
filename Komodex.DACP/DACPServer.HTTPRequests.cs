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
using System.Windows.Media.Imaging;

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
        protected HttpWebRequest SubmitHTTPRequest(string url, AsyncCallback callback = null)
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

                return webRequest;
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

                Utility.DebugWrite("Got HTTP response for: " + requestInfo.WebRequest.RequestUri);

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

                requestInfo.ResponseCode = parsedResponse.Key;
                requestInfo.ResponseBody = parsedResponse.Value;

                // Determine the type of response
                switch (requestInfo.ResponseCode)
                {
                    case "mlog": // Login response
                        ProcessLoginResponse(requestInfo);
                        break;
                    case "cmst": // Play status response
                        ProcessPlayStatusRequest(requestInfo);
                        break;
                    default:
                        break;
                }
            }
            catch (WebException e)
            {
                Utility.DebugWrite("Caught exception: " + e.Message);
                // TODO: Signal the main application
                // Just exiting for now
                throw e;
            }
        }

        protected void HTTPImageCallback(IAsyncResult result)
        {
            try
            {
                HTTPRequestInfo requestInfo = (HTTPRequestInfo)result.AsyncState;

                WebResponse response = requestInfo.WebRequest.EndGetResponse(result);
                Stream responseStream = response.GetResponseStream();

                BitmapImage image;
                
                // TODO: Find a better way
                // BitmapImage objects need to be created in the UI thread
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    image = new BitmapImage();
                    image.SetSource(responseStream);
                    // For now, going to assume this is for the album art
                    CurrentAlbumArt = image;
                });
            }
            catch { }
        }

        #endregion

        #region Requests and Responses

        #region Login

        protected void SubmitLoginRequest()
        {
            string url = "/login?pairing-guid=0x" + PairingKey;
            SubmitHTTPRequest(url);
        }

        protected void ProcessLoginResponse(HTTPRequestInfo requestInfo)
        {
            foreach (var kvp in requestInfo.ResponseNodes)
            {
                if (kvp.Key == "mlid")
                {
                    SessionID = kvp.Value.GetInt32Value();
                    break;
                }
            }
            SubmitPlayStatusRequest();
        }

        #endregion

        #region Play Status

        protected int playStatusRevisionNumber = 1;
        protected HttpWebRequest playStatusWebRequest = null;

        protected void SubmitPlayStatusRequest()
        {
            if (playStatusWebRequest != null)
                playStatusWebRequest.Abort();

            string url = "/ctrl-int/1/playstatusupdate?revision-number=" + playStatusRevisionNumber + "&session-id=" + SessionID;
            playStatusWebRequest = SubmitHTTPRequest(url);
        }

        protected void ProcessPlayStatusRequest(HTTPRequestInfo requestInfo)
        {
            foreach (var kvp in requestInfo.ResponseNodes)
            {
                switch (kvp.Key)
                {
                    case "cmsr": // Revision number
                        playStatusRevisionNumber = kvp.Value.GetInt32Value();
                        break;
                    case "cann": // Song name
                        CurrentSongName = kvp.Value.GetStringValue();
                        break;
                    case "cana": // Artist
                        CurrentArtist = kvp.Value.GetStringValue();
                        break;
                    case "canl": // Album
                        CurrentAlbum = kvp.Value.GetStringValue();
                        break;
                    case "caps": // Play status
                        PlayStatus = (PlayStatuses)kvp.Value[0];
                        break;
                    case "cash": // Shuffle status
                        ShuffleStatus = !(kvp.Value[0] == 0);
                        break;
                    case "carp": // Repeat status
                        RepeatStatus = (RepeatStatuses)kvp.Value[0];
                        break;
                    default:
                        break;
                }
            }

            SubmitAlbumArtRequest();
            SubmitPlayStatusRequest();
        }

        #endregion

        #region Album Art

        protected void SubmitAlbumArtRequest()
        {
            //string url = "/ctrl-int/1/nowplayingartwork?mw=320&mh=320&session-id=" + SessionID;
            string url = "/ctrl-int/1/nowplayingartwork?session-id=" + SessionID;
            SubmitHTTPRequest(url, new AsyncCallback(HTTPImageCallback));
        }

        #endregion

        #endregion

    }
}
