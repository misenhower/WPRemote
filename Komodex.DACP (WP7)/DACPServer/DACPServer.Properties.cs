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
using System.ComponentModel;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Linq;
using Komodex.DACP.Localization;
using Komodex.Common;
using Komodex.DACP.Databases;

namespace Komodex.DACP
{
    public partial class DACPServer : INotifyPropertyChanged
    {
        #region Connection

        private bool _IsConnected = false;
        public bool IsConnected
        {
            get { return _IsConnected; }
            protected set
            {
                if (_IsConnected == value)
                    return;
                _IsConnected = value;
                PropertyChanged.RaiseOnUIThread(this, "IsConnected");
            }
        }

        private string _LibraryName = string.Empty;
        public string LibraryName
        {
            get { return _LibraryName; }
            set
            {
                if (_LibraryName == value)
                    return;
                _LibraryName = value;
                PropertyChanged.RaiseOnUIThread(this, "LibraryName");
            }
        }

        private int _ServerVersion = 0;
        public int ServerVersion
        {
            get { return _ServerVersion; }
            protected set
            {
                if (_ServerVersion == value)
                    return;
                _ServerVersion = value;
                PropertyChanged.RaiseOnUIThread(this, "ServerVersion");
            }
        }

        private string _ServerVersionString = null;
        public string ServerVersionString
        {
            get { return _ServerVersionString; }
            set
            {
                if (_ServerVersionString == value)
                    return;
                _ServerVersionString = value;
                PropertyChanged.RaiseOnUIThread(this, "ServerVersionString");
            }
        }

        private int _ServerDMAPVersion = 0;
        public int ServerDMAPVersion
        {
            get { return _ServerDMAPVersion; }
            protected set
            {
                if (_ServerDMAPVersion == value)
                    return;
                _ServerDMAPVersion = value;
                PropertyChanged.RaiseOnUIThread(this, "ServerDMAPVersion");
            }
        }

        private int _ServerDAAPVersion = 0;
        public int ServerDAAPVersion
        {
            get { return _ServerDAAPVersion; }
            protected set
            {
                if (_ServerDAAPVersion == value)
                    return;
                _ServerDAAPVersion = value;
                PropertyChanged.RaiseOnUIThread(this, "ServerDAAPVersion");
            }
        }

        private string[] _macAddresses;
        public string[] MACAddresses
        {
            get { return _macAddresses; }
            protected set
            {
                if (_macAddresses == value)
                    return;
                _macAddresses = value;
                PropertyChanged.RaiseOnUIThread(this, "MACAddresses");
            }
        }

        private bool _GettingData = false;
        public bool GettingData
        {
            get { return _GettingData; }
            private set
            {
                if (_GettingData == value)
                    return;
                _GettingData = value;
                PropertyChanged.RaiseOnUIThread(this, "GettingData");
            }
        }

        private void UpdateGettingData(bool setToTrue = false)
        {
            if (setToTrue)
                GettingData = true;
            else
            {
                lock (PendingHttpRequests)
                    GettingData = PendingHttpRequests.Any(hri => hri.IsDataRetrieval);
            }
        }

        private bool _SupportsPlayQueue = false;
        public bool SupportsPlayQueue
        {
            get { return _SupportsPlayQueue; }
            private set
            {
                if (_SupportsPlayQueue == value)
                    return;
                _SupportsPlayQueue = value;
                PropertyChanged.RaiseOnUIThread(this, "SupportsPlayQueue");
            }
        }

        #endregion

        #region Databases

        private DACPDatabase _mainDatabase;
        public DACPDatabase MainDatabase
        {
            get { return _mainDatabase; }
            protected set
            {
                if (_mainDatabase == value)
                    return;
                _mainDatabase = value;
                PropertyChanged.RaiseOnUIThread(this, "MainDatabase");
            }
        }

        private DACPDatabase _internetRadioDatabase;
        public DACPDatabase InternetRadioDatabase
        {
            get { return _internetRadioDatabase; }
            protected set
            {
                if (_internetRadioDatabase == value)
                    return;
                _internetRadioDatabase = value;
                PropertyChanged.RaiseOnUIThread(this, "InternetRadioDatabase");
            }
        }

        private ObservableCollectionEx<DACPDatabase> _sharedDatabases = new ObservableCollectionEx<DACPDatabase>();
        public ObservableCollectionEx<DACPDatabase> SharedDatabases
        {
            get { return _sharedDatabases; }
        }

        #endregion

        #region Current Song

        private int _currentDatabaseID;
        public int CurrentDatabaseID
        {
            get { return _currentDatabaseID; }
            protected set
            {
                if (_currentDatabaseID == value)
                    return;
                _currentDatabaseID = value;
                PropertyChanged.RaiseOnUIThread(this, "CurrentDatabaseID");
            }
        }

        private int _currentItemID = 0;
        public int CurrentItemID
        {
            get { return _currentItemID; }
            protected set
            {
                if (_currentItemID == value)
                    return;
                _currentItemID = value;
                PropertyChanged.RaiseOnUIThread(this, "CurrentItemID");
            }
        }

        private int _CurrentContainerID = 0;
        public int CurrentContainerID
        {
            get { return _CurrentContainerID; }
            protected set
            {
                if (_CurrentContainerID == value)
                    return;
                _CurrentContainerID = value;
                PropertyChanged.RaiseOnUIThread(this, "CurrentContainerID");
            }
        }

        private int _CurrentContainerItemID = 0;
        public int CurrentContainerItemID
        {
            get { return _CurrentContainerItemID; }
            protected set
            {
                if (_CurrentContainerItemID == value)
                    return;
                _CurrentContainerItemID = value;
                PropertyChanged.RaiseOnUIThread(this, "CurrentContainerItemID");
            }
        }

        private string _CurrentSongName = null;
        public string CurrentSongName
        {
            get { return _CurrentSongName; }
            protected set
            {
                if (_CurrentSongName == value)
                    return;
                _CurrentSongName = value;
                PropertyChanged.RaiseOnUIThread(this, "CurrentSongName");
            }
        }

        private string _CurrentArtist = null;
        public string CurrentArtist
        {
            get { return _CurrentArtist; }
            protected set
            {
                if (_CurrentArtist == value)
                    return;
                _CurrentArtist = value;
                PropertyChanged.RaiseOnUIThread(this, "CurrentArtist");
            }
        }

        private string _CurrentAlbum = null;
        public string CurrentAlbum
        {
            get { return _CurrentAlbum; }
            protected set
            {
                if (_CurrentAlbum == value)
                    return;
                _CurrentAlbum = value;
                PropertyChanged.RaiseOnUIThread(this, "CurrentAlbum");
            }
        }

        private UInt64 _CurrentAlbumPersistentID = 0;
        public UInt64 CurrentAlbumPersistentID
        {
            get { return _CurrentAlbumPersistentID; }
            protected set
            {
                if (_CurrentAlbumPersistentID == value)
                    return;
                _CurrentAlbumPersistentID = value;
                PropertyChanged.RaiseOnUIThread(this, "CurrentAlbumPersistentID");
            }
        }

        // This is set whenever the session id is modified
        private string _CurrentAlbumArtURL = null;
        public string CurrentAlbumArtURL
        {
            get { return _CurrentAlbumArtURL; }
            set
            {
                if (_CurrentAlbumArtURL == value)
                    return;
                _CurrentAlbumArtURL = value;
                PropertyChanged.RaiseOnUIThread(this, "CurrentAlbumArtURL");
            }
        }

        #endregion

        #region User/Star Rating

        private int _CurrentSongUserRating = 0;
        public int CurrentSongUserRating
        {
            get { return _CurrentSongUserRating; }
            set
            {
                if (value < 0)
                    value = 0;
                if (value > 5)
                    value = 5;

                if (_CurrentSongUserRating == value)
                    return;

                _CurrentSongUserRating = value;
                PropertyChanged.RaiseOnUIThread(this, "CurrentSongUserRating");

                int actualRating = value * 20; // 1 star is 20, 2 is 40, 3 is 60, 4 is 80, 5 is 100
                SendUserRatingCommand(actualRating);
            }
        }

        #endregion

        #region Play Position

        private int _TrackTimeTotal = 0;
        public int TrackTimeTotal
        {
            get { return _TrackTimeTotal; }
            protected set
            {
                if (_TrackTimeTotal == value)
                    return;
                _TrackTimeTotal = value;
                SendTrackTimePropertyChanged();
            }
        }

        private DateTime _trackTimeUpdatedAt = DateTime.MinValue;
        private int _TrackTimeRemaining = 0;
        public int TrackTimeRemaining
        {
            get { return _TrackTimeRemaining; }
            protected set
            {
                // Removing this for now because of issue with repeated SendPrevItemCommand calls
                //if (_TrackTimeRemaining == value)
                //    return;

                if (value < 0)
                    value = 0;

                _TrackTimeRemaining = value;
                _trackTimeUpdatedAt = DateTime.Now;
                SendTrackTimePropertyChanged();
            }
        }


        public int CurrentTrackTimeRemaining
        {
            get
            {
                if (PlayState != PlayStates.Playing)
                    return TrackTimeRemaining;

                double adjustedMilliseconds = (DateTime.Now - _trackTimeUpdatedAt).TotalMilliseconds;
                if (adjustedMilliseconds > int.MaxValue || adjustedMilliseconds < int.MinValue)
                    return TrackTimeRemaining;

                int time = TrackTimeRemaining - (int)adjustedMilliseconds;
                if (time < 0)
                    return 0;
                return time;
            }
            set { } // TODO
        }

        public int CurrentTrackTimePosition
        {
            get { return TrackTimeTotal - CurrentTrackTimeRemaining; }
        }

        private bool ignoringTrackTimeChanges = false;
        private int sendTrackTimeChangeWhenFinished = -1;

        public double CurrentTrackTimePercentage
        {
            get
            {
                if (TrackTimeTotal == 0)
                    return 0;
                return ((double)CurrentTrackTimePosition / (double)TrackTimeTotal) * 100;
            }
            set
            {
                if (TrackTimeTotal == 0)
                    return;

                double percentage = value / 100d;
                int newPos = (int)(percentage * TrackTimeTotal);
                TrackTimeRemaining = TrackTimeTotal - newPos;

                if (ignoringTrackTimeChanges)
                    sendTrackTimeChangeWhenFinished = newPos;
                else
                    SendTrackTimeUpdate(newPos);
            }
        }

        protected void SendTrackTimeUpdate(int position)
        {
            ignoringTrackTimeChanges = true;
            string url = "/ctrl-int/1/setproperty?dacp.playingtime=" + position + "&session-id=" + SessionID;
            SubmitHTTPRequest(url, new HTTPResponseHandler(ProcessSendTrackTimeUpdateResponse));
        }

        protected void ProcessSendTrackTimeUpdateResponse(HTTPRequestInfo requestInfo)
        {
            if (sendTrackTimeChangeWhenFinished >= 0)
            {
                int newPos = sendTrackTimeChangeWhenFinished;
                sendTrackTimeChangeWhenFinished = -1;
                SendTrackTimeUpdate(newPos);
            }
            else
                ignoringTrackTimeChanges = false;
        }

        private readonly string timeFormat = "{0}:{1:00}";
        private readonly string timeFormatHours = "{0}:{1:00}:{2:00}";

        public string CurrentTrackTimePositionString
        {
            get
            {
                TimeSpan ts = TimeSpan.FromMilliseconds(CurrentTrackTimePosition);

                if (ts.Hours > 0)
                    return string.Format(timeFormatHours, ts.Hours, ts.Minutes, ts.Seconds);
                return string.Format(timeFormat, ts.Minutes, ts.Seconds);
            }
        }

        public string CurrentTrackTimeRemainingString
        {
            get
            {
                TimeSpan ts = TimeSpan.FromMilliseconds(CurrentTrackTimeRemaining);

                if (ts.Hours > 0)
                    return "-" + string.Format(timeFormatHours, ts.Hours, ts.Minutes, ts.Seconds);
                return "-" + string.Format(timeFormat, ts.Minutes, ts.Seconds);
            }
        }

        public string CurrentTrackTimePositionOrPausedString
        {
            get
            {
                switch (PlayState)
                {
                    case PlayStates.Playing:
                        return CurrentTrackTimePositionString;
                    case PlayStates.Paused:
                        return LocalizedDACPStrings.PlayStatusPaused;
                    case PlayStates.Stopped:
                        if (CurrentSongName != null)
                            return LocalizedDACPStrings.PlayStatusPaused;
                        goto default;
                    default:
                        return string.Empty;
                }
            }
        }

        private void SendTrackTimePropertyChanged()
        {
            PropertyChanged.RaiseOnUIThread(this,
                "TrackTimeTotal",
                "TrackTimeRemaining",
                "CurrentTrackTimeRemaining",
                "CurrentTrackTimePosition",
                "CurrentTrackTimePercentage",
                "CurrentTrackTimePositionString",
                "CurrentTrackTimeRemainingString",
                "CurrentTrackTimePositionOrPausedString");
        }

        private DispatcherTimer timerTrackTimeUpdate;
        void timerTrackTimeUpdate_Tick(object sender, EventArgs e)
        {
            SendTrackTimePropertyChanged();
        }

        #endregion

        #region Media Kind

        private int _CurrentMediaKind = 0;
        public int CurrentMediaKind
        {
            get { return _CurrentMediaKind; }
            set
            {
                if (_CurrentMediaKind == value)
                    return;
                _CurrentMediaKind = value;
                PropertyChanged.RaiseOnUIThread(this, "CurrentMediaKind", "IsCurrentlyPlayingVideo");
            }
        }

        public bool IsCurrentlyPlayingVideo
        {
            get
            {
                switch (CurrentMediaKind)
                {
                    case 2: // Movie
                    case 32: // Music video
                    case 6: // Podcast (video)
                    case 36: // Podcast (video)
                    case 64: // TV Shows
                    case 2097154: // iTunes U (video)
                        return true;
                    default:
                        return false;
                }
            }
        }

        #endregion

        #region Program Status

        private PlayStates _PlayState = PlayStates.Stopped;
        public PlayStates PlayState
        {
            get { return _PlayState; }
            protected set
            {
                if (value == 0)
                    value = PlayStates.Stopped;

                if (_PlayState == value)
                    return;
                _PlayState = value;

                PropertyChanged.RaiseOnUIThread(this, "PlayState", "PlayStateBool");
                SendTrackTimePropertyChanged();
            }
        }

        public bool PlayStateBool
        {
            get { return (PlayState == PlayStates.Playing); }
        }

        private bool _ShuffleState = false;
        public bool ShuffleState
        {
            get { return _ShuffleState; }
            protected set
            {
                if (_ShuffleState == value)
                    return;
                _ShuffleState = value;
                PropertyChanged.RaiseOnUIThread(this, "ShuffleState");
            }
        }

        private RepeatStates _RepeatState = RepeatStates.None;
        public RepeatStates RepeatState
        {
            get { return _RepeatState; }
            protected set
            {
                if (_RepeatState == value)
                    return;
                _RepeatState = value;
                PropertyChanged.RaiseOnUIThread(this, "RepeatState");
            }
        }

        private bool ignoringVolumeChanges = false;
        private int sendVolumeChangeWhenFinished = -1;

        private int _Volume = 0;
        public int Volume
        {
            get { return _Volume; }
            set
            {
                if (_Volume == value)
                    return;

                if (value > 100)
                    _Volume = 100;
                else if (value < 0)
                    _Volume = 0;
                else
                    _Volume = value;

                SendVolumePropertyChanged();

                if (ignoringVolumeChanges)
                    sendVolumeChangeWhenFinished = _Volume;
                else
                    SendVolumeUpdate(_Volume);
            }
        }

        protected void SendVolumePropertyChanged()
        {
            PropertyChanged.RaiseOnUIThread(this, "Volume");

            lock (Speakers)
            {
                foreach (AirPlaySpeaker speaker in Speakers)
                    speaker.UpdateBindableVolume();
            }
        }

        protected void SendVolumeUpdate(int newVolume)
        {
            ignoringVolumeChanges = true;
            string url = "/ctrl-int/1/setproperty?dmcp.volume=" + newVolume + "&session-id=" + SessionID;
            SubmitHTTPRequest(url, new HTTPResponseHandler(ProcessSendVolumeUpdateResponse));
        }

        protected void ProcessSendVolumeUpdateResponse(HTTPRequestInfo requestInfo)
        {
            if (sendVolumeChangeWhenFinished >= 0)
            {
                int newVolume = sendVolumeChangeWhenFinished;
                sendVolumeChangeWhenFinished = -1;
                SendVolumeUpdate(newVolume);
            }
            else
                ignoringVolumeChanges = false;
        }

        #endregion

        #region Visualizer

        private bool _visualizerAvailable;
        public bool VisualizerAvailable
        {
            get { return _visualizerAvailable; }
            protected set
            {
                if (_visualizerAvailable == value)
                    return;

                _visualizerAvailable = value;
                PropertyChanged.RaiseOnUIThread(this, "VisualizerAvailable");
            }
        }

        private bool _visualizerActive;
        public bool VisualizerActive
        {
            get { return _visualizerActive; }
            protected set
            {
                if (_visualizerActive == value)
                    return;

                _visualizerActive = value;
                PropertyChanged.RaiseOnUIThread(this, "VisualizerActive");
            }
        }

        public void SendVisualizerCommand(bool showVisualizer)
        {
            int state = (showVisualizer) ? 1 : 0;
            string url = "/ctrl-int/1/setproperty?dacp.visualizer=" + state + "&session-id=" + SessionID;
            SubmitHTTPRequest(url);
        }

        #endregion

        #region Full Screen Mode

        private bool _fullScreenModeAvailable;
        public bool FullScreenModeAvailable
        {
            get { return _fullScreenModeAvailable; }
            protected set
            {
                if (_fullScreenModeAvailable == value)
                    return;

                _fullScreenModeAvailable = value;
                PropertyChanged.RaiseOnUIThread(this, "FullScreenModeAvailable");
            }
        }

        private bool _fullScreenModeActive;
        public bool FullScreenModeActive
        {
            get { return _fullScreenModeActive; }
            protected set
            {
                if (_fullScreenModeActive == value)
                    return;

                _fullScreenModeActive = value;
                PropertyChanged.RaiseOnUIThread(this, "FullScreenModeActive");
            }
        }

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }
}
