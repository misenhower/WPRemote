﻿using System;
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

namespace Komodex.DACP
{
    public partial class DACPServer : INotifyPropertyChanged
    {
        #region Connection

        private bool _IsConnected = false;
        public bool IsConnected
        {
            get { return _IsConnected; }
            set
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

        private BitmapImage _CurrentAlbumArt = null;
        public BitmapImage CurrentAlbumArt
        {
            get { return _CurrentAlbumArt; }
            protected set
            {
                if (_CurrentAlbumArt == value)
                    return;
                _CurrentAlbumArt = value;
                SendPropertyChanged("CurrentAlbumArt");
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
                SendPropertyChanged("TrackTimeTotal");
            }
        }

        private int _TrackTimeRemaining = 0;
        public int TrackTimeRemaining
        {
            get { return _TrackTimeRemaining; }
            protected set
            {
                if (_TrackTimeRemaining == value)
                    return;

                if (value < 0)
                    value = 0;

                _TrackTimeRemaining = value;
                SendPropertyChanged("TrackTimeRemaining");
                SendPropertyChanged("CurrentTimeRemaining");
            }
        }

        private DateTime _TrackTimeUpdatedAt = DateTime.MinValue;
        public DateTime TrackTimeUpdatedAt
        {
            get { return _TrackTimeUpdatedAt; }
            protected set
            {
                if (_TrackTimeUpdatedAt == value)
                    return;
                _TrackTimeUpdatedAt = value;
                SendPropertyChanged("TrackTimeUpdatedAt");
            }
        }

        public int CurrentTrackTimeRemaining
        {
            get
            {
                double adjustedMilliseconds = (DateTime.Now - TrackTimeUpdatedAt).TotalMilliseconds;
                if (adjustedMilliseconds > int.MaxValue || adjustedMilliseconds < int.MinValue)
                    return TrackTimeRemaining;

                return TrackTimeRemaining - (int)adjustedMilliseconds;
            }
            set { } // TODO
        }

        private DispatcherTimer timerTrackTimeUpdate = new DispatcherTimer();
        void timerTrackTimeUpdate_Tick(object sender, EventArgs e)
        {
            SendPropertyChanged("CurrentTrackTimeRemaining");
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
                SendPropertyChanged("ShuffleStatus");
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
                SendPropertyChanged("RepeatStatus");
            }
        }

        private int _Volume = 0;
        public int Volume
        {
            get { return _Volume; }
            protected set
            {
                if (_Volume == value)
                    return;

                if (value > 100)
                    _Volume = 100;
                else if (value < 0)
                    _Volume = 0;
                else
                    _Volume = value;

                SendPropertyChanged("Volume");
            }
        }

        #endregion

        #region Notify Property Changed

        protected void SendPropertyChanged(string propertyName)
        {
            // TODO: Is this the best way to execute this on the UI thread?
            if (PropertyChanged != null)
                Deployment.Current.Dispatcher.BeginInvoke(() => { PropertyChanged(this, new PropertyChangedEventArgs(propertyName)); });

        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #endregion
    }
}
