using Komodex.Common;
using Komodex.DACP.Databases;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.Web.Http;
using Windows.Web.Http.Filters;

namespace Komodex.DACP
{
    public sealed partial class DacpClient
    {
        #region HttpClient Management

        private HttpClient _httpClient;

        private void UpdateHttpClient()
        {
            HttpPrefix = "http://" + Hostname + ":" + Port;
            UpdateNowPlayingAlbumArtUri();

            HttpBaseProtocolFilter filter = new HttpBaseProtocolFilter();

            // Never cache responses
            filter.CacheControl.WriteBehavior = HttpCacheWriteBehavior.NoCache;

            HttpClient client = new HttpClient(filter);
            client.DefaultRequestHeaders.Add("Viewer-Only-Client", "1");
            client.DefaultRequestHeaders.Add("Client-DAAP-Version", "3.11");
            client.DefaultRequestHeaders.CacheControl.ParseAdd("no-cache");

            if (_httpClient != null)
                _httpClient.Dispose();

            _httpClient = client;
        }

        internal async Task<DacpResponse> SendRequestAsync(DacpRequest request)
        {
            if (request.IncludeSessionID)
                request.QueryParameters["session-id"] = SessionID.ToString();
            return await SendRequestAsync(request.GetURI(), request.CancellationToken).ConfigureAwait(false);
        }

        internal async Task<DacpResponse> SendRequestAsync(string uri, CancellationToken cancellationToken)
        {
            _log.Info("Sending request for: " + uri);

            Task<HttpResponseMessage> task;
            lock (_httpClient)
                task = _httpClient.GetAsync(new Uri(HttpPrefix + uri)).AsTask(cancellationToken);
            HttpResponseMessage response = await task.ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
                throw new DacpRequestException(response);

            var buffer = await response.Content.ReadAsBufferAsync().AsTask(cancellationToken).ConfigureAwait(false);
            var reader = DataReader.FromBuffer(buffer);
            byte[] data = new byte[reader.UnconsumedBufferLength];
            reader.ReadBytes(data);

            _log.Info("Received response for: {0} (source: {1})", uri, response.Source);

            // Get the content of the first node
            IEnumerable<DacpNode> nodes = null;
            if (data.Length > 0)
            {
                data = DacpUtility.GetResponseNodes(data, true).First().Value;
                nodes = DacpUtility.GetResponseNodes(data);
            }

            return new DacpResponse(response, nodes);
        }

        internal async Task<bool> SendCommandAsync(DacpRequest request)
        {
            try
            {
                await SendRequestAsync(request).ConfigureAwait(false);
            }
            catch { return false; }
            return true;
        }

        internal Task<bool> SetPropertyAsync(string propertyName, string value)
        {
            DacpRequest request = new DacpRequest("/ctrl-int/1/setproperty");
            request.QueryParameters[propertyName] = value;
            return SendCommandAsync(request);
        }

        internal async Task<List<T>> GetListAsync<T>(DacpRequest request, Func<DacpNodeDictionary, T> itemGenerator, string listKey = DacpUtility.DefaultListKey)
        {
            try
            {
                var response = await SendRequestAsync(request).ConfigureAwait(false);
                return DacpUtility.GetItemsFromNodes(response.Nodes, itemGenerator, listKey).ToList();
            }
            catch (Exception)
            {
                return new List<T>();
            }
        }

        internal async Task<IDacpList> GetAlphaGroupedListAsync<T>(DacpRequest request, Func<byte[], T> itemGenerator, string listKey = DacpUtility.DefaultListKey)
        {
            try
            {
                var response = await SendRequestAsync(request).ConfigureAwait(false);
                return DacpUtility.GetAlphaGroupedDacpList(response.Nodes, itemGenerator, listKey);
            }
            catch (Exception)
            {
                return new DacpList<T>(false);
            }
        }

        internal Task<IDacpList> GetAlphaGroupedListAsync<T>(DacpRequest request, Func<DacpNodeDictionary, T> itemGenerator, string listKey = DacpUtility.DefaultListKey)
        {
            return GetAlphaGroupedListAsync(request, b => itemGenerator(DacpNodeDictionary.Parse(b)), listKey);
        }

        #endregion

        #region Requests and Responses

        #region Server Info

        private async Task<bool> GetServerInfoAsync()
        {
            DacpRequest request = new DacpRequest("/server-info");
            request.IncludeSessionID = false;

            try
            {
                var response = await SendRequestAsync(request).ConfigureAwait(false);

                // Process response
                ServerVersion = response.HTTPResponse.Headers.GetValueOrDefault("DAAP-Server");

                var nodes = DacpNodeDictionary.Parse(response.Nodes);
                ServerName = nodes.GetString("minm");
                //ServerVersion = nodes.GetInt("aeSV");
                //ServerDMAPVersion = nodes.GetInt("mpro");
                //ServerDAAPVersion = nodes.GetInt("apro");

                // MAC addresses
                if (nodes.ContainsKey("msml"))
                {
                    List<string> macAddresses = new List<string>();
                    var addressNodes = DacpUtility.GetResponseNodes(nodes["msml"]).Where(n => n.Key == "msma").Select(n => n.Value);
                    foreach (var addressNode in addressNodes)
                    {
                        var address = BitConverter.ToInt64(addressNode, 0);
                        address = address >> 16;
                        macAddresses.Add(address.ToString("X12"));
                    }
                    ServerMacAddresses = macAddresses.ToArray();
                }
            }
            catch { return false; }
            return true;
        }

        #endregion

        #region Server Capabilities (ctrl-int)

        private async Task<bool> GetServerCapabilitiesAsync()
        {
            DacpRequest request = new DacpRequest("/ctrl-int");
            request.IncludeSessionID = false;

            try
            {
                var response = await SendRequestAsync(request).ConfigureAwait(false);

                // Process response
                var mlcl = DacpUtility.GetResponseNodes(response.Nodes.First(n => n.Key == "mlcl").Value);
                var nodes = DacpNodeDictionary.Parse(mlcl.First(n => n.Key == "mlit").Value);

                if (nodes.ContainsKey("ceSX"))
                {
                    Int64 ceSX = nodes.GetLong("ceSX");

                    // Bit 0: Supports Play Queue
                    if ((ceSX & (1 << 0)) != 0)
                        ServerSupportsPlayQueue = true;

                    // Bit 1: iTunes Radio? Appeared in iTunes 11.1.2 with the iTunes Radio DB.
                    // Apple's Remote for iOS doesn't seem to use this bit to determine whether iTunes Radio is available.
                    // Instead, it looks for an iTunes Radio database and checks whether it has any containers.

                    // Bit 2: Genius Shuffle Enabled/Available
                    if ((ceSX & (1 << 2)) != 0)
                        ServerSupportsGeniusShuffle = true;
                }
            }
            catch { return false; }
            return true;
        }

        #endregion

        #region Login

        private async Task<ConnectionResult> LoginAsync()
        {
            DacpRequest request = new DacpRequest("/login");
            request.QueryParameters["pairing-guid"] = "0x" + PairingCode;
            request.IncludeSessionID = false;

            try
            {
                var response = await SendRequestAsync(request).ConfigureAwait(false);

                // Process response
                var nodes = DacpNodeDictionary.Parse(response.Nodes);

                if (!nodes.ContainsKey("mlid"))
                    return ConnectionResult.InvalidPIN;

                SessionID = nodes.GetInt("mlid");
                UpdateNowPlayingAlbumArtUri();
            }
            catch (DacpRequestException e)
            {
                int statusCode = (int)e.Response.StatusCode;
                if (statusCode >= 500 && statusCode <= 599)
                    return ConnectionResult.InvalidPIN;
                return ConnectionResult.ConnectionError;
            }
            catch { return ConnectionResult.ConnectionError; }

            return ConnectionResult.Success;
        }

        #endregion

        #region Databases

        private async Task<bool> GetDatabasesAsync()
        {
            DacpRequest request = new DacpRequest("/databases");

            try
            {
                var databases = await GetListAsync(request, n => DacpDatabase.GetDatabase(this, n)).ConfigureAwait(false);

                if (databases == null || databases.Count == 0)
                    return false;

                List<DacpDatabase> newSharedDatabases = new List<DacpDatabase>();

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
                Dictionary<int, DacpDatabase> removedSharedDBs = SharedDatabases.ToDictionary(db => db.ID);
                foreach (var sharedDB in newSharedDatabases)
                {
                    removedSharedDBs.Remove(sharedDB.ID);
                    if (SharedDatabases.Any(db => db.ID == sharedDB.ID))
                        continue;
                    SharedDatabases.Add(sharedDB);
                }
                foreach (DacpDatabase db in removedSharedDBs.Values)
                    SharedDatabases.Remove(db);

                Databases.Clear();
                Databases.AddRange(databases);
            }
            catch { return false; }
            return true;
        }

        #endregion

        #region Update

        private CancellationTokenSource _currentLibraryUpdateCancellationTokenSource;

        private Task<bool> GetFirstLibraryUpdateAsync()
        {
            CurrentLibraryUpdateNumber = 1;
            return GetLibraryUpdateAsync(CancellationToken.None);
        }

        private async Task<bool> GetLibraryUpdateAsync(CancellationToken cancellationToken)
        {
            DacpRequest request = new DacpRequest("/update");
            request.QueryParameters["revision-number"] = CurrentLibraryUpdateNumber.ToString();
            request.QueryParameters["daap-no-disconnect"] = "1";

            try
            {
                var response = await SendRequestAsync(request).ConfigureAwait(false);

                if (cancellationToken.IsCancellationRequested)
                    return false;

                var nodes = DacpNodeDictionary.Parse(response.Nodes);
                CurrentLibraryUpdateNumber = nodes.GetInt("musr");
            }
            catch { return false; }
            return true;
        }

        private async void SubscribeToLibraryUpdates()
        {
            TimeSpan resubmitDelay = TimeSpan.FromSeconds(2);

            CancellationToken token;

            while (IsConnected)
            {
                _currentLibraryUpdateCancellationTokenSource = new CancellationTokenSource();
                token = _currentLibraryUpdateCancellationTokenSource.Token;

                bool success = await GetLibraryUpdateAsync(token).ConfigureAwait(false);

                if (token.IsCancellationRequested)
                    return;

                if (success)
                    success = await GetDatabasesAsync().ConfigureAwait(false);

                if (!success)
                {
                    HandleConnectionError();
                    return;
                }

                //SendLibraryUpdate(); // todo

                await Task.Delay(resubmitDelay).ConfigureAwait(false);
            }
        }

        #endregion

        #region Play Status

        private int _playStatusRevisionNumber = 1;
        private CancellationTokenSource _currentPlayStatusCancellationTokenSource;

        private Task<bool> GetFirstPlayStatusUpdateAsync()
        {
            _playStatusRevisionNumber = 1;
            return GetPlayStatusUpdateAsync(CancellationToken.None);
        }

        private async Task<bool> GetPlayStatusUpdateAsync(CancellationToken cancellationToken)
        {
            // Do not pass the cancellation token to the HTTP request since canceling a request will cause iTunes to close the current session.
            DacpRequest request = new DacpRequest("/ctrl-int/1/playstatusupdate");
            request.QueryParameters["revision-number"] = _playStatusRevisionNumber.ToString();

            try
            {
                var response = await SendRequestAsync(request).ConfigureAwait(false);

                // Do we still need to process this response?
                if (cancellationToken.IsCancellationRequested)
                    return false;

                // Process response
                var nodes = DacpNodeDictionary.Parse(response.Nodes);
                _playStatusRevisionNumber = nodes.GetInt("cmsr");

                // Current item and container IDs
                if (nodes.ContainsKey("canp"))
                {
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
                CurrentItemSignature = string.Format("{0}|{1}|{2}|{3}", CurrentDatabaseID, CurrentContainerID, CurrentContainerItemID, CurrentItemID);

                // Current item info
                CurrentArtistName = nodes.GetString("cana");
                CurrentAlbumName = nodes.GetString("canl");
                CurrentSongName = nodes.GetString("cann");
                CurrentAlbumPersistentID = (UInt64)nodes.GetLong("asai");

                // Play state
                CurrentPlayState = (PlayState)nodes.GetByte("caps");

                // Track time
                UpdateTrackTime(nodes.GetInt("cast"), nodes.GetNullableInt("cant"));

                // Shuffle
                int caas = nodes.GetInt("caas");
                IsShuffleAvailable = (caas & (1 << 1)) != 0;
                CurrentShuffleMode = nodes.GetBool("cash");

                // Repeat
                int caar = nodes.GetInt("caar");
                IsRepeatOneAvailable = (caar & (1 << 1)) != 0;
                IsRepeatAllAvailable = (caar & (1 << 2)) != 0;
                IsRepeatAvailable = (IsRepeatOneAvailable || IsRepeatAllAvailable);
                CurrentRepeatMode = (RepeatMode)nodes.GetByte("carp");

                //CurrentMediaKind = nodes.GetInt("cmmk");
                //ShowUserRating = nodes.GetBool("casu");

                // dacp.visualizer
                //VisualizerActive = nodes.GetBool("cavs");
                // dacp.visualizerenabled
                //VisualizerAvailable = nodes.GetBool("cave");
                // dacp.fullscreen
                //FullScreenModeActive = nodes.GetBool("cafs");
                // dacp.fullscreenenabled
                //FullScreenModeAvailable = nodes.GetBool("cafe");

                // iTunes Radio
                if (iTunesRadioDatabase != null && iTunesRadioDatabase.ID == CurrentDatabaseID)
                {
                    //IsCurrentlyPlayingiTunesRadio = true;
                    //CurrentiTunesRadioStationName = nodes.GetString("ceNR");

                    // caks = 1 when the next button is disabled, and 2 when it's enabled
                    //IsiTunesRadioNextButtonEnabled = (nodes.GetByte("caks") == 2);

                    // "aelb" indicates whether the star button (iTunes Radio menu) should be enabled, but this only seems to be set to true
                    // when connected via Home Sharing. This parameter is missing when an ad is playing, so use this to determine whether
                    // the menu should be enabled.
                    //IsiTunesRadioMenuEnabled = nodes.ContainsKey("aelb");

                    //IsiTunesRadioSongFavorited = (nodes.GetByte("aels") == 2);
                }
                else
                {
                    //IsCurrentlyPlayingiTunesRadio = false;
                }


                //if (IsCurrentlyPlayingiTunesRadio)
                //{
                //    var caks = nodes.GetByte("caks");
                //    IsiTunesRadioNextButtonEnabled = !(caks == 1);
                //}

                //if (!nodes.ContainsKey("casc") || nodes.GetBool("casc") == true)
                //    IsPlayPositionBarEnabled = true;
                //else
                //    IsPlayPositionBarEnabled = false;

                // Genius Shuffle
                //IsCurrentlyPlayingGeniusShuffle = nodes.GetBool("ceGs");
                // There are two other nodes related to Genius Shuffle, "ceGS" and "aeGs" (currently unknown)

                // If the song ID changed, refresh the album art
                //if (oldSongID != CurrentItemID)
                //    PropertyChanged.RaiseOnUIThread(this, "CurrentAlbumArtURL");

                if (CurrentPlayState == PlayState.FastForward || CurrentPlayState == PlayState.Rewind)
                    BeginRepeatedTrackTimeRequests();

                await GetVolumeLevelAsync().ConfigureAwait(false);
                await GetSpeakersAsync().ConfigureAwait(false);

                //var volumeTask = UpdateCurrentVolumeLevelAsync();
                //var userRatingTask = UpdateCurrentSongUserRatingAsync();
                //var playQueueTask = UpdatePlayQueueContentsAsync();

                //Task[] tasks = new[] { volumeTask, userRatingTask, playQueueTask };

#if WP7
                //await TaskEx.WhenAll(tasks).ConfigureAwait(false);
#else
                //await Task.WhenAll(tasks).ConfigureAwait(false);
#endif

                //SubmitGetSpeakersRequest();

                PlayStatusUpdated.RaiseOnUIThread(this, new EventArgs());
            }
            catch { return false; }
            return true;
        }

        private async void SubscribeToPlayStatusUpdates()
        {
            CancellationToken token;

            while (IsConnected)
            {
                _currentPlayStatusCancellationTokenSource = new CancellationTokenSource();
                token = _currentPlayStatusCancellationTokenSource.Token;

                bool success = await GetPlayStatusUpdateAsync(token).ConfigureAwait(false);

                if (token.IsCancellationRequested)
                    return;

                if (!success)
                {
                    HandleConnectionError();
                    return;
                }
            }
        }

        #endregion

        #region Volume Level

        private async Task<bool> GetVolumeLevelAsync()
        {
            DacpRequest request = new DacpRequest("/ctrl-int/1/getproperty");
            request.QueryParameters["properties"] = "dmcp.volume";

            try
            {
                var response = await SendRequestAsync(request).ConfigureAwait(false);
                var nodes = DacpNodeDictionary.Parse(response.Nodes);

                CurrentVolumeLevel = nodes.GetInt("cmvo");
            }
            catch { return false; }
            return true;
        }

        private Task<bool> SetVolumeLevelAsync(int volumeLevel)
        {
            return SetPropertyAsync("dmcp.volume", volumeLevel.ToString());
        }

        #endregion

        #region AirPlay Speakers

        private async Task<bool> GetSpeakersAsync()
        {
            DacpRequest request = new DacpRequest("/ctrl-int/1/getspeakers");

            try
            {
                var response = await SendRequestAsync(request).ConfigureAwait(false);
                var speakerNodes = response.Nodes.Where(n => n.Key == "mdcl").Select(n => DacpNodeDictionary.Parse(n.Value)).ToList();

                var speakers = Speakers;

                // Determine whether we need to replace the list of speakers
                bool replaceSpeakers = false;
                if (speakers == null || speakers.Count != speakerNodes.Count)
                    replaceSpeakers = true;
                else
                {
                    // Determine whether we still have the same speaker IDs
                    for (int i = 0; i < speakers.Count; i++)
                    {
                        if (speakers[i].ID != (UInt64)speakerNodes[i].GetLong("msma"))
                        {
                            replaceSpeakers = true;
                            break;
                        }
                    }
                }

                // Create the new list of speakers or update the existing speakers
                if (replaceSpeakers)
                    Speakers = speakerNodes.Select(n => new AirPlaySpeaker(this, n)).ToList();
                else
                {
                    for (int i = 0; i < speakers.Count; i++)
                        speakers[i].ProcessNodes(speakerNodes[i]);
                }
            }
            catch { return false; }
            return true;
        }

        #endregion

        #region Track Time Position

        private async Task<bool> GetTrackTimePositionAsync()
        {
            DacpRequest request = new DacpRequest("/ctrl-int/1/getproperty");
            request.QueryParameters["properties"] = "dacp.playingtime";

            try
            {
                var response = await SendRequestAsync(request).ConfigureAwait(false);
                var nodes = DacpNodeDictionary.Parse(response.Nodes);

                int totalMS = nodes.GetInt("cast");
                int? remainingMS = nodes.GetNullableInt("cant");
                UpdateTrackTime(totalMS, remainingMS);
            }
            catch { return false; }
            return true;
        }

        private async void BeginRepeatedTrackTimeRequests()
        {
            int playStatusRevision = _playStatusRevisionNumber;

            do
            {
                await GetTrackTimePositionAsync().ConfigureAwait(false);
                await Task.Delay(250);
            } while (IsConnected && playStatusRevision == _playStatusRevisionNumber);
        }

        private async Task<bool> SetTrackTimePositionAsync(TimeSpan position)
        {
            bool success = await SetPropertyAsync("dacp.playingtime", position.TotalMilliseconds.ToString()).ConfigureAwait(false);

            if (!success)
                return false;

            int totalMS = (int)CurrentTrackDuration.TotalMilliseconds;
            int positionMS = (int)position.TotalMilliseconds;
            int remainingMS = totalMS - positionMS;

            UpdateTrackTime(totalMS, remainingMS);
            return true;
        }

        #endregion

        #region Play Transport Commands

        public Task<bool> SendPlayPauseCommandAsync()
        {
            DacpRequest request = new DacpRequest("/ctrl-int/1/playpause");
            return SendCommandAsync(request);
        }

        public Task<bool> SendPreviousTrackCommandAsync()
        {
            // If the track doesn't change, we won't receive a play status update
            int totalMS = (int)CurrentTrackDuration.TotalMilliseconds;
            UpdateTrackTime(totalMS, totalMS);

            DacpRequest request = new DacpRequest("/ctrl-int/1/previtem");
            return SendCommandAsync(request);
        }

        public Task<bool> SendNextTrackCommandAsync()
        {
            DacpRequest request = new DacpRequest("/ctrl-int/1/nextitem");
            return SendCommandAsync(request);
        }

        public Task<bool> SendBeginRewindCommandAsync()
        {
            DacpRequest request = new DacpRequest("/ctrl-int/1/beginrew");
            return SendCommandAsync(request);
        }

        public Task<bool> SendBeginFastForwardCommandAsync()
        {
            DacpRequest request = new DacpRequest("/ctrl-int/1/beginff");
            return SendCommandAsync(request);
        }

        public Task<bool> SendPlayResumeCommandAsync()
        {
            DacpRequest request = new DacpRequest("/ctrl-int/1/playresume");
            return SendCommandAsync(request);
        }

        #endregion

        #region Shuffle/Repeat Commands

        public Task<bool> ToggleShuffleModeAsync()
        {
            if (!IsShuffleAvailable)
                return Task.FromResult(true);

            string newMode = (CurrentShuffleMode) ? "0" : "1";
            return SetPropertyAsync("dacp.shufflestate", newMode);
        }

        public Task<bool> ToggleRepeatModeAsync()
        {
            if (!IsRepeatAvailable)
                return Task.FromResult(true);

            RepeatMode newRepeatMode = RepeatMode.None;

            switch (CurrentRepeatMode)
            {
                case RepeatMode.None:
                    if (IsRepeatAllAvailable)
                        newRepeatMode = RepeatMode.RepeatAll;
                    else if (IsRepeatOneAvailable)
                        newRepeatMode = RepeatMode.RepeatOne;
                    break;

                case RepeatMode.RepeatAll:
                    if (IsRepeatOneAvailable)
                        newRepeatMode = RepeatMode.RepeatOne;
                    break;

                case RepeatMode.RepeatOne:
                    // Just go to "repeat none" mode
                    break;
            }

            string newRepeatModeString = ((int)newRepeatMode).ToString();
            return SetPropertyAsync("dacp.repeatstate", newRepeatModeString);
        }

        #endregion

        #endregion
    }
}
