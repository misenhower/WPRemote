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
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Threading.Tasks;

namespace Komodex.DACP
{
    public delegate void HTTPResponseHandler(HTTPRequestInfo requestInfo);
    public delegate void HTTPExceptionHandler(HTTPRequestInfo requestInfo, WebException e);

    public partial class DACPServer
    {
        #region HttpClient

        internal HttpClient HttpClient { get; private set; }

        protected void UpdateHttpClient()
        {
            // Build the new HttpClient
            HttpClientHandler clientHandler = new HttpClientHandler();
            if (clientHandler.SupportsAutomaticDecompression)
                clientHandler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            HttpClient client = new HttpClient(clientHandler);
            client.BaseAddress = new Uri(HTTPPrefix);
            client.DefaultRequestHeaders.Add("Viewer-Only-Client", "1");
            client.DefaultRequestHeaders.Add("Client-DAAP-Version", "3.11");

            if (HttpClient != null)
            {
                HttpClient.CancelPendingRequests();
                HttpClient.Dispose();
            }
            HttpClient = client;
        }

        internal async Task<DACPResponse> SubmitRequestAsync(DACPRequest request)
        {
            if (request.IncludeSessionID)
                request.QueryParameters["session-id"] = SessionID.ToString();
            return await SubmitRequestAsync(request.GetURI()).ConfigureAwait(false);
        }

        internal async Task<DACPResponse> SubmitRequestAsync(string uri)
        {
            _log.Info("Submitting request for: " + uri);

            HttpResponseMessage response = await HttpClient.PostAsync(uri, null).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            byte[] data = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);

            // Get the content of the first node
            IEnumerable<DACPNode> nodes = null;
            if (data.Length > 0)
            {
                data = DACPUtility.GetResponseNodes(data).First().Value;
                nodes = DACPUtility.GetResponseNodes(data);
            }

            return new DACPResponse(response, nodes);
        }

        #endregion

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

                var parsedResponse = DACPUtility.GetResponseNodes(byteResult).FirstOrDefault();
                if (parsedResponse != null)
                {
                    requestInfo.ResponseCode = parsedResponse.Key;
                    requestInfo.ResponseBody = parsedResponse.Value;
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

            var nodes = DACPNodeDictionary.Parse(requestInfo.ResponseBody);

            LibraryName = nodes.GetString("minm");
            ServerVersion = nodes.GetInt("aeSV");
            ServerDMAPVersion = nodes.GetInt("mpro");
            ServerDAAPVersion = nodes.GetInt("apro");

            // MAC addresses
            if (nodes.ContainsKey("msml"))
            {
                List<string> macAddresses = new List<string>();
                var addressNodes = DACPUtility.GetResponseNodes(nodes["msml"]).Where(n => n.Key == "msma").Select(n => n.Value);
                foreach (var addressNode in addressNodes)
                {
                    var address = BitConverter.ToInt64(addressNode, 0);
                    address = address >> 16;
                    macAddresses.Add(address.ToString("X12"));
                }
                MACAddresses = macAddresses.ToArray();
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
            var mlcl = DACPUtility.GetResponseNodes(requestInfo.ResponseNodes.First(n => n.Key == "mlcl").Value);
            var nodes = DACPNodeDictionary.Parse(mlcl.First(n => n.Key == "mlit").Value);

            if (nodes.ContainsKey("ceSX"))
            {
                Int64 ceSX = nodes.GetLong("ceSX");

                // Supports Play Queue (indicated by bit 0 of ceSX)
                if ((ceSX & (1 << 0)) == (1 << 0))
                    SupportsPlayQueue = true;
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
            var nodes = DACPNodeDictionary.Parse(requestInfo.ResponseBody);

            if (nodes.ContainsKey("mlid"))
            {
                SessionID = nodes.GetInt("mlid");

                if (!Stopped)
                    SubmitDatabasesRequest();

                int pixels = ResolutionUtility.GetScaledPixels(284);
                CurrentAlbumArtURL = HTTPPrefix + "/ctrl-int/1/nowplayingartwork?mw=" + pixels + "&mh=" + pixels + "&session-id=" + SessionID;
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

        private void playStatusCancelTimer_Tick(object state)
        {
            if (UseDelayedResponseRequests && !Stopped)
            {
                _log.Info("Canceling play status request...");
                var requestInfo = _playStatusRequestInfo;
                _playStatusRequestInfo = null;

                // Ignore any response we get for this request in case it isn't cancelled immediately
                requestInfo.ResponseHandlerDelegate = null;

                // Remove it from the list of pending HTTP requests
                lock (PendingHttpRequests)
                    PendingHttpRequests.Remove(requestInfo);

                // Submitting a new play status request will cause iTunes to abort the previous one without ending the session
                SubmitPlayStatusRequest();
            }
        }

        protected void HandlePlayStatusException(HTTPRequestInfo requestInfo, WebException e)
        {
            // If we've canceled this request, ignore any exceptions
            if (requestInfo.ResponseHandlerDelegate == null)
            {
                _log.Info("Caught timed out play status response.");
                return;
            }

            // Otherwise, this is a legitimate connection error
            ConnectionError();
        }

        // NOTE: If this method's name changes, it must be updated in the HTTPByteCallback method as well
        protected void ProcessPlayStatusResponse(HTTPRequestInfo requestInfo)
        {
            Utility.BeginInvokeOnUIThread(() =>
            {
                timerTrackTimeUpdate.Stop();
                if (_trackTimeRequestTimer != null)
                    _trackTimeRequestTimer.Stop();
            });

            var nodes = DACPNodeDictionary.Parse(requestInfo.ResponseBody);

            int oldSongID = CurrentSongID;

            _playStatusRevisionNumber = nodes.GetInt("cmsr");
            if (nodes.ContainsKey("canp"))
            {
                // Current song and container IDs
                byte[] value = nodes["canp"];

                //byte[] dbID = { value[0], value[1], value[2], value[3] }; // We already have the current DB (for now)
                byte[] containerID = { value[4], value[5], value[6], value[7] };
                byte[] containerItemID = { value[8], value[9], value[10], value[11] };
                byte[] songID = { value[12], value[13], value[14], value[15] };
                CurrentContainerID = containerID.GetInt32Value();
                CurrentContainerItemID = containerItemID.GetInt32Value();
                CurrentSongID = songID.GetInt32Value();
            }
            else
            {
                CurrentContainerID = 0;
                CurrentContainerItemID = 0;
                CurrentSongID = 0;
            }
            CurrentSongName = nodes.GetString("cann");
            CurrentArtist = nodes.GetString("cana");
            CurrentAlbum = nodes.GetString("canl");
            CurrentAlbumPersistentID = (UInt64)nodes.GetLong("asai");
            PlayState = (PlayStates)nodes.GetByte("caps");
            ShuffleState = nodes.GetBool("cash");
            RepeatState = (RepeatStates)nodes.GetByte("carp");
            CurrentMediaKind = nodes.GetInt("cmmk");

            // Track length (ms)
            TrackTimeTotal = nodes.GetInt("cast");
            // Remaining track length (ms)
            TrackTimeRemaining = nodes.GetNullableInt("cant") ?? TrackTimeTotal;

            // dacp.visualizer
            VisualizerActive = nodes.GetBool("cavs");
            // dacp.visualizerenabled
            VisualizerAvailable = nodes.GetBool("cave");
            // dacp.fullscreen
            FullScreenModeActive = nodes.GetBool("cafs");
            // dacp.fullscreenenabled
            FullScreenModeAvailable = nodes.GetBool("cafe");

            // If the song ID changed, refresh the album art
            if (oldSongID != CurrentSongID)
                PropertyChanged.RaiseOnUIThread(this, "CurrentAlbumArtURL");

            Utility.BeginInvokeOnUIThread(() =>
            {
                if (PlayState == PlayStates.Playing)
                    timerTrackTimeUpdate.Start();
                else if (PlayState == PlayStates.FastForward || PlayState == PlayStates.Rewind)
                {
                    if (_trackTimeRequestTimer == null)
                    {
                        _trackTimeRequestTimer = new DispatcherTimer();
                        _trackTimeRequestTimer.Tick += TrackTimeRequestTimer_Tick;
                        _trackTimeRequestTimer.Interval = TimeSpan.FromMilliseconds(250);
                    }
                    _trackTimeRequestTimer.Start();
                }
            });
            SubmitUserRatingRequest();
            SubmitVolumeStatusRequest();
            SubmitGetSpeakersRequest();
            SubmitPlayQueueRequest();
            if (UseDelayedResponseRequests && !Stopped)
                SubmitPlayStatusRequest();
        }

        #endregion

        #region Track Time

        protected DispatcherTimer _trackTimeRequestTimer;

        private void TrackTimeRequestTimer_Tick(object sender, EventArgs e)
        {
            SubmitTrackTimeRequest();
        }

        protected void SubmitTrackTimeRequest()
        {
            string url = "/ctrl-int/1/getproperty"
                + "?properties=dacp.playingtime"
                + "&session-id=" + SessionID;

            SubmitHTTPRequest(url, ProcessTrackTimeResponse);
        }

        protected void ProcessTrackTimeResponse(HTTPRequestInfo requestInfo)
        {
            var nodes = DACPNodeDictionary.Parse(requestInfo.ResponseBody);

            TrackTimeTotal = nodes.GetInt("cast");
            TrackTimeRemaining = nodes.GetNullableInt("cant") ?? TrackTimeTotal;
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
            var nodes = DACPNodeDictionary.Parse(requestInfo.ResponseBody);

            _Volume = (byte)nodes.GetInt("cmvo");
            SendVolumePropertyChanged();
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
            var mlcl = requestInfo.ResponseNodes.FirstOrDefault(n => n.Key == "mlcl");
            if (mlcl == null)
                return;

            var songNodes = DACPUtility.GetResponseNodes(mlcl.Value);
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

        public void SendBeginRewCommand()
        {
            string url = "/ctrl-int/1/beginrew?session-id=" + SessionID;
            SubmitHTTPRequest(url, null, false, r => r.ExceptionHandlerDelegate = HandlePlayTransportException);
        }

        public void SendBeginFFCommand()
        {
            string url = "/ctrl-int/1/beginff?session-id=" + SessionID;
            SubmitHTTPRequest(url, null, false, r => r.ExceptionHandlerDelegate = HandlePlayTransportException);
        }

        public void SendPlayResumeCommand()
        {
            string url = "/ctrl-int/1/playresume?session-id=" + SessionID;
            SubmitHTTPRequest(url, null, false, r => r.ExceptionHandlerDelegate = HandlePlayTransportException);
        }

        private void HandlePlayTransportException(HTTPRequestInfo requestInfo, WebException e)
        {
            // Do nothing
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

        #region Play Queue

        private ObservableCollection<PlayQueue> _playQueues;
        public ObservableCollection<PlayQueue> PlayQueues
        {
            get { return _playQueues; }
            protected set
            {
                if (_playQueues == value)
                    return;

                _playQueues = value;
                PropertyChanged.RaiseOnUIThread(this, "PlayQueues");
            }
        }

        private string _playQueueUpcomingSongName1;
        public string PlayQueueUpcomingSongName1
        {
            get { return _playQueueUpcomingSongName1; }
            protected set
            {
                if (_playQueueUpcomingSongName1 == value)
                    return;

                _playQueueUpcomingSongName1 = value;
                PropertyChanged.RaiseOnUIThread(this, "PlayQueueUpcomingSongName1");
            }
        }

        private string _playQueueUpcomingSongName2;
        public string PlayQueueUpcomingSongName2
        {
            get { return _playQueueUpcomingSongName2; }
            protected set
            {
                if (_playQueueUpcomingSongName2 == value)
                    return;

                _playQueueUpcomingSongName2 = value;
                PropertyChanged.RaiseOnUIThread(this, "PlayQueueUpcomingSongName2");
            }
        }

        protected void SubmitPlayQueueRequest()
        {
            if (!SupportsPlayQueue)
                return;

            string url = "/ctrl-int/1/playqueue-contents?span=50&session-id=" + SessionID;
            SubmitHTTPRequest(url, HandlePlayQueueResponse);
        }

        private void HandlePlayQueueResponse(HTTPRequestInfo requestInfo)
        {
            if (PlayQueues == null)
                PlayQueues = new ObservableCollection<PlayQueue>();

            List<PlayQueue> queues = new List<PlayQueue>();
            List<PlayQueueItem> queueItems = new List<PlayQueueItem>();

            var mlcl = requestInfo.ResponseNodes.FirstOrDefault(n => n.Key == "mlcl");
            if (mlcl != null)
            {
                var nodes = DACPUtility.GetResponseNodes(mlcl.Value);

                // Get the queues
                var ceQS = nodes.FirstOrDefault(n => n.Key == "ceQS");
                if (ceQS != null)
                    queues.AddRange(DACPUtility.GetResponseNodes(ceQS.Value).Where(n => n.Key == "mlit").Select(n => new PlayQueue(this, n.Value)));

                // Get the queue items
                queueItems.AddRange(nodes.Where(n => n.Key == "mlit").Select(n => new PlayQueueItem(this, n.Value)));
            }

            // Update the queues and queue items with minimal changes to avoid reloading the list while it's displayed.
            // This is optimized for simple inserts and deletions. Reordering items will still cause most of the list to reload.
            // Updating on the UI thread because of the observable collections being tied to UI elements.
            Utility.BeginInvokeOnUIThread(() =>
            {
                // Remove queues
                var removedQueues = PlayQueues.Where(q1 => !queues.Any(q2 => q1.ID == q2.ID)).ToArray();
                foreach (var q in removedQueues)
                    PlayQueues.Remove(q);

                // Update/insert queues
                for (int i = 0; i < queues.Count; i++)
                {
                    var queue = queues[i];

                    // Add the queue to the list if we don't already have it
                    if (PlayQueues.Count <= i || PlayQueues[i].ID != queue.ID)
                    {
                        PlayQueues.Insert(i, queue);
                    }
                    // Update the existing queue object's start index and item count
                    else
                    {
                        PlayQueues[i].Title1 = queue.Title1;
                        PlayQueues[i].Title2 = queue.Title2;
                        PlayQueues[i].StartIndex = queue.StartIndex;
                        PlayQueues[i].ItemCount = queue.ItemCount;
                    }
                }

                // Remove extra queues
                while (PlayQueues.Count > queues.Count)
                    PlayQueues.RemoveAt(queues.Count);

                // Put queue items in queues
                foreach (var queue in PlayQueues)
                {
                    int start = queue.StartIndex;
                    int stop = start + queue.ItemCount;

                    var items = queueItems.Where(i => i.QueueIndex >= start && i.QueueIndex < stop).OrderBy(i => i.QueueIndex).ToArray();

                    // Remove items
                    var removedItems = queue.Where(i1 => !items.Any(i2 => i1.SongID == i2.SongID && i1.DatabaseID == i2.DatabaseID)).ToArray();
                    foreach (var i in removedItems)
                        queue.Remove(i);

                    // Update/insert items
                    for (int i = 0; i < items.Length; i++)
                    {
                        if (queue.Count <= i || queue[i].SongID != items[i].SongID || queue[i].DatabaseID != items[i].DatabaseID)
                            queue.Insert(i, items[i]);
                        else
                            queue[i].QueueIndex = items[i].QueueIndex;
                    }

                    // Remove extra items
                    while (queue.Count > items.Length)
                        queue.RemoveAt(items.Length);
                }

                // Update upcoming songs
                string upcomingSongName1 = null;
                string upcomingSongName2 = null;

                // If an upcoming song is from a different artist than the currently playing artist, display both the
                // artist name and the song name. Also, if the first upcoming song is from a different artist, make sure
                // the artist name is shown for both songs to avoid any confusion.
                bool includeArtistName = false;

                var upcomingItem1 = queueItems.FirstOrDefault(i => i.QueueIndex == 1);
                if (upcomingItem1 != null)
                {
                    if (upcomingItem1.ArtistName != CurrentArtist)
                        includeArtistName = true;

                    if (includeArtistName)
                        upcomingSongName1 = upcomingItem1.ArtistName + " – " + upcomingItem1.SongName;
                    else
                        upcomingSongName1 = upcomingItem1.SongName;
                }

                var upcomingItem2 = queueItems.FirstOrDefault(i => i.QueueIndex == 2);
                if (upcomingItem2 != null)
                {
                    if (upcomingItem2.ArtistName != CurrentArtist)
                        includeArtistName = true;

                    if (includeArtistName)
                        upcomingSongName2 = upcomingItem2.ArtistName + " – " + upcomingItem2.SongName;
                    else
                        upcomingSongName2 = upcomingItem2.SongName;
                }

                PlayQueueUpcomingSongName1 = upcomingSongName1;
                PlayQueueUpcomingSongName2 = upcomingSongName2;
            });
        }

        #endregion

    }
}
