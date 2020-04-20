using Komodex.Common;
using Komodex.DACP.Databases;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Komodex.DACP
{
    public sealed partial class DacpClient
    {
        #region Server Properties

        private bool _isConnected;
        public bool IsConnected
        {
            get { return _isConnected; }
            private set
            {
                if (_isConnected == value)
                    return;
                _isConnected = value;
                SendPropertyChanged();
            }
        }

        private string _serverName;
        public string ServerName
        {
            get { return _serverName; }
            private set
            {
                if (_serverName == value)
                    return;
                _serverName = value;
                SendPropertyChanged();
            }
        }

        private string _serverVersion;
        public string ServerVersion
        {
            get { return _serverVersion; }
            private set
            {
                if (_serverVersion == value)
                    return;
                _serverVersion = value;
                SendPropertyChanged();
            }
        }

        private bool _serverSupportsPlayQueue;
        public bool ServerSupportsPlayQueue
        {
            get { return _serverSupportsPlayQueue; }
            private set
            {
                if (_serverSupportsPlayQueue == value)
                    return;
                _serverSupportsPlayQueue = value;
                SendPropertyChanged();
            }
        }

        private bool _serverSupportsGeniusShuffle;
        public bool ServerSupportsGeniusShuffle
        {
            get { return _serverSupportsGeniusShuffle; }
            private set
            {
                if (_serverSupportsGeniusShuffle == value)
                    return;
                _serverSupportsGeniusShuffle = value;
                SendPropertyChanged();
            }
        }

        private string[] _serverMacAddresses;
        public string[] ServerMacAddresses
        {
            get { return _serverMacAddresses; }
            private set
            {
                if (_serverMacAddresses == value)
                    return;
                _serverMacAddresses = value;
                SendPropertyChanged();
            }
        }

        #endregion

        #region Play Status

        private string _currentArtistName;
        public string CurrentArtistName
        {
            get { return _currentArtistName; }
            private set
            {
                if (_currentArtistName == value)
                    return;
                _currentArtistName = value;
                SendPropertyChanged();
            }
        }

        private string _currentAlbumName;
        public string CurrentAlbumName
        {
            get { return _currentAlbumName; }
            private set
            {
                if (_currentAlbumName == value)
                    return;
                _currentAlbumName = value;
                SendPropertyChanged();
            }
        }

        private string _currentSongName;
        public string CurrentSongName
        {
            get { return _currentSongName; }
            private set
            {
                if (_currentSongName == value)
                    return;
                _currentSongName = value;
                SendPropertyChanged();
            }
        }

        private int _currentDatabaseID;
        public int CurrentDatabaseID
        {
            get { return _currentDatabaseID; }
            private set
            {
                if (_currentDatabaseID == value)
                    return;
                _currentDatabaseID = value;
                SendPropertyChanged();
            }
        }

        private int _currentItemID;
        public int CurrentItemID
        {
            get { return _currentItemID; }
            private set
            {
                if (_currentItemID == value)
                    return;
                _currentItemID = value;
                SendPropertyChanged();
            }
        }

        private int _currentContainerID;
        public int CurrentContainerID
        {
            get { return _currentContainerID; }
            private set
            {
                if (_currentContainerID == value)
                    return;
                _currentContainerID = value;
                SendPropertyChanged();
            }
        }

        private int _currentContainerItemID;
        public int CurrentContainerItemID
        {
            get { return _currentContainerItemID; }
            private set
            {
                if (_currentContainerItemID == value)
                    return;
                _currentContainerItemID = value;
                SendPropertyChanged();
            }
        }

        private UInt64 _currentAlbumPersistentID;
        public UInt64 CurrentAlbumPersistentID
        {
            get { return _currentAlbumPersistentID; }
            private set
            {
                if (_currentAlbumPersistentID == value)
                    return;
                _currentAlbumPersistentID = value;
                SendPropertyChanged();
            }
        }

        private string _currentItemSignature;
        /// <summary>
        /// String that represents the values of the current item ID, database ID, etc.
        /// CurrentItemSignature can be used as a trigger for updating data binding whenever the current item changes.
        /// </summary>
        public string CurrentItemSignature
        {
            get { return _currentItemSignature; }
            private set
            {
                if (_currentItemSignature == value)
                    return;
                _currentItemSignature = value;
                SendPropertyChanged();
            }
        }

        private PlayState _currentPlayState;
        public PlayState CurrentPlayState
        {
            get { return _currentPlayState; }
            private set
            {
                if (_currentPlayState == value)
                    return;
                _currentPlayState = value;
                SendPropertyChanged();
            }
        }

        #endregion

        #region Shuffle/Repeat

        private bool _isShuffleAvailable;
        public bool IsShuffleAvailable
        {
            get { return _isShuffleAvailable; }
            private set
            {
                if (_isShuffleAvailable == value)
                    return;
                _isShuffleAvailable = value;
                SendPropertyChanged();
            }
        }

        private bool _currentShuffleMode;
        public bool CurrentShuffleMode
        {
            get { return _currentShuffleMode; }
            private set
            {
                if (_currentShuffleMode == value)
                    return;
                _currentShuffleMode = value;
                SendPropertyChanged();
            }
        }

        private bool _isRepeatOneAvailable;
        public bool IsRepeatOneAvailable
        {
            get { return _isRepeatOneAvailable; }
            private set
            {
                if (_isRepeatOneAvailable == value)
                    return;
                _isRepeatOneAvailable = value;
                SendPropertyChanged();
            }
        }

        private bool _isRepeatAllAvailable;
        public bool IsRepeatAllAvailable
        {
            get { return _isRepeatAllAvailable; }
            private set
            {
                if (_isRepeatAllAvailable == value)
                    return;
                _isRepeatAllAvailable = value;
                SendPropertyChanged();
            }
        }

        private bool _isRepeatAvailable;
        public bool IsRepeatAvailable
        {
            get { return _isRepeatAvailable; }
            private set
            {
                if (_isRepeatAvailable == value)
                    return;
                _isRepeatAvailable = value;
                SendPropertyChanged();
            }
        }

        private RepeatMode _currentRepeatMode;
        public RepeatMode CurrentRepeatMode
        {
            get { return _currentRepeatMode; }
            private set
            {
                if (_currentRepeatMode == value)
                    return;
                _currentRepeatMode = value;
                SendPropertyChanged();
            }
        }

        #endregion

        #region Track Time/Position

        private CancellationTokenSource _trackTimePositionUpdateCancellationTokenSource;

        private async void UpdateTrackTime(int durationMilliseconds, int? timeRemainingMilliseconds)
        {
            // In order to prevent issues with Slider controls, we need to raise PropertyChanged notifications synchronously from the UI thread.
            if (!ThreadUtility.IsOnUIThread)
            {
                ThreadUtility.RunOnUIThread(() => UpdateTrackTime(durationMilliseconds, timeRemainingMilliseconds));
                return;
            }

            if (_trackTimePositionUpdateCancellationTokenSource != null)
            {
                _trackTimePositionUpdateCancellationTokenSource.Cancel();
                _trackTimePositionUpdateCancellationTokenSource = null;
            }

            _ignoreBoundTrackTimePositionChanges = true;
            CurrentTrackDuration = TimeSpan.FromMilliseconds(durationMilliseconds);
            CurrentTrackDurationSeconds = CurrentTrackDuration.TotalSeconds;
            _ignoreBoundTrackTimePositionChanges = false;

            int remaining = timeRemainingMilliseconds ?? durationMilliseconds;
            if (remaining > durationMilliseconds)
                remaining = durationMilliseconds;

            CancellationToken token = CancellationToken.None;

            do
            {
                CurrentTrackTimeRemaining = TimeSpan.FromMilliseconds(remaining);
                CurrentTrackTimePosition = TimeSpan.FromMilliseconds(durationMilliseconds - remaining);
                CurrentTrackTimePositionSeconds = CurrentTrackTimePosition.TotalSeconds;

                if (CurrentPlayState != PlayState.Playing)
                    break;

                if (token == CancellationToken.None)
                {
                    _trackTimePositionUpdateCancellationTokenSource = new CancellationTokenSource();
                    token = _trackTimePositionUpdateCancellationTokenSource.Token;
                }

                DateTime dt = DateTime.Now;
                await Task.Delay(1000);

                // Adjust remaining time
                remaining -= (int)(DateTime.Now - dt).TotalMilliseconds;
                if (remaining < 0)
                    remaining = 0;
            } while (IsConnected && !token.IsCancellationRequested);
        }

        private TimeSpan _currentTrackDuration;
        public TimeSpan CurrentTrackDuration
        {
            get { return _currentTrackDuration; }
            private set
            {
                if (_currentTrackDuration == value)
                    return;
                _currentTrackDuration = value;
                SendPropertyChanged();
            }
        }

        private TimeSpan _currentTrackTimeRemaining;
        public TimeSpan CurrentTrackTimeRemaining
        {
            get { return _currentTrackTimeRemaining; }
            private set
            {
                if (_currentTrackTimeRemaining == value)
                    return;
                _currentTrackTimeRemaining = value;
                SendPropertyChanged();
            }
        }

        private TimeSpan _currentTrackTimePosition;
        public TimeSpan CurrentTrackTimePosition
        {
            get { return _currentTrackTimePosition; }
            private set
            {
                if (_currentTrackTimePosition == value)
                    return;
                _currentTrackTimePosition = value;
                SendPropertyChanged();
            }
        }

        private double _currentTrackDurationSeconds;
        public double CurrentTrackDurationSeconds
        {
            get { return _currentTrackDurationSeconds; }
            private set
            {
                if (_currentTrackDurationSeconds == value)
                    return;
                _currentTrackDurationSeconds = value;
                SendPropertyChanged();
            }
        }

        private double _currentTrackTimePositionSeconds;
        public double CurrentTrackTimePositionSeconds
        {
            get { return _currentTrackTimePositionSeconds; }
            private set
            {
                if (_currentTrackTimePositionSeconds == value)
                    return;
                _currentTrackTimePositionSeconds = value;
                SendPropertyChanged();
                if (!_updatingBoundTrackTimePosition)
                    SendPropertyChanged("BindableTrackTimePositionSeconds");
            }
        }

        public double BindableTrackTimePositionSeconds
        {
            get { return _currentTrackTimePositionSeconds; }
            set { UpdateBoundTrackTimePosition(value); }
        }

        private double _newBoundTrackTimePosition;
        private bool _updatingBoundTrackTimePosition;
        private bool _ignoreBoundTrackTimePositionChanges;

        private async void UpdateBoundTrackTimePosition(double position)
        {
            if (_ignoreBoundTrackTimePositionChanges)
                return;

            _newBoundTrackTimePosition = position;
            if (_updatingBoundTrackTimePosition)
                return;

            _updatingBoundTrackTimePosition = true;

            bool success;
            double value;

            do
            {
                value = _newBoundTrackTimePosition;
                success = await SetTrackTimePositionAsync(TimeSpan.FromSeconds(value)).ConfigureAwait(false);
            } while (success && value != _newBoundTrackTimePosition);

            _newBoundTrackTimePosition = -1;
            _updatingBoundTrackTimePosition = false;
        }

        private bool _isTrackTimePositionBarEnabled;
        public bool IsTrackTimePositionBarEnabled
        {
            get { return _isTrackTimePositionBarEnabled; }
            private set
            {
                if (_isTrackTimePositionBarEnabled == value)
                    return;
                _isTrackTimePositionBarEnabled = value;
                SendPropertyChanged();
            }
        }

        #endregion

        #region Visualizer

        private bool _isVisualizerAvailable;
        public bool IsVisualizerAvailable
        {
            get { return _isVisualizerAvailable; }
            private set
            {
                if (_isVisualizerAvailable == value)
                    return;
                _isVisualizerAvailable = value;
                SendPropertyChanged();
            }
        }

        private bool _isVisualizerEnabled;
        public bool IsVisualizerEnabled
        {
            get { return _isVisualizerEnabled; }
            private set
            {
                if (_isVisualizerEnabled == value)
                    return;
                _isVisualizerEnabled = value;
                SendPropertyChanged();
            }
        }

        #endregion

        #region Full Screen

        private bool _isFullScreenModeAvailable;
        public bool IsFullScreenModeAvailable
        {
            get { return _isFullScreenModeAvailable; }
            private set
            {
                if (_isFullScreenModeAvailable == value)
                    return;
                _isFullScreenModeAvailable = value;
                SendPropertyChanged();
            }
        }

        private bool _isFullScreenModeEnabled;
        public bool IsFullScreenModeEnabled
        {
            get { return _isFullScreenModeEnabled; }
            private set
            {
                if (_isFullScreenModeEnabled == value)
                    return;
                _isFullScreenModeEnabled = value;
                SendPropertyChanged();
            }
        }

        #endregion

        #region Volume Level

        private int _currentVolumeLevel;
        public int CurrentVolumeLevel
        {
            get { return _currentVolumeLevel; }
            private set
            {
                if (_currentVolumeLevel == value)
                    return;
                _currentVolumeLevel = value;
                SendPropertyChanged();
                if (!_updatingBoundVolumeLevel)
                    SendPropertyChanged("BindableVolumeLevel");
            }
        }

        public int BindableVolumeLevel
        {
            get { return _currentVolumeLevel; }
            set { UpdateBoundVolumeLevel(value); }
        }

        private int _newBoundVolumeLevel;
        private bool _updatingBoundVolumeLevel;

        private async void UpdateBoundVolumeLevel(int volumeLevel)
        {
            _newBoundVolumeLevel = volumeLevel;
            if (_updatingBoundVolumeLevel)
                return;

            _updatingBoundVolumeLevel = true;

            bool success;
            int value;

            do
            {
                value = _newBoundVolumeLevel;
                success = await SetVolumeLevelAsync(value).ConfigureAwait(false);
            } while (success && value != _newBoundVolumeLevel);

            _newBoundVolumeLevel = -1;
            _updatingBoundVolumeLevel = false;
        }

        #endregion

        #region AirPlay Speakers

        private List<AirPlaySpeaker> _speakers;
        public List<AirPlaySpeaker> Speakers
        {
            get { return _speakers; }
            private set
            {
                if (_speakers == value)
                    return;
                _speakers = value;
                SendPropertyChanged();
            }
        }

        #endregion

        #region Now Playing Artwork

        private string _nowPlayingAlbumArtUriFormat;
        public string NowPlayingAlbumArtUriFormat
        {
            get { return _nowPlayingAlbumArtUriFormat; }
            private set
            {
                if (_nowPlayingAlbumArtUriFormat == value)
                    return;
                _nowPlayingAlbumArtUriFormat = value;
                SendPropertyChanged();
            }
        }

        private void UpdateNowPlayingAlbumArtUri()
        {
            NowPlayingAlbumArtUriFormat = HttpPrefix + "/ctrl-int/1/nowplayingartwork?mw={w}&mh={h}&session-id=" + SessionID;
        }

        #endregion

        #region iTunes Radio

        private bool _isPlayingiTunesRadio;
        public bool IsPlayingiTunesRadio
        {
            get { return _isPlayingiTunesRadio; }
            private set
            {
                if (_isPlayingiTunesRadio == value)
                    return;
                _isPlayingiTunesRadio = value;
                SendPropertyChanged();
            }
        }

        private iTunesRadioControlState _currentiTunesRadioControlState;
        public iTunesRadioControlState CurrentiTunesRadioControlState
        {
            get { return _currentiTunesRadioControlState; }
            private set
            {
                if (_currentiTunesRadioControlState == value)
                    return;
                _currentiTunesRadioControlState = value;
                SendPropertyChanged();
            }
        }

        private string _currentiTunesRadioStationName;
        public string CurrentiTunesRadioStationName
        {
            get { return _currentiTunesRadioStationName; }
            private set
            {
                if (_currentiTunesRadioStationName == value)
                    return;
                _currentiTunesRadioStationName = value;
                SendPropertyChanged();
            }
        }

        #endregion

        #region Genius Shuffle

        private bool _isPlayingGeniusShuffle;
        public bool IsPlayingGeniusShuffle
        {
            get { return _isPlayingGeniusShuffle; }
            private set
            {
                if (_isPlayingGeniusShuffle == value)
                    return;
                _isPlayingGeniusShuffle = value;
                SendPropertyChanged();
            }
        }

        #endregion

        #region Databases

        private readonly List<DacpDatabase> _databases = new List<DacpDatabase>();
        public List<DacpDatabase> Databases
        {
            get { return _databases; }
        }

        private DacpDatabase _mainDatabase;
        public DacpDatabase MainDatabase
        {
            get { return _mainDatabase; }
            private set
            {
                if (_mainDatabase == value)
                    return;
                _mainDatabase = value;
                SendPropertyChanged();
            }
        }

        private DacpDatabase _internetRadioDatabase;
        public DacpDatabase InternetRadioDatabase
        {
            get { return _internetRadioDatabase; }
            private set
            {
                if (_internetRadioDatabase == value)
                    return;
                _internetRadioDatabase = value;
                SendPropertyChanged();
            }
        }

        private iTunesRadioDatabase _iTunesRadioDatabase;
        public iTunesRadioDatabase iTunesRadioDatabase
        {
            get { return _iTunesRadioDatabase; }
            private set
            {
                if (_iTunesRadioDatabase == value)
                    return;
                _iTunesRadioDatabase = value;
                SendPropertyChanged();
            }
        }

        private ObservableCollectionEx<DacpDatabase> _sharedDatabases = new ObservableCollectionEx<DacpDatabase>();
        public ObservableCollectionEx<DacpDatabase> SharedDatabases
        {
            get { return _sharedDatabases; }
        }

        public DacpDatabase GetDatabaseByID(int id)
        {
            return Databases.FirstOrDefault(db => db.ID == id);
        }

        public DacpDatabase CurrentDatabase
        {
            get { return GetDatabaseByID(CurrentDatabaseID); }
        }

        #endregion
    }
}
