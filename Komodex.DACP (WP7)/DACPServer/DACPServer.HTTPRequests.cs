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
using System.Text;
using Komodex.Common;
using System.Threading;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Threading.Tasks;
using Komodex.DACP.Items;
using Komodex.DACP.Databases;
using Komodex.DACP.Queries;

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

            var oldHttpClient = HttpClient;
            if (oldHttpClient != null)
            {
                try
                {
                    oldHttpClient.CancelPendingRequests();
                    oldHttpClient.Dispose();
                }
                catch { }
            }

            HttpClient = client;
        }

        internal async Task<DACPResponse> SubmitRequestAsync(DACPRequest request)
        {
            if (request.IncludeSessionID)
                request.QueryParameters["session-id"] = SessionID.ToString();

            string uri = request.GetURI();

            _log.Info("Submitting request for: " + uri);

            HttpResponseMessage response = await HttpClient.PostAsync(uri, request.HttpContent, request.CancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
                throw new DACPRequestException(response);

            byte[] data = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);

            _log.Info("Received response for: " + uri);

            // Get the content of the first node
            IEnumerable<DACPNode> nodes = null;
            if (data.Length > 0)
            {
                data = DACPUtility.GetResponseNodes(data, true).First().Value;
                nodes = DACPUtility.GetResponseNodes(data);
            }

            return new DACPResponse(response, nodes);
        }

        internal async Task<List<T>> GetListAsync<T>(DACPRequest request, Func<DACPNodeDictionary, T> itemGenerator, string listKey = DACPUtility.DefaultListKey)
        {
            try
            {
                var response = await SubmitRequestAsync(request).ConfigureAwait(false);
                return DACPUtility.GetItemsFromNodes(response.Nodes, itemGenerator, listKey).ToList();
            }
            catch (Exception)
            {
                return new List<T>();
            }
        }

        internal async Task<IDACPList> GetAlphaGroupedListAsync<T>(DACPRequest request, Func<byte[], T> itemGenerator, string listKey = DACPUtility.DefaultListKey)
        {
            try
            {
                var response = await SubmitRequestAsync(request).ConfigureAwait(false);
                return DACPUtility.GetAlphaGroupedDACPList(response.Nodes, itemGenerator, listKey);
            }
            catch (Exception)
            {
                return new DACPList<T>(false);
            }
        }

        internal Task<IDACPList> GetAlphaGroupedListAsync<T>(DACPRequest request, Func<DACPNodeDictionary, T> itemGenerator, string listKey = DACPUtility.DefaultListKey)
        {
            return GetAlphaGroupedListAsync(request, b => itemGenerator(DACPNodeDictionary.Parse(b)), listKey);
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
                HandleConnectionError(errorString.ToString());
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

                if (!IsConnected)
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
                HandleConnectionError(errorString.ToString());
            }
            finally
            {
                lock (PendingHttpRequests)
                    PendingHttpRequests.Remove(requestInfo);
                UpdateGettingData();
            }
        }

        #endregion

        #region Requests and Responses

        #region Server Info

        protected async Task<bool> GetServerInfoAsync()
        {
            DACPRequest request = new DACPRequest("/server-info");
            request.IncludeSessionID = false;

            try
            {
                var response = await SubmitRequestAsync(request).ConfigureAwait(false);

                // Process response
                ServerVersionString = response.HTTPResponse.Headers.GetValues("DAAP-Server").FirstOrDefault();

                var nodes = DACPNodeDictionary.Parse(response.Nodes);

                // Fixing an issue with Apple TV devices where \0 may be appended to the end of the library name
                string libraryName = nodes.GetString("minm");
                if (!string.IsNullOrEmpty(libraryName))
                    libraryName = libraryName.Replace("\0", "");
                LibraryName = libraryName;

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
            }
            catch (Exception e)
            {
                HandleHTTPException(request, e);
                return false;
            }

            return true;
        }

        #endregion

        #region Server Capabilities (ctrl-int)

        protected async Task<bool> GetServerCapabilitiesAsync()
        {
            DACPRequest request = new DACPRequest("/ctrl-int");
            request.IncludeSessionID = false;

            try
            {
                var response = await SubmitRequestAsync(request).ConfigureAwait(false);

                // Process response
                var mlcl = DACPUtility.GetResponseNodes(response.Nodes.First(n => n.Key == "mlcl").Value);
                var nodes = DACPNodeDictionary.Parse(mlcl.First(n => n.Key == "mlit").Value);

                if (nodes.ContainsKey("ceSX"))
                {
                    Int64 ceSX = nodes.GetLong("ceSX");

                    // Bit 0: Supports Play Queue
                    if ((ceSX & (1 << 0)) != 0)
                        SupportsPlayQueue = true;

                    // Bit 1: iTunes Radio? Appeared in iTunes 11.1.2 with the iTunes Radio DB.
                    // Apple's Remote for iOS doesn't seem to use this bit to determine whether iTunes Radio is available.
                    // Instead, it looks for an iTunes Radio database and checks whether it has any containers.

                    // Bit 2: Genius Shuffle Enabled/Available
                    if ((ceSX & (1 << 2)) != 0)
                        SupportsGeniusShuffle = true;
                }

                // Apple TV
                // TODO: Is this the best way to detect this?
                IsAppleTV = nodes.GetBool("ceDR");
            }
            catch (Exception e)
            {
                HandleHTTPException(request, e);
                return false;
            }

            return true;
        }

        #endregion

        #region Login

        protected async Task<ConnectionResult> LoginAsync()
        {
            DACPRequest request = new DACPRequest("/login");
            request.QueryParameters["pairing-guid"] = "0x" + PairingCode;
            request.IncludeSessionID = false;

            try
            {
                var response = await SubmitRequestAsync(request).ConfigureAwait(false);

                // Process response
                var nodes = DACPNodeDictionary.Parse(response.Nodes);

                if (!nodes.ContainsKey("mlid"))
                    return ConnectionResult.InvalidPIN;

                SessionID = nodes.GetInt("mlid");
            }
            catch (DACPRequestException e)
            {
                int statusCode = (int)e.Response.StatusCode;
                if (statusCode >= 500 && statusCode <= 599)
                    return ConnectionResult.InvalidPIN;
                return ConnectionResult.ConnectionError;
            }
            catch (Exception e)
            {
                HandleHTTPException(request, e);
                return ConnectionResult.ConnectionError;
            }

            return ConnectionResult.Success;
        }

        #endregion

        #region Databases

        protected async Task<bool> GetDatabasesAsync()
        {
            DACPRequest request = new DACPRequest("/databases");

            try
            {
                var databases = await GetListAsync(request, n => DACPDatabase.GetDatabase(this, n)).ConfigureAwait(false);

                if (databases == null || databases.Count == 0)
                    return false;

                List<DACPDatabase> newSharedDatabases = new List<DACPDatabase>();

                for (int i = 0; i < databases.Count; i++)
                {
                    var db = databases[i];

                    // The main database will be first in the list
                    if (i == 0)
                    {
                        if (MainDatabase != null && MainDatabase.ID == db.ID)
                            continue;

                        bool success = await db.RequestContainersAsync().ConfigureAwait(false);
                        if (!success)
                            return false;
                        MainDatabase = db;
                        continue;
                    }

                    // Shared database
                    if (db.Type == DatabaseType.Shared)
                    {
                        newSharedDatabases.Add(db);
                        continue;
                    }

                    // Internet Radio
                    if (db.Type == DatabaseType.InternetRadio)
                    {
                        if (InternetRadioDatabase != null && InternetRadioDatabase.ID == db.ID)
                            continue;

                        InternetRadioDatabase = db;
                        continue;
                    }

                    // iTunes Radio
                    if (db.Type == DatabaseType.iTunesRadio)
                    {
                        if (iTunesRadioDatabase != null && iTunesRadioDatabase.ID == db.ID)
                            continue;

                        iTunesRadioDatabase = (iTunesRadioDatabase)db;
                        // Attempt to load the stations asynchronously to determine whether iTunes Radio is enabled.
                        var task = iTunesRadioDatabase.RequestStationsAsync();
                        continue;
                    }
                }

                // Update shared databases
                Dictionary<int, DACPDatabase> removedSharedDBs = SharedDatabases.ToDictionary(db => db.ID);
                foreach (var sharedDB in newSharedDatabases)
                {
                    removedSharedDBs.Remove(sharedDB.ID);
                    if (SharedDatabases.Any(db => db.ID == sharedDB.ID))
                        continue;
                    SharedDatabases.Add(sharedDB);
                }
                foreach (DACPDatabase db in removedSharedDBs.Values)
                    SharedDatabases.Remove(db);
            }
            catch (Exception e)
            {
                HandleHTTPException(request, e);
                return false;
            }

            return true;
        }

        #endregion

        #region Update

        internal int CurrentLibraryUpdateNumber { get; private set; }
        private CancellationTokenSource _currentLibraryUpdateCancellationTokenSource;

        protected Task<bool> GetFirstLibraryUpdateAsync()
        {
            CurrentLibraryUpdateNumber = 1;
            return GetLibraryUpdateAsync(CancellationToken.None);
        }

        protected async Task<bool> GetLibraryUpdateAsync(CancellationToken cancellationToken)
        {
            DACPRequest request = new DACPRequest("/update");
            request.QueryParameters["revision-number"] = CurrentLibraryUpdateNumber.ToString();
            request.QueryParameters["daap-no-disconnect"] = "1";

            try
            {
                var response = await SubmitRequestAsync(request).ConfigureAwait(false);

                if (cancellationToken.IsCancellationRequested)
                    return false;

                var nodes = DACPNodeDictionary.Parse(response.Nodes);
                CurrentLibraryUpdateNumber = nodes.GetInt("musr");
            }
            catch
            {
                return false;
            }
            return true;
        }

        protected async void SubscribeToLibraryUpdates()
        {
            TimeSpan autoCancelTimeSpan = TimeSpan.FromSeconds(45);
            TimeSpan resubmitDelay = TimeSpan.FromSeconds(2);
            CancellationToken token;

            while (IsConnected)
            {
                _currentLibraryUpdateCancellationTokenSource = new CancellationTokenSource();
                token = _currentLibraryUpdateCancellationTokenSource.Token;

#if WP7
                var updateTask = GetLibraryUpdateAsync(token);
                var cancelTask = TaskEx.Delay(autoCancelTimeSpan, token);
                await TaskEx.WhenAny(updateTask, cancelTask).ConfigureAwait(false);
#else
                var updateTask = GetLibraryUpdateAsync(token);
                var cancelTask = Task.Delay(autoCancelTimeSpan, token);
                await Task.WhenAny(updateTask, cancelTask).ConfigureAwait(false);
#endif

                if (token.IsCancellationRequested)
                    return;

                _currentLibraryUpdateCancellationTokenSource.Cancel();

                if (updateTask.Status == TaskStatus.RanToCompletion)
                {
                    if (updateTask.Result == false)
                    {
                        SendConnectionError();
                        return;
                    }

                    bool success = await GetDatabasesAsync().ConfigureAwait(false);
                    if (!success)
                    {
                        SendConnectionError();
                        return;
                    }

                    SendLibraryUpdate();

#if WP7
                    await TaskEx.Delay(resubmitDelay).ConfigureAwait(false);
#else
                    await Task.Delay(resubmitDelay).ConfigureAwait(false);
#endif
                }
            }
        }

        #endregion

        #region Play Status

        protected int _playStatusRevisionNumber = 1;
        private CancellationTokenSource _currentPlayStatusCancellationTokenSource;

        protected Task<bool> GetFirstPlayStatusUpdateAsync()
        {
            _playStatusRevisionNumber = 1;
            return GetPlayStatusUpdateAsync(CancellationToken.None);
        }

        protected async Task<bool> GetPlayStatusUpdateAsync(CancellationToken cancellationToken)
        {
            // Do not pass the cancellation token to the HTTP request since cancelling a request will cause iTunes to close the current session.
            DACPRequest request = new DACPRequest("/ctrl-int/1/playstatusupdate");
            request.QueryParameters["revision-number"] = _playStatusRevisionNumber.ToString();

            try
            {
                var response = await SubmitRequestAsync(request).ConfigureAwait(false);

                // Do we still need to process this response?
                if (cancellationToken.IsCancellationRequested)
                    return false;

                // Process response
                ThreadUtility.RunOnUIThread(() =>
                {
                    timerTrackTimeUpdate.Stop();
                });

                var nodes = DACPNodeDictionary.Parse(response.Nodes);
                _playStatusRevisionNumber = nodes.GetInt("cmsr");

                int oldSongID = CurrentItemID;

                if (nodes.ContainsKey("canp"))
                {
                    // Current song and container IDs
                    byte[] value = nodes["canp"];

                    byte[] dbID = { value[0], value[1], value[2], value[3] };
                    byte[] containerID = { value[4], value[5], value[6], value[7] };
                    byte[] containerItemID = { value[8], value[9], value[10], value[11] };
                    byte[] itemID = { value[12], value[13], value[14], value[15] };
                    CurrentDatabaseID = dbID.GetInt32Value();
                    CurrentContainerID = containerID.GetInt32Value();
                    CurrentContainerItemID = containerItemID.GetInt32Value();
                    CurrentItemID = itemID.GetInt32Value();
                }
                else
                {
                    CurrentDatabaseID = 0;
                    CurrentContainerID = 0;
                    CurrentContainerItemID = 0;
                    CurrentItemID = 0;
                }
                CurrentSongName = nodes.GetString("cann");
                CurrentArtist = nodes.GetString("cana");
                CurrentAlbum = nodes.GetString("canl");
                CurrentAlbumPersistentID = (UInt64)nodes.GetLong("asai");
                PlayState = (PlayStates)nodes.GetByte("caps");

                // Shuffle
                int caas = nodes.GetInt("caas");
                IsShuffleAvailable = (caas & (1 << 1)) != 0;
                ShuffleState = nodes.GetBool("cash");

                // Repeat
                int caar = nodes.GetInt("caar");
                IsRepeatOneAvailable = (caar & (1 << 1)) != 0;
                IsRepeatAllAvailable = (caar & (1 << 2)) != 0;
                RepeatState = (RepeatStates)nodes.GetByte("carp");

                CurrentMediaKind = nodes.GetInt("cmmk");
                ShowUserRating = nodes.GetBool("casu");

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

                // iTunes Radio
                if (iTunesRadioDatabase != null && iTunesRadioDatabase.ID == CurrentDatabaseID)
                {
                    IsCurrentlyPlayingiTunesRadio = true;
                    CurrentiTunesRadioStationName = nodes.GetString("ceNR");

                    // caks = 1 when the next button is disabled, and 2 when it's enabled
                    IsiTunesRadioNextButtonEnabled = (nodes.GetByte("caks") == 2);

                    // "aelb" indicates whether the star button (iTunes Radio menu) should be enabled, but this only seems to be set to true
                    // when connected via Home Sharing. This parameter is missing when an ad is playing, so use this to determine whether
                    // the menu should be enabled.
                    IsiTunesRadioMenuEnabled = nodes.ContainsKey("aelb");

                    IsiTunesRadioSongFavorited = (nodes.GetByte("aels") == 2);
                }
                else
                {
                    IsCurrentlyPlayingiTunesRadio = false;
                }


                if (IsCurrentlyPlayingiTunesRadio)
                {
                    var caks = nodes.GetByte("caks");
                    IsiTunesRadioNextButtonEnabled = !(caks == 1);
                }

                if (!nodes.ContainsKey("casc") || nodes.GetBool("casc") == true)
                    IsPlayPositionBarEnabled = true;
                else
                    IsPlayPositionBarEnabled = false;

                // Genius Shuffle
                IsCurrentlyPlayingGeniusShuffle = nodes.GetBool("ceGs");
                // There are two other nodes related to Genius Shuffle, "ceGS" and "aeGs" (currently unknown)

                // If the song ID changed, refresh the album art
                if (oldSongID != CurrentItemID)
                    PropertyChanged.RaiseOnUIThread(this, "CurrentAlbumArtURL");

                ThreadUtility.RunOnUIThread(() =>
                {
                    if (PlayState == PlayStates.Playing)
                        timerTrackTimeUpdate.Start();
                    else if (PlayState == PlayStates.FastForward || PlayState == PlayStates.Rewind)
                        BeginRepeatedTrackTimeRequest();
                });

                var volumeTask = UpdateCurrentVolumeLevelAsync();
                var userRatingTask = UpdateCurrentSongUserRatingAsync();
                var playQueueTask = UpdatePlayQueueContentsAsync();

                Task[] tasks = new[] { volumeTask, userRatingTask, playQueueTask };

#if WP7
                await TaskEx.WhenAll(tasks).ConfigureAwait(false);
#else
                await Task.WhenAll(tasks).ConfigureAwait(false);
#endif

                SubmitGetSpeakersRequest();
            }
            catch { return false; }
            return true;
        }

        protected async void SubscribeToPlayStatusUpdates()
        {
            // Windows Phone has a maximum HTTP timeout of 60 seconds. There is no way to increase this limit.
            // If an HTTP request times out and is canceled by the OS, iTunes will end the current session and
            // we'll have to log in again. Submitting another request before the first one times out causes
            // iTunes to close the first request without ending the session.

            TimeSpan autoCancelTimeSpan = TimeSpan.FromSeconds(45);
            CancellationToken token;

            while (IsConnected)
            {
                _currentPlayStatusCancellationTokenSource = new CancellationTokenSource();
                token = _currentPlayStatusCancellationTokenSource.Token;

#if WP7
                var updateTask = GetPlayStatusUpdateAsync(token);
                var cancelTask = TaskEx.Delay(autoCancelTimeSpan, token);
                await TaskEx.WhenAny(updateTask, cancelTask).ConfigureAwait(false);
#else
                var updateTask = GetPlayStatusUpdateAsync(token);
                var cancelTask = Task.Delay(autoCancelTimeSpan, token);
                await Task.WhenAny(updateTask, cancelTask).ConfigureAwait(false);
#endif

                if (token.IsCancellationRequested)
                    return;

                _currentPlayStatusCancellationTokenSource.Cancel();

                if (updateTask.Status == TaskStatus.RanToCompletion && updateTask.Result == false)
                {
                    SendConnectionError();
                    return;
                }
            }
        }

        #endregion

        #region Track Time

        protected async Task<bool> UpdateTrackTimeAsync()
        {
            DACPRequest request = new DACPRequest("/ctrl-int/1/getproperty");
            request.QueryParameters["properties"] = "dacp.playingtime";

            try
            {
                var response = await SubmitRequestAsync(request).ConfigureAwait(false);
                var nodes = DACPNodeDictionary.Parse(response.Nodes);

                TrackTimeTotal = nodes.GetInt("cast");
                TrackTimeRemaining = nodes.GetNullableInt("cant") ?? TrackTimeTotal;
            }
            catch { return false; }
            return true;
        }

        protected CancellationTokenSource _currentRepeatedTrackTimeRequestCancellationTokenSource;

        protected async void BeginRepeatedTrackTimeRequest()
        {
            TimeSpan delayTimeSpan = TimeSpan.FromMilliseconds(250);
            CancellationToken token;

            // Cancel any previous requests
            if (_currentRepeatedTrackTimeRequestCancellationTokenSource != null)
                _currentRepeatedTrackTimeRequestCancellationTokenSource.Cancel();

            _currentRepeatedTrackTimeRequestCancellationTokenSource = new CancellationTokenSource();
            token = _currentRepeatedTrackTimeRequestCancellationTokenSource.Token;

            while (IsConnected && !token.IsCancellationRequested && (PlayState == PlayStates.FastForward || PlayState == PlayStates.Rewind))
            {
                await UpdateTrackTimeAsync().ConfigureAwait(false);

#if WP7
                await TaskEx.Delay(delayTimeSpan);
#else
                await Task.Delay(delayTimeSpan);
#endif
            }
        }

        #endregion

        #region Volume Status

        protected async Task<bool> UpdateCurrentVolumeLevelAsync()
        {
            DACPRequest request = new DACPRequest("/ctrl-int/1/getproperty");
            request.QueryParameters["properties"] = "dmcp.volume";

            try
            {
                var response = await SubmitRequestAsync(request).ConfigureAwait(false);
                var nodes = DACPNodeDictionary.Parse(response.Nodes);

                CurrentVolume = (byte)nodes.GetInt("cmvo");
            }
            catch { return false; }
            return true;
        }

        protected async Task<bool> SetVolumeLevelAsync(int value)
        {
            DACPRequest request = new DACPRequest("/ctrl-int/1/setproperty");
            request.QueryParameters["dmcp.volume"] = value.ToString();

            try
            {
                await SubmitRequestAsync(request).ConfigureAwait(false);
            }
            catch { return false; }
            return true;
        }

        #endregion

        #region User/Star Ratings

        private int _ratingUpdatedForSongID = 0;

        protected async Task<bool> UpdateCurrentSongUserRatingAsync()
        {
            // Make sure we have all the values we need
            if (CurrentDatabaseID == 0 || CurrentContainerID == 0 || CurrentItemID == 0 || CurrentAlbumPersistentID == 0)
            {
                ClearCurrentSongUserRating();
                return true;
            }

            // Make sure this is for the main DB
            if (!ShowUserRating || MainDatabase == null || CurrentDatabaseID != MainDatabase.ID)
            {
                ClearCurrentSongUserRating();
                return true;
            }

            // If we're requesting the rating for a new song, clear out the old value
            if (CurrentItemID != _ratingUpdatedForSongID)
                ClearCurrentSongUserRating();

            DACPRequest request = new DACPRequest("/databases/{0}/containers/{1}/items", CurrentDatabaseID, CurrentContainerID);
            request.QueryParameters["meta"] = "dmap.itemid,dmap.containeritemid,daap.songuserrating";
            request.QueryParameters["type"] = "music";
            request.QueryParameters["sort"] = "album";
            var query = DACPQueryCollection.And(DACPQueryPredicate.Is("daap.songalbumid", CurrentAlbumPersistentID), DACPQueryPredicate.Is("dmap.itemid", CurrentItemID));
            request.QueryParameters["query"] = query.ToString();

            try
            {
                var response = await SubmitRequestAsync(request).ConfigureAwait(false);
                var mlcl = response.Nodes.First(n => n.Key == "mlcl");

                var songNodes = DACPUtility.GetResponseNodes(mlcl.Value);
                foreach (var songData in songNodes)
                {
                    var nodes = DACPNodeDictionary.Parse(songData.Value);
                    var id = nodes.GetInt("miid");
                    if (id != CurrentItemID)
                        continue;
                    var rating = nodes.GetByte("asur");
                    SetCurrentSongUserRatingFromServer(rating);
                    break;
                }
            }
            catch
            {
                ClearCurrentSongUserRating();
                return false;
            }
            return true;
        }

        private void ClearCurrentSongUserRating()
        {
            _CurrentSongUserRating = 0;
            _ratingUpdatedForSongID = 0;
            PropertyChanged.RaiseOnUIThread(this, "CurrentSongUserRating");
        }

        private void SetCurrentSongUserRatingFromServer(int serverValue)
        {
            int rating = serverValue / 20;
            _CurrentSongUserRating = rating;
            _ratingUpdatedForSongID = CurrentItemID;
            PropertyChanged.RaiseOnUIThread(this, "CurrentSongUserRating");
        }

        protected async Task<bool> SetCurrentItemUserRatingAsync(int rating)
        {
            if (CurrentItemID != 0)
                return await SetUserRatingAsync(rating, CurrentItemID).ConfigureAwait(false);
            return false;
        }

        protected async Task<bool> SetUserRatingAsync(int rating, int songID)
        {
            DACPRequest request = new DACPRequest("/ctrl-int/1/setproperty");
            request.QueryParameters["dacp.userrating"] = rating.ToString();
            request.QueryParameters["database-spec"] = DACPQueryPredicate.Is("dmap.persistentid", "0x" + CurrentDatabaseID.ToString("x16")).ToString();
            request.QueryParameters["item-spec"] = DACPQueryPredicate.Is("dmap.itemid", "0x" + songID.ToString("x")).ToString();

            try
            {
                await SubmitRequestAsync(request).ConfigureAwait(false);
            }
            catch { return false; }
            return true;
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

        protected async Task<bool> UpdatePlayQueueContentsAsync()
        {
            if (!SupportsPlayQueue)
                return false;

            DACPRequest request = new DACPRequest("/ctrl-int/1/playqueue-contents");
            request.QueryParameters["span"] = "50";

            try
            {
                var response = await SubmitRequestAsync(request).ConfigureAwait(false);
                HandlePlayQueueResponse(response.Nodes);
            }
            catch { return false; }
            return true;
        }

        private void HandlePlayQueueResponse(IEnumerable<DACPNode> responseNodes)
        {
            if (PlayQueues == null)
                PlayQueues = new ObservableCollection<PlayQueue>();

            List<PlayQueue> queues = new List<PlayQueue>();
            List<PlayQueueItem> queueItems = new List<PlayQueueItem>();

            var mlcl = responseNodes.FirstOrDefault(n => n.Key == "mlcl");
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
            ThreadUtility.RunOnUIThread(() =>
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

        #region iTunes Radio

        public async Task<bool> SendiTunesRadioPlayMoreLikeThisAsync()
        {
            if (!IsCurrentlyPlayingiTunesRadio)
                return false;

            IsiTunesRadioSongFavorited = true;

            DACPRequest request = new DACPRequest("/ctrl-int/1/setproperty");
            request.QueryParameters["com.apple.itunes.liked-state"] = "2";
            request.QueryParameters["database-spec"] = DACPQueryPredicate.Is("dmap.itemid", "0x" + CurrentDatabaseID.ToString("x")).ToString();
            request.QueryParameters["item-spec"] = DACPQueryPredicate.Is("dmap.itemid", "0x" + CurrentItemID.ToString("x")).ToString();

            try { await SubmitRequestAsync(request); }
            catch { return false; }
            return true;
        }

        public async Task<bool> SendiTunesRadioNeverPlayThisSongAsync()
        {
            if (!IsCurrentlyPlayingiTunesRadio)
                return false;

            DACPRequest request = new DACPRequest("/ctrl-int/1/setproperty");
            request.QueryParameters["com.apple.itunes.liked-state"] = "3";
            request.QueryParameters["database-spec"] = DACPQueryPredicate.Is("dmap.itemid", "0x" + CurrentDatabaseID.ToString("x")).ToString();
            request.QueryParameters["item-spec"] = DACPQueryPredicate.Is("dmap.itemid", "0x" + CurrentItemID.ToString("x")).ToString();

            try { await SubmitRequestAsync(request); }
            catch { return false; }
            return true;
        }

        #endregion

        #region Genius Shuffle

        public async Task<bool> SendGeniusShuffleCommandAsync()
        {
            if (!SupportsGeniusShuffle)
                return false;

            DACPRequest request = new DACPRequest("/ctrl-int/1/genius-shuffle");
            // Apple's Remote seems to always set "span" to "$Q"
            request.QueryParameters["span"] = "$Q";

            try { await SubmitRequestAsync(request); }
            catch { return false; }
            return true;
        }

        #endregion

        #region Apple TV

        private const int AppleTVEncryptionKey = 0x4567;

        #region Remote Control Buttons

        private async Task<bool> SendAppleTVControlCommandAsync(string command)
        {
            List<byte> contentBytes = new List<byte>();
            contentBytes.AddRange(DACPUtility.GetDACPFormattedBytes("cmcc", "0"));
            contentBytes.AddRange(DACPUtility.GetDACPFormattedBytes("cmbe", command));

            ByteArrayContent content = new ByteArrayContent(contentBytes.ToArray());

            DACPRequest request = new DACPRequest("/ctrl-int/1/controlpromptentry");
            request.HttpContent = content;

            try
            {
                await SubmitRequestAsync(request).ConfigureAwait(false);
            }
            catch { return false; }
            return true;
        }

        /// <summary>
        /// Menu command.
        /// </summary>
        public Task<bool> SendAppleTVMenuCommandAsync()
        {
            return SendAppleTVControlCommandAsync("menu");
        }

        /// <summary>
        /// Alternate menu command (equivalent to pressing and holding the Select button).
        /// </summary>
        public Task<bool> SendAppleTVContextMenuCommandAsync()
        {
            return SendAppleTVControlCommandAsync("contextmenu");
        }

        /// <summary>
        /// Top menu command (equivalent to pressing and holding the Menu button).
        /// </summary>
        public Task<bool> SendAppleTVTopMenuCommandAsync()
        {
            return SendAppleTVControlCommandAsync("topmenu");
        }

        /// <summary>
        /// Select command.
        /// </summary>
        public Task<bool> SendAppleTVSelectCommand()
        {
            return SendAppleTVControlCommandAsync("select");
        }

        #endregion

        #region Control Prompt Update

        private int _controlPromptUpdateNumber = 1;
        private CancellationTokenSource _currentControlPromptUpdateCancellationTokenSource;

        protected Task<bool> GetFirstControlPromptUpdateAsync()
        {
            _controlPromptUpdateNumber = 1;
            return GetControlPromptUpdateAsync(CancellationToken.None);
        }

        protected async Task<bool> GetControlPromptUpdateAsync(CancellationToken cancellationToken)
        {
            DACPRequest request = new DACPRequest("/controlpromptupdate");
            request.QueryParameters["prompt-id"] = _controlPromptUpdateNumber.ToString();

            try
            {
                var response = await SubmitRequestAsync(request).ConfigureAwait(false);

                if (cancellationToken.IsCancellationRequested)
                    return false;

                _controlPromptUpdateNumber = response.Nodes.First(n => n.Key == "miid").Value.GetInt32Value();

                // Parse response
                // This comes back as a list of string key/value pairs.
                var nodeDictionary = response.Nodes.Where(n => n.Key == "mdcl").Select(n => DACPNodeDictionary.Parse(n.Value)).ToDictionary(n => n.GetString("cmce"), n => n.GetString("cmcv"));
                if (nodeDictionary.ContainsKey("kKeybMsgKey_MessageType"))
                {
                    switch (nodeDictionary["kKeybMsgKey_MessageType"])
                    {
                        case "5": // Trackpad interface update
                            _appleTVTrackpadPort = int.Parse(nodeDictionary["kKeybMsgKey_String"]) ^ AppleTVEncryptionKey;
                            _appleTVTrackpadKey = BitUtility.NetworkToHostOrder(int.Parse(nodeDictionary["kKeybMsgKey_SubText"]) ^ AppleTVEncryptionKey);
                            _log.Debug("Apple TV virtual trackpad parameters updated: Encryption key: {0:X8} Port: {1}", _appleTVTrackpadKey, _appleTVTrackpadPort);
                            break;
                    }
                }
            }
            catch
            {
                return false;
            }
            return true;
        }

        protected async void SubscribeToControlPromptUpdates()
        {
            TimeSpan autoCancelTimeSpan = TimeSpan.FromSeconds(45);
            TimeSpan resubmitDelay = TimeSpan.FromSeconds(2);
            CancellationToken token;

            while (IsConnected)
            {
                _currentControlPromptUpdateCancellationTokenSource = new CancellationTokenSource();
                token = _currentControlPromptUpdateCancellationTokenSource.Token;

#if WP7
                var updateTask = GetControlPromptUpdateAsync(token);
                var cancelTask = TaskEx.Delay(autoCancelTimeSpan, token);
                await TaskEx.WhenAny(updateTask, cancelTask).ConfigureAwait(false);
#else
                var updateTask = GetControlPromptUpdateAsync(token);
                var cancelTask = Task.Delay(autoCancelTimeSpan, token);
                await Task.WhenAny(updateTask, cancelTask).ConfigureAwait(false);
#endif

                if (token.IsCancellationRequested)
                    return;

                _currentControlPromptUpdateCancellationTokenSource.Cancel();

                if (updateTask.Status == TaskStatus.RanToCompletion)
                {
                    if (updateTask.Result == false)
                    {
                        SendConnectionError();
                        return;
                    }

#if WP7
                    await TaskEx.Delay(resubmitDelay).ConfigureAwait(false);
#else
                    await Task.Delay(resubmitDelay).ConfigureAwait(false);
#endif
                }
            }
        }

        private async Task<bool> RequestAppleTVTrackpadInfoUpdateAsync()
        {
            List<byte> contentBytes = new List<byte>();
            contentBytes.AddRange(DACPUtility.GetDACPFormattedBytes("cmcc", "0"));
            contentBytes.AddRange(DACPUtility.GetDACPFormattedBytes("cmbe", "DRPortInfoRequest"));
            contentBytes.AddRange(DACPUtility.GetDACPFormattedBytes("cmte", string.Format("{0},0x{1}", AppleTVEncryptionKey, PairingCode)));

            ByteArrayContent content = new ByteArrayContent(contentBytes.ToArray());

            DACPRequest request = new DACPRequest("/ctrl-int/1/controlpromptentry");
            request.HttpContent = content;

            try
            {
                await SubmitRequestAsync(request).ConfigureAwait(false);
            }
            catch { return false; }
            return true;
        }

        #endregion

        #region Virtual Trackpad

        private int _appleTVTrackpadPort;
        private int _appleTVTrackpadKey;

        private readonly SemaphoreSlim _trackpadSemaphore = new SemaphoreSlim(0, 1);
        private CancellationTokenSource _trackpadCancellationTokenSource;
        private short _trackpadX;
        private short _trackpadY;

        //private Windows.Networking.Sockets.StreamSocket _trackpadSocket;

        public async void StartAppleTVTrackpadControl(short x, short y)
        {
            // Cancel any previous connections
            var cancellationTokenSource = _trackpadCancellationTokenSource;
            if (cancellationTokenSource != null)
                cancellationTokenSource.Cancel();
            cancellationTokenSource = new CancellationTokenSource();
            _trackpadCancellationTokenSource = cancellationTokenSource;

            // Set up initial message
            int[] message = new int[8];
            message[0] = 0x00000020;
            message[1] = 0x00010000;
            message[2] = 0x00000100; // Touch down
            message[3] = 0x00000000;
            message[4] = 0x0000000C;
            message[5] = 0x00000000; // Initial time = 0
            message[6] = 0x00000001;
            message[7] = ((x << 16) + y);
            byte[] encodedMessage = EncodeAppleTVTrackpadMessage(message);

            // Make socket connection
            using (var socket = new Windows.Networking.Sockets.StreamSocket())
            {
                try
                {
                    await socket.ConnectAsync(new Windows.Networking.HostName(Hostname), _appleTVTrackpadPort.ToString()).AsTask().ConfigureAwait(false);
                }
                catch
                {
                    RequestAppleTVTrackpadInfoUpdateAsync();
                    return;
                }
                if (cancellationTokenSource.IsCancellationRequested)
                    return;

                using (var writer = new Windows.Storage.Streams.DataWriter(socket.OutputStream))
                {
                    DateTime startTime = DateTime.Now;

                    // Write initial message
                    writer.WriteBytes(encodedMessage);
                    await writer.StoreAsync().AsTask().ConfigureAwait(false);

                    message[2] = 0x00000101; // Touch move

                    bool closeAfterSend = false;

                    // Write successive messages
                    while (true)
                    {
                        try
                        {
                            await _trackpadSemaphore.WaitAsync(cancellationTokenSource.Token).ConfigureAwait(false);
                        }
                        catch { }

                        message[5] = (int)(DateTime.Now - startTime).TotalMilliseconds;
                        message[7] = ((_trackpadX << 16) + _trackpadY);

                        if (cancellationTokenSource.IsCancellationRequested)
                        {
                            message[2] = 0x00000102; // Touch up
                            closeAfterSend = true;
                        }

                        encodedMessage = EncodeAppleTVTrackpadMessage(message);
                        writer.WriteBytes(encodedMessage);
                        await writer.StoreAsync().AsTask().ConfigureAwait(false);

                        if (closeAfterSend)
                        {
                            //writer.DetachStream(); // TODO: Is this needed?
                            return;
                        }
                    }

                }
            }
        }

        public void MoveAppleTVTrackpadControl(short x, short y)
        {
            _trackpadX = x;
            _trackpadY = y;

            lock (_trackpadSemaphore)
            {
                if (_trackpadSemaphore.CurrentCount < 1)
                    _trackpadSemaphore.Release();
            }
        }

        public void ReleaseAppleTVTrackpadControl(short x, short y)
        {
            _trackpadX = x;
            _trackpadY = y;

            var cancellationTokenSource = _trackpadCancellationTokenSource;
            if (cancellationTokenSource == null)
                return;
            cancellationTokenSource.Cancel();
        }

        private byte[] EncodeAppleTVTrackpadMessage(int[] message)
        {
            byte[] result = new byte[message.Length * 4];
            int encoded;
            int offset;
            byte[] bytes;
            for (int i = 0; i < message.Length; i++)
            {
                encoded = message[i] ^ _appleTVTrackpadKey;
                offset = i * 4;
                bytes = BitConverter.GetBytes(encoded);
                result[offset + 0] = bytes[3];
                result[offset + 1] = bytes[2];
                result[offset + 2] = bytes[1];
                result[offset + 3] = bytes[0];
            }

            return result;
        }

        #endregion

        #endregion
    }
}
