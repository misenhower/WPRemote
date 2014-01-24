using Komodex.Common;
using Komodex.DACP.Databases;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
                PropertyChanged.RaiseOnUIThread(this, "CurrentAlbumPersistentID");
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
