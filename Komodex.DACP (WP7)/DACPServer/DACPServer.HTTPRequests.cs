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
            return await SubmitRequestAsync(request.GetURI(), request.CancellationToken).ConfigureAwait(false);
        }

        internal Task<DACPResponse> SubmitRequestAsync(string uri)
        {
            return SubmitRequestAsync(uri, CancellationToken.None);
        }

        internal async Task<DACPResponse> SubmitRequestAsync(string uri, CancellationToken cancellationToken)
        {
            _log.Info("Submitting request for: " + uri);

            HttpResponseMessage response = await HttpClient.PostAsync(uri, null, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            byte[] data = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);

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

        protected async Task<bool> LoginAsync()
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
                    return false;

                SessionID = nodes.GetInt("mlid");
            }
            catch (Exception e)
            {
                HandleHTTPException(request, e);
                return false;
            }

            return true;
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
                Utility.BeginInvokeOnUIThread(() =>
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
                ShuffleState = nodes.GetBool("cash");
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

                // If the song ID changed, refresh the album art
                if (oldSongID != CurrentItemID)
                    PropertyChanged.RaiseOnUIThread(this, "CurrentAlbumArtURL");

                Utility.BeginInvokeOnUIThread(() =>
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


        public string DACPQuery { get; set; }
    }
}
