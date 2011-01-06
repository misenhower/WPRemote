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
                SendPropertyChanged("IsConnected");
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
                SendPropertyChanged("LibraryName");
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
                SendPropertyChanged("ServerVersion");
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
                SendPropertyChanged("ServerDMAPVersion");
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
                SendPropertyChanged("ServerDAAPVersion");
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
                SendPropertyChanged("GettingData");
            }
        }

        private void UpdateGettingData(bool setToTrue = false)
        {
            if (setToTrue)
                GettingData = true;
            else
                GettingData = PendingHttpRequests.Any(hri => hri.IsDataRetrieval);
        }

        #endregion

        #region Current Song

        private string _CurrentSongName = null;
        public string CurrentSongName
        {
            get { return _CurrentSongName; }
            protected set
            {
                if (_CurrentSongName == value)
                    return;
                _CurrentSongName = value;
                SendPropertyChanged("CurrentSongName");
            }
        }

        private string _CurrentArtist = null;
        public string CurrentArtist {
            get { return _CurrentArtist; }
            protected set {
                if (_CurrentArtist == value)
                    return;
                _CurrentArtist = value;
                SendPropertyChanged("CurrentArtist");
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
                SendPropertyChanged("CurrentAlbum");
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
                SendPropertyChanged("CurrentAlbumArtURL");
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
                SendTrackTimeUpdate(newPos);
            }
        }

        protected void SendTrackTimeUpdate(int position)
        {
            string url = "/ctrl-int/1/setproperty?dacp.playingtime=" + position + "&session-id=" + SessionID;
            SubmitHTTPRequest(url);
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
                        return "paused";
                    case PlayStates.Stopped:
                        if (CurrentSongName != null)
                            return "paused";
                        goto default;
                    default:
                        return string.Empty;
                }
            }
        }

        private void SendTrackTimePropertyChanged()
        {
            SendPropertyChanged("TrackTimeTotal");
            SendPropertyChanged("TrackTimeRemaining");
            SendPropertyChanged("CurrentTrackTimeRemaining");
            SendPropertyChanged("CurrentTrackTimePosition");
            SendPropertyChanged("CurrentTrackTimePercentage");
            SendPropertyChanged("CurrentTrackTimePositionString");
            SendPropertyChanged("CurrentTrackTimeRemainingString");
            SendPropertyChanged("CurrentTrackTimePositionOrPausedString");
        }

        private DispatcherTimer timerTrackTimeUpdate = new DispatcherTimer();
        void timerTrackTimeUpdate_Tick(object sender, EventArgs e)
        {
            SendTrackTimePropertyChanged();
        }

        #endregion

        #region Program Status

        private PlayStates _PlayState = PlayStates.Stopped;
        public PlayStates PlayState
        {
            get { return _PlayState; }
            protected set
            {
                if (_PlayState == value)
                    return;
                _PlayState = value;
                SendPropertyChanged("PlayState");
                SendPropertyChanged("PlayStateBool");
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
                SendPropertyChanged("ShuffleState");
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
                SendPropertyChanged("RepeatState");
            }
        }

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
                SendVolumeUpdate();
            }
        }

        protected void SendVolumePropertyChanged()
        {
            SendPropertyChanged("Volume");

            if (Speakers == null)
                return;

            foreach (AirPlaySpeaker speaker in Speakers)
                speaker.SendAdjustedVolumePropertyChanged();
        }

        protected void SendVolumeUpdate()
        {
            string url = "/ctrl-int/1/setproperty?dmcp.volume=" + _Volume + "&session-id=" + SessionID;
            SubmitHTTPRequest(url);
        }

        #endregion

        #region Notify Property Changed

        protected void SendPropertyChanged(string propertyName)
        {
            // TODO: Is this the best way to execute this on the UI thread?
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            });

        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #endregion
    }
}
