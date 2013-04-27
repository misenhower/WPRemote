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
using System.Windows.Threading;
using Komodex.DACP.Library;
using System.Text;
using Komodex.Common;
using System.Threading;

namespace Komodex.DACP
{
    public delegate void HTTPResponseHandler(HTTPRequestInfo requestInfo);
    public delegate void HTTPExceptionHandler(HTTPRequestInfo requestInfo, WebException e);

    public partial class DACPServer
    {
        #region HTTP Management

        protected List<HTTPRequestInfo> PendingHttpRequests = new List<HTTPRequestInfo>();

        /// <summary>
        /// Submits a HTTP request to the DACP server
        /// </summary>
        /// <param name="url">The URL request (e.g., "/server-info").</param>
        /// <param name="callback">If no callback is specified, the default HTTPByteCallback will be used.</param>
        internal HTTPRequestInfo SubmitHTTPRequest(string url, HTTPResponseHandler responseHandlerDelegate = null, bool isDataRetrieval = false, Action<HTTPRequestInfo> additionalSettings = null)
        {
            _log.Info("Submitting HTTP request for: " + url);

            try
            {
                // Set up HTTPWebRequest
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(HTTPPrefix + url);
                webRequest.Method = "POST";
                webRequest.Headers["Viewer-Only-Client"] = "1";

                // Create a new HTTPRequestState object
                HTTPRequestInfo requestInfo = new HTTPRequestInfo(webRequest);
                requestInfo.ResponseHandlerDelegate = responseHandlerDelegate;
                requestInfo.IsDataRetrieval = isDataRetrieval;
                if (additionalSettings != null)
                    additionalSettings(requestInfo);

                // Send HTTP request
                webRequest.BeginGetResponse(new AsyncCallback(HTTPByteCallback), requestInfo);

                lock (PendingHttpRequests)
                    PendingHttpRequests.Add(requestInfo);

                if (isDataRetrieval)
                    UpdateGettingData(true);

                return requestInfo;
            }
            catch (Exception e)
            {
                _log.Error("Caught exception: " + e.Message);
                StringBuilder errorString = new StringBuilder("Error creating HTTP Request:\r\n");
                errorString.AppendLine("URL: " + url);
                errorString.AppendLine(e.ToString());
                ConnectionError(errorString.ToString());
                return null;
            }
        }

        protected void HTTPByteCallback(IAsyncResult result)
        {
            // Get the HTTPRequestInfo object
            HTTPRequestInfo requestInfo = (HTTPRequestInfo)result.AsyncState;

            _log.Info("Got HTTP response for: " + requestInfo.WebRequest.RequestUri);

            try
            {
                WebResponse response = requestInfo.WebRequest.EndGetResponse(result);
                requestInfo.WebResponse = response as HttpWebResponse;

                if (Stopped)
                    return;

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

                var parsedResponse = DACPUtility.GetResponseNodes(byteResult, true);
                if (parsedResponse.Count > 0)
                {
                    var parsedResponseNode = parsedResponse[0];

                    requestInfo.ResponseCode = parsedResponseNode.Key;
                    requestInfo.ResponseBody = parsedResponseNode.Value;
                }

                if (requestInfo.ResponseHandlerDelegate != null)
                    requestInfo.ResponseHandlerDelegate(requestInfo);
            }
            catch (Exception e)
            {
                _log.Warning("Caught exception for {0}: {1}", requestInfo.WebRequest.RequestUri, e.Message);
                _log.Debug("Exception details: " + e.ToString());

                if (e is WebException)
                {
                    WebException webException = (WebException)e;

                    _log.Warning("Caught web exception: " + webException.Message);
                    _log.Debug("WebException Status: " + webException.Status.ToString());

                    if (webException.Status == WebExceptionStatus.RequestCanceled)
                    {
                        lock (PendingHttpRequests)
                        {
                            if (!PendingHttpRequests.Contains(requestInfo))
                                return;
                        }
                    }

                    if (requestInfo.ExceptionHandlerDelegate != null)
                    {
                        requestInfo.ExceptionHandlerDelegate(requestInfo, webException);
                        return;
                    }
                }
                StringBuilder errorString = new StringBuilder("HTTPByteCallback Error:\r\n");
                errorString.AppendLine("URL: " + requestInfo.WebRequest.RequestUri.GetPathAndQueryString());
                errorString.AppendLine(e.ToString());
                _log.Error("Unhandled web exception.");
                _log.Debug(errorString.ToString());
                ConnectionError(errorString.ToString());
            }
            finally
            {
                lock (PendingHttpRequests)
                    PendingHttpRequests.Remove(requestInfo);
                UpdateGettingData();
            }
        }

        #endregion

        #region Library Play Requests

        internal HTTPRequestInfo SubmitHTTPPlayRequest(string url)
        {
            return SubmitHTTPRequest(url, null, false, r => r.ExceptionHandlerDelegate = HandleLibraryPlayException);
        }

        protected void HandleLibraryPlayException(HTTPRequestInfo requestInfo, WebException e)
        {
            // TODO: Check error status code, etc.
            SendServerUpdate(ServerUpdateType.LibraryError);
        }

        #endregion

        #region Requests and Responses

        #region Server Info

        protected void SubmitServerInfoRequest()
        {
            string url = "/server-info";
            SubmitHTTPRequest(url, new HTTPResponseHandler(ProcessServerInfoResponse));
        }

        protected void ProcessServerInfoResponse(HTTPRequestInfo requestInfo)
        {
            if (requestInfo.WebResponse != null && requestInfo.WebResponse.Headers != null)
            {
                // This will return null if the header doesn't exist
                ServerVersionString = requestInfo.WebResponse.Headers["DAAP-Server"];
            }

            foreach (var kvp in requestInfo.ResponseNodes)
            {
                switch (kvp.Key)
                {
                    case "minm": // Library name
                        LibraryName = kvp.Value.GetStringValue();
                        break;
                    case "aeSV": // Server Version
                        ServerVersion = kvp.Value.GetInt32Value();
                        break;
                    case "mpro": // DMAP Version
                        ServerDMAPVersion = kvp.Value.GetInt32Value();
                        break;
                    case "apro": // DAAP Version
                        ServerDAAPVersion = kvp.Value.GetInt32Value();
                        break;
                    default:
                        break;
                }
            }

            SubmitServerCapabilitiesRequest();
        }

        #endregion

        #region Server Capabilities (ctrl-int)

        protected void SubmitServerCapabilitiesRequest()
        {
            string url = "/ctrl-int";
            SubmitHTTPRequest(url, ProcessServerCapabilityResponse);
        }

        protected void ProcessServerCapabilityResponse(HTTPRequestInfo requestInfo)
        {
            foreach (var kvp in requestInfo.ResponseNodes)
            {
                switch(kvp.Key)
                {
                    case "mlcl":
                        var mlcl = DACPUtility.GetResponseNodes(kvp.Value);
                        var capabilityNodes = DACPUtility.GetResponseNodes(mlcl[0].Value);
                        foreach (var capabilityNode in capabilityNodes)
                        {
                            switch(capabilityNode.Key)
                            {
                                case "ceSX":
                                    Int64 ceSX = capabilityNode.Value.GetInt64Value();

                                    // Supports Play Queue (indicated by bit 0 of ceSX)
                                    if ((ceSX & (1 << 0)) == (1 << 0))
                                        SupportsPlayQueue = true;

                                    break;
                            }
                        }
                        break;
                }
            }

            SubmitLoginRequest();
        }

        #endregion

        #region Login

        protected void SubmitLoginRequest()
        {
            string url = "/login?pairing-guid=0x" + PairingCode;
            SubmitHTTPRequest(url, new HTTPResponseHandler(ProcessLoginResponse), false, r => r.ExceptionHandlerDelegate = new HTTPExceptionHandler(HandleLoginException));
        }

        protected void HandleLoginException(HTTPRequestInfo requestInfo, WebException e)
        {
            ConnectionError(ServerErrorType.InvalidPIN);
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
            libraryUpdateRequestInfo = SubmitHTTPRequest(url, new HTTPResponseHandler(ProcessLibraryUpdateResponse));
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

        protected int _playStatusRevisionNumber = 1;
        protected HTTPRequestInfo _playStatusRequestInfo = null;
        protected Timer _playStatusCancelTimer;

        protected void SubmitPlayStatusRequest()
        {
            // Disable the cancellation timer
            _playStatusCancelTimer.Change(Timeout.Infinite, Timeout.Infinite);

            string url = "/ctrl-int/1/playstatusupdate?revision-number=" + _playStatusRevisionNumber + "&session-id=" + SessionID;
            _playStatusRequestInfo = SubmitHTTPRequest(url, new HTTPResponseHandler(ProcessPlayStatusResponse), false, r => r.ExceptionHandlerDelegate = new HTTPExceptionHandler(HandlePlayStatusException));

            // Re-enable the cancellation timer
            _playStatusCancelTimer.Change(45000, Timeout.Infinite);
        }

        // HTTPWebRequests appear to have a timeout of 60 seconds.  I have not found a way to extend
        // this timeout, so if this is a WebException for the Play Status request, we need to handle
        // the error differently.  When this timeout occurs, iTunes will also end the current session.
        // Also, it appears that the web exception's status will NOT be set to WebExceptionStatus.Timeout
        // To get around the session ending issue, I am re-requesting the play status every 45 seconds.

        protected HTTPRequestInfo canceledPlayStatusRequestInfo = null;

        private void playStatusCancelTimer_Tick(object state)
        {
            if (UseDelayedResponseRequests && !Stopped)
            {
                _log.Info("Canceling play status request...");
                canceledPlayStatusRequestInfo = _playStatusRequestInfo;
                _playStatusRequestInfo = null;
                SubmitPlayStatusRequest();
            }
        }

        protected void HandlePlayStatusException(HTTPRequestInfo requestInfo, WebException e)
        {
            if (canceledPlayStatusRequestInfo == null || requestInfo != canceledPlayStatusRequestInfo)
            {
                ConnectionError();
                return;
            }

            canceledPlayStatusRequestInfo = null;

            if (e.Status != WebExceptionStatus.UnknownError)
                ConnectionError();

            _log.Info("Caught timed out play status response.");
        }

        // NOTE: If this method's name changes, it must be updated in the HTTPByteCallback method as well
        protected void ProcessPlayStatusResponse(HTTPRequestInfo requestInfo)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                timerTrackTimeUpdate.Stop();
            });

            int newSongID = 0;
            int newContainerID = 0;
            int newContainerItemID = 0;
            string newSongName = null;
            string newArtist = null;
            string newAlbum = null;
            UInt64 newAlbumPersistentID = 0;
            int newTrackTimeTotal = 0;
            int? newTrackTimeRemaining = null;
            int newMediaKind = 0;

            foreach (var kvp in requestInfo.ResponseNodes)
            {
                switch (kvp.Key)
                {
                    case "cmsr": // Revision number
                        _playStatusRevisionNumber = kvp.Value.GetInt32Value();
                        break;
                    case "canp": // Current song and container IDs
                        byte[] value = kvp.Value;

                        //byte[] dbID = { value[0], value[1], value[2], value[3] }; // We already have the current DB (for now)
                        byte[] containerID = { value[4], value[5], value[6], value[7] };
                        byte[] containerItemID = { value[8], value[9], value[10], value[11] };
                        byte[] songID = { value[12], value[13], value[14], value[15] };
                        newContainerID = containerID.GetInt32Value();
                        newContainerItemID = containerItemID.GetInt32Value();
                        newSongID = songID.GetInt32Value();
                        break;
                    case "cann": // Song name
                        newSongName = kvp.Value.GetStringValue();
                        if (newSongName == "\u2603\u26035\u26034\u2603")
                            newSongName += "!";
                        break;
                    case "cana": // Artist
                        newArtist = kvp.Value.GetStringValue();
                        break;
                    case "canl": // Album
                        newAlbum = kvp.Value.GetStringValue();
                        break;
                    case "asai":
                        newAlbumPersistentID = (UInt64)kvp.Value.GetInt64Value();
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
                    case "cmmk": // Media kind
                        newMediaKind = kvp.Value.GetInt32Value();
                        break;
                    default:
                        break;
                }
            }

            // If the song ID changed, refresh the album art
            if (newSongID != CurrentSongID)
                PropertyChanged.RaiseOnUIThread(this, "CurrentAlbumArtURL");

            // Set all the properties
            CurrentSongID = newSongID;
            CurrentContainerID = newContainerID;
            CurrentContainerItemID = newContainerItemID;
            CurrentSongName = newSongName;
            CurrentArtist = newArtist;
            CurrentAlbum = newAlbum;
            CurrentAlbumPersistentID = newAlbumPersistentID;
            TrackTimeTotal = newTrackTimeTotal;
            TrackTimeRemaining = newTrackTimeRemaining ?? newTrackTimeTotal;
            CurrentMediaKind = newMediaKind;

            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (PlayState == PlayStates.Playing)
                    timerTrackTimeUpdate.Start();
            });
            SubmitUserRatingRequest();
            SubmitVolumeStatusRequest();
            SubmitGetSpeakersRequest();
            if (UseDelayedResponseRequests && !Stopped)
                SubmitPlayStatusRequest();
        }

        #endregion

        #region Volume Status

        protected void SubmitVolumeStatusRequest()
        {
            string url = "/ctrl-int/1/getproperty?properties=dmcp.volume&session-id=" + SessionID;
            SubmitHTTPRequest(url, new HTTPResponseHandler(ProcessVolumeStatusResponse));
        }

        protected void ProcessVolumeStatusResponse(HTTPRequestInfo requestInfo)
        {
            foreach (var kvp in requestInfo.ResponseNodes)
            {
                if (kvp.Key == "cmvo")
                {
                    _Volume = (byte)kvp.Value.GetInt32Value();
                    SendVolumePropertyChanged();
                    break;
                }
            }
        }

        #endregion

        #region User/Star Ratings

        private int _ratingUpdatedForSongID = 0;

        protected void SubmitUserRatingRequest()
        {
            if (CurrentSongID != _ratingUpdatedForSongID)
            {
                _ratingUpdatedForSongID = 0;
                SetCurrentSongUserRatingFromServer(0);
            }

            if (CurrentContainerID == 0 || CurrentSongID == 0 || CurrentAlbumPersistentID == 0)
                return;

            string url = "/databases/" + DatabaseID + "/containers/" + CurrentContainerID + "/items"
                + "?meta=dmap.itemid,dmap.containeritemid,daap.songuserrating"
                + "&type=music"
                + "&sort=album"
                + "&query=('daap.songalbumid:" + CurrentAlbumPersistentID + "'+'dmap.itemid:" + CurrentSongID + "')"
                + "&session-id=" + SessionID;
            SubmitHTTPRequest(url, ProcessUserRatingResponse, false, ri => ri.ExceptionHandlerDelegate = HandleUserRatingException);
        }

        protected void ProcessUserRatingResponse(HTTPRequestInfo requestInfo)
        {
            foreach (var kvp in requestInfo.ResponseNodes)
            {
                switch (kvp.Key)
                {
                    case "mlcl":
                        var songNodes = DACPUtility.GetResponseNodes(kvp.Value);
                        foreach (var songData in songNodes)
                        {
                            MediaItem mediaItem = new MediaItem(this, songData.Value);
                            if (mediaItem.ID == CurrentSongID)
                            {
                                _ratingUpdatedForSongID = CurrentSongID;
                                SetCurrentSongUserRatingFromServer(mediaItem.UserRating);
                                break;
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        protected void HandleUserRatingException(HTTPRequestInfo requestInfo, WebException e)
        {
            _ratingUpdatedForSongID = 0;
            SetCurrentSongUserRatingFromServer(0);
        }

        private void SetCurrentSongUserRatingFromServer(int serverValue)
        {
            int rating = serverValue / 20;
            _CurrentSongUserRating = rating;
            PropertyChanged.RaiseOnUIThread(this, "CurrentSongUserRating");
        }

        protected void SendUserRatingCommand(int rating)
        {
            if (CurrentSongID != 0)
                SendUserRatingCommand(rating, CurrentSongID);
        }

        protected void SendUserRatingCommand(int rating, int songID)
        {
            string url = "/ctrl-int/1/setproperty"
                + "?dacp.userrating=" + rating
                + "&database-spec='dmap.persistentid:0x" + DatabaseID.ToString("x") + "'&item-spec='dmap.itemid:0x" + songID.ToString("x") + "'"
                + "&session-id=" + SessionID;
            SubmitHTTPRequest(url);
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

        public void SendShuffleAllSongsCommand()
        {
            string url;

            if (SupportsPlayQueue)
            {
                int id;

                if (MusicPlaylist == null)
                    id = BasePlaylist.ID;
                else
                    id = MusicPlaylist.ID;

                url = "/ctrl-int/1/playqueue-edit?command=add&query='dmap.itemid:" + id + "'&query-modifier=containers&sort=name&mode=2&session-id=" + SessionID;
            }
            else
            {
                url = "/ctrl-int/1/cue?command=play&query=('com.apple.itunes.mediakind:1','com.apple.itunes.mediakind:32')&dacp.shufflestate=1&sort=artist&clear-first=1&session-id=" + SessionID;
            }

            SubmitHTTPRequest(url);
        }

        #endregion

    }
}
