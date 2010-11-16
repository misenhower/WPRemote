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
                SendServerUpdate(ServerUpdateType.Error);
                return null;
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

                var parsedResponse = Utility.GetResponseNodes(byteResult, true);
                if (parsedResponse.Count == 0)
                    return;

                var parsedResponseNode = parsedResponse[0];

                requestInfo.ResponseCode = parsedResponseNode.Key;
                requestInfo.ResponseBody = parsedResponseNode.Value;

                // Determine the type of response
                switch (requestInfo.ResponseCode)
                {
                    case "msrv":
                        ProcessServerInfoResponse(requestInfo);
                        break;
                    case "mlog": // Login response
                        ProcessLoginResponse(requestInfo);
                        break;
                    case "cmst": // Play status response
                        ProcessPlayStatusResponse(requestInfo);
                        break;
                    case "cmgt": // Volume status response (TODO: and maybe others?)
                        ProcessVolumeStatusResponse(requestInfo);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception e)
            {
                Utility.DebugWrite("Caught exception: " + e.Message);
                SendServerUpdate(ServerUpdateType.Error);
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

        #region Server Info

        protected void SubmitServerInfoRequest()
        {
            string url = "/server-info";
            SubmitHTTPRequest(url);
        }

        protected void ProcessServerInfoResponse(HTTPRequestInfo requestInfo)
        {
            foreach (var kvp in requestInfo.ResponseNodes)
            {
                switch (kvp.Key)
                {
                    case "minm": // Library name
                        LibraryName = kvp.Value.GetStringValue();
                        break;
                    default:
                        break;
                }
            }

            SendServerUpdate(ServerUpdateType.ServerInfoResponse);
        }

        #endregion

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

            SendServerUpdate(ServerUpdateType.ServerConnected);
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

        protected void ProcessPlayStatusResponse(HTTPRequestInfo requestInfo)
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
                        PlayState = (PlayStates)kvp.Value[0];
                        break;
                    case "cash": // Shuffle status
                        ShuffleState = !(kvp.Value[0] == 0);
                        break;
                    case "carp": // Repeat status
                        RepeatState = (RepeatStates)kvp.Value[0];
                        break;
                    default:
                        break;
                }
            }

            SubmitAlbumArtRequest(); // TODO: Need to be a bit more efficient about this, perhaps by doing this in the PropertyChanged event
            SubmitVolumeStatusRequest();
            if (UseDelayedResponseRequests)
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

        #region Volume Status

        protected void SubmitVolumeStatusRequest()
        {
            string url = "/ctrl-int/1/getproperty?properties=dmcp.volume&session-id=" + SessionID;
            SubmitHTTPRequest(url);
        }

        protected void ProcessVolumeStatusResponse(HTTPRequestInfo requestInfo)
        {
            foreach (var kvp in requestInfo.ResponseNodes)
            {
                if (kvp.Key == "cmvo")
                {
                    Volume = (byte)kvp.Value.GetInt32Value();
                    break;
                }
            }
        }

        #endregion

        #endregion

        #region Commands

        public void SendPlayPauseCommand()
        {
            string url = "/ctrl-int/1/playpause?session-id=" + SessionID;
            SubmitHTTPRequest(url);
        }

        public void SendNextItemCommand()
        {
            string url = "/ctrl-int/1/nextitem?session-id=" + SessionID;
            SubmitHTTPRequest(url);
        }

        public void SendPrevItemCommand()
        {
            string url = "/ctrl-int/1/previtem?session-id=" + SessionID;
            SubmitHTTPRequest(url);
        }

        public void SendShuffleStateCommand(bool shuffleState)
        {
            int intState = (shuffleState) ? 1 : 0;
            string url = "/ctrl-int/1/setproperty?dacp.shufflestate=" + intState + "&session-id=" + SessionID;
            SubmitHTTPRequest(url);
        }

        public void SendRepeatStateCommand(RepeatStates repeatState)
        {
            int intState = (int)repeatState;
            string url = "/ctrl-int/1/setproperty?dacp.repeatstate=" + intState + "&session-id=" + SessionID;
            SubmitHTTPRequest(url);
        }

        #endregion

    }
}
