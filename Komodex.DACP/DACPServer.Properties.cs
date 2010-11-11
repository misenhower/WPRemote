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

namespace Komodex.DACP
{
    public partial class DACPServer : INotifyPropertyChanged
    {
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

        #region Program Status

        private PlayStatuses _PlayStatus = PlayStatuses.Stopped;
        public PlayStatuses PlayStatus
        {
            get { return _PlayStatus; }
            protected set
            {
                if (_PlayStatus == value)
                    return;
                _PlayStatus = value;
                SendPropertyChanged("PlayStatus");
            }
        }

        private bool _ShuffleStatus = false;
        public bool ShuffleStatus
        {
            get { return _ShuffleStatus; }
            protected set
            {
                if (_ShuffleStatus == value)
                    return;
                _ShuffleStatus = value;
                SendPropertyChanged("ShuffleStatus");
            }
        }

        private RepeatStatuses _RepeatStatus = RepeatStatuses.None;
        public RepeatStatuses RepeatStatus
        {
            get { return _RepeatStatus; }
            protected set
            {
                if (_RepeatStatus == value)
                    return;
                _RepeatStatus = value;
                SendPropertyChanged("RepeatStatus");
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
