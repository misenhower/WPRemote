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
using System.Collections.Generic;

namespace Komodex.DACP
{
    public delegate void HTTPResponseHandler(HTTPRequestInfo requestInfo);

    public partial class DACPServer
    {
        #region HTTP Management

        protected List<HttpWebRequest> PendingHttpRequests = new List<HttpWebRequest>();


        /// <summary>
        /// Submits a HTTP request to the DACP server
        /// </summary>
        /// <param name="url">The URL request (e.g., "/server-info").</param>
        /// <param name="callback">If no callback is specified, the default HTTPByteCallback will be used.</param>
        public HTTPRequestInfo SubmitHTTPRequest(string url, AsyncCallback callback = null, IDACPResponseHandler responseHandler = null, HTTPResponseHandler responseHandlerDelegate = null, object actionObject = null)
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
                requestInfo.ResponseHandler = responseHandler;
                requestInfo.ResponseHandlerDelegate = responseHandlerDelegate;
                requestInfo.ActionObject = actionObject;

                // Send HTTP request
                webRequest.BeginGetResponse(callback, requestInfo);

                PendingHttpRequests.Add(webRequest);

                return requestInfo;
            }
            catch (Exception e)
            {
                Utility.DebugWrite("Caught exception: " + e.Message);
                ConnectionError();
                return null;
            }
        }

        protected void HTTPByteCallback(IAsyncResult result)
        {
            try
            {
                // Get the HTTPRequestInfo object
                HTTPRequestInfo requestInfo = (HTTPRequestInfo)result.AsyncState;

                Utility.DebugWrite("Got HTTP response for: " + requestInfo.WebRequest.RequestUri);

                WebResponse response = requestInfo.WebRequest.EndGetResponse(result);

                if (Stopped)
                    return;

                if (PendingHttpRequests.Contains(requestInfo.WebRequest))
                    PendingHttpRequests.Remove(requestInfo.WebRequest);

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

                if (requestInfo.ResponseHandler != null)
                {
                    requestInfo.ResponseHandler.ProcessResponse(requestInfo);
                }
                else if (requestInfo.ResponseHandlerDelegate != null)
                {
                    requestInfo.ResponseHandlerDelegate(requestInfo);
                }
                else
                {
                    // Determine the type of response
                    switch (requestInfo.ResponseCode)
                    {
                        case "msrv": // Server info response
                            ProcessServerInfoResponse(requestInfo);
                            break;
                        case "mlog": // Login response
                            ProcessLoginResponse(requestInfo);
                            break;
                        case "mupd": // Library update response
                            ProcessLibraryUpdateResponse(requestInfo);
                            break;
                        case "cmst": // Play status response
                            ProcessPlayStatusResponse(requestInfo);
                            break;
                        case "cmgt": // Volume status response (TODO: and maybe others?)
                            ProcessVolumeStatusResponse(requestInfo);
                            break;
                        case "avdb": // Databases
                            ProcessDatabasesResponse(requestInfo);
                            break;
                        case "aply": // Playlists
                            ProcessPlaylistsResponse(requestInfo);
                            break;
                        case "agar": // Artists response
                            ProcessArtistsResponse(requestInfo);
                            break;
                        case "agal": // Albums response
                            ProcessAlbumsResponse(requestInfo);
                            break;
                        default:
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                if (e is WebException && ((WebException)e).Status == WebExceptionStatus.RequestCanceled)
                    return;

                Utility.DebugWrite("Caught exception: " + e.Message);
                ConnectionError();
            }
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

            SubmitLoginRequest();
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
            bool gotSessionID = false;

            foreach (var kvp in requestInfo.ResponseNodes)
            {
                if (kvp.Key == "mlid")
                {
                    SessionID = kvp.Value.GetInt32Value();
                    gotSessionID = true;
                    break;
                }
            }

            if (gotSessionID)
            {
                if (!Stopped)
                {
                    SubmitDatabasesRequest();
                    if (UseDelayedResponseRequests)
                    {
                        SubmitLibraryUpdateRequest();
                        SubmitPlayStatusRequest();
                    }
                }


                CurrentAlbumArtURL = HTTPPrefix + "/ctrl-int/1/nowplayingartwork?mw=300&mh=300&session-id=" + SessionID;
            }
            else
            {
                ConnectionError();
            }
        }

        #endregion

        #region Update

        protected int libraryUpdateRevisionNumber = 1;
        protected HTTPRequestInfo libraryUpdateRequestInfo = null;

        protected void SubmitLibraryUpdateRequest()
        {
            if (libraryUpdateRequestInfo != null)
                return; // Slightly different behavior than the PlayStatus update because I'm not quite sure where this is needed yet

            string url = "/update?revision-number=" + libraryUpdateRevisionNumber + "&daap-no-disconnect=1&session-id=" + SessionID;
            libraryUpdateRequestInfo = SubmitHTTPRequest(url);
        }

        protected void ProcessLibraryUpdateResponse(HTTPRequestInfo requestInfo)
        {
            foreach (var kvp in requestInfo.ResponseNodes)
            {
                if (kvp.Key == "musr")
                {
                    libraryUpdateRevisionNumber = kvp.Value.GetInt32Value();
                    break;
                }
            }

            if (UseDelayedResponseRequests && !Stopped)
                SubmitLibraryUpdateRequest();
        }

        #endregion

        #region Play Status

        protected int playStatusRevisionNumber = 1;
        protected HTTPRequestInfo playStatusRequestInfo = null;

        protected void SubmitPlayStatusRequest()
        {
            if (playStatusRequestInfo != null)
                playStatusRequestInfo.WebRequest.Abort();

            string url = "/ctrl-int/1/playstatusupdate?revision-number=" + playStatusRevisionNumber + "&session-id=" + SessionID;
            playStatusRequestInfo = SubmitHTTPRequest(url);
        }

        protected void ProcessPlayStatusResponse(HTTPRequestInfo requestInfo)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                timerTrackTimeUpdate.Stop();
            });

            string newSongName = null;
            string newArtist = null;
            string newAlbum = null;
            int newTrackTimeTotal = 0;
            int? newTrackTimeRemaining = null;

            foreach (var kvp in requestInfo.ResponseNodes)
            {
                switch (kvp.Key)
                {
                    case "cmsr": // Revision number
                        playStatusRevisionNumber = kvp.Value.GetInt32Value();
                        break;
                    case "cann": // Song name
                        newSongName = kvp.Value.GetStringValue();
                        break;
                    case "cana": // Artist
                        newArtist = kvp.Value.GetStringValue();
                        break;
                    case "canl": // Album
                        newAlbum = kvp.Value.GetStringValue();
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
                    case "cast": // Track length (ms)
                        newTrackTimeTotal = kvp.Value.GetInt32Value();
                        break;
                    case "cant": // Remaining track length (ms)
                        newTrackTimeRemaining = kvp.Value.GetInt32Value();
                        break;
                    default:
                        break;
                }
            }

            // Set all the properties
            CurrentSongName = newSongName;
            CurrentArtist = newArtist;
            CurrentAlbum = newAlbum;
            TrackTimeTotal = newTrackTimeTotal;
            TrackTimeRemaining = newTrackTimeRemaining ?? newTrackTimeTotal;

            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (PlayState == PlayStates.Playing)
                    timerTrackTimeUpdate.Start();
            });
            SendPropertyChanged("CurrentAlbumArtURL"); // TODO: Need to be a bit more efficient about this, perhaps by doing this in the PropertyChanged event
            SubmitVolumeStatusRequest();
            if (UseDelayedResponseRequests && !Stopped)
                SubmitPlayStatusRequest();
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
            // If the track doesn't change, iTunes won't send a status update
            TrackTimeRemaining = TrackTimeTotal;

            string url = "/ctrl-int/1/previtem?session-id=" + SessionID;
            SubmitHTTPRequest(url);
        }

        /// <summary>
        /// Toggles the shuffle state
        /// </summary>
        public void SendShuffleStateCommand()
        {
            SendShuffleStateCommand(!ShuffleState);
        }

        public void SendShuffleStateCommand(bool shuffleState)
        {
            int intState = (shuffleState) ? 1 : 0;
            string url = "/ctrl-int/1/setproperty?dacp.shufflestate=" + intState + "&session-id=" + SessionID;
            SubmitHTTPRequest(url);
            ShuffleState = shuffleState;
        }

        /// <summary>
        /// Cycles through the repeat states
        /// </summary>
        public void SendRepeatStateCommand()
        {
            switch (RepeatState)
            {
                case RepeatStates.None:
                    SendRepeatStateCommand(RepeatStates.RepeatAll);
                    break;
                case RepeatStates.RepeatAll:
                    SendRepeatStateCommand(RepeatStates.RepeatOne);
                    break;
                case RepeatStates.RepeatOne:
                default:
                    SendRepeatStateCommand(RepeatStates.None);
                    break;
            }
        }

        public void SendRepeatStateCommand(RepeatStates repeatState)
        {
            int intState = (int)repeatState;
            string url = "/ctrl-int/1/setproperty?dacp.repeatstate=" + intState + "&session-id=" + SessionID;
            SubmitHTTPRequest(url);
            RepeatState = repeatState;
        }

        #endregion

    }
}
