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
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Komodex.DACP.Library
{
    public class Artist : IDACPResponseHandler, INotifyPropertyChanged
    {
        private Artist()
        { }

        public Artist(DACPServer server, string name)
        {
            Server = server;
            Name = name;
        }

        public Artist(DACPServer server, byte[] data)
        {
            Server = server;
            ParseByteData(data);
        }

        #region Properties

        public string Name { get; protected set; }
        public DACPServer Server { get; protected set; }

        private ObservableCollection<Album> _Albums = null;
        public ObservableCollection<Album> Albums
        {
            get { return _Albums; }
            protected set
            {
                if (_Albums == value)
                    return;
                _Albums = value;
                SendPropertyChanged("Albums");
            }
        }

        private ObservableCollection<Song> _Songs = null;
        public ObservableCollection<Song> Songs
        {
            get { return _Songs; }
            set
            {
                if (_Songs == value)
                    return;
                _Songs = value;
                SendPropertyChanged("Songs");
            }
        }

        #endregion

        #region Methods

        private void ParseByteData(byte[] data)
        {
            var nodes = Utility.GetResponseNodes(data);
            foreach (var kvp in nodes)
            {
                switch (kvp.Key)
                {
                    case "minm":
                        Name = kvp.Value.GetStringValue();
                        break;
                    default:
                        break;
                }
            }
        }

        #endregion

        #region IDACPResponseHandler Members

        public void ProcessResponse(HTTPRequestInfo requestInfo)
        {
            switch (requestInfo.ResponseCode)
            {
                case "agal": // Albums
                    ProcessAlbumsResponse(requestInfo);
                    break;
                case "apso": // Songs
                    ProcessSongsResponse(requestInfo);
                    break;
                default:
                    break;
            }
        }

        #endregion

        #region HTTP Requests and Responses

        #region Albums

        private bool retrievingAlbums = false;

        public void GetAlbums()
        {
            if (!retrievingAlbums)
                SubmitAlbumsRequest();
        }

        protected void SubmitAlbumsRequest()
        {
            retrievingAlbums = true;
            string url = "/databases/" + Server.DatabaseID + "/groups"
                + "?meta=dmap.itemname,dmap.itemid,dmap.persistentid,daap.songartist,daap.songdatereleased,dmap.itemcount,daap.songtime,dmap.persistentid"
                + "&type=music"
                + "&group-type=albums"
                + "&sort=album"
                + "&include-sort-headers=1"
                + "&query=(('daap.songartist:" + Name + "','daap.songalbumartist:" + Name + "')+('com.apple.itunes.mediakind:1','com.apple.itunes.mediakind:32')+'daap.songalbum!:')"
                + "&session-id=" + Server.SessionID;
            Server.SubmitHTTPRequest(url, null, this);
        }

        protected void ProcessAlbumsResponse(HTTPRequestInfo requestInfo)
        {
            foreach (var kvp in requestInfo.ResponseNodes)
            {
                switch (kvp.Key)
                {
                    case "mlcl":
                        ObservableCollection<Album> albums = new ObservableCollection<Album>();

                        var albumNodes = Utility.GetResponseNodes(kvp.Value);
                        foreach (var albumData in albumNodes)
                        {
                            albums.Add(new Album(Server, albumData.Value));
                        }

                        Albums = albums;
                        break;
                    default:
                        break;
                }
            }

            retrievingAlbums = false;
        }

        #endregion

        #region Songs

        private bool retrievingSongs = false;

        public void GetSongs()
        {
            if (!retrievingSongs)
                SubmitSongsRequest();
        }

        protected void SubmitSongsRequest()
        {
            retrievingSongs = true;
            string url = "/databases/" + Server.DatabaseID + "/containers/" + Server.BasePlaylistID + "/items"
                + "?meta=dmap.itemname,dmap.itemid,daap.songartist,daap.songalbum,dmap.containeritemid,com.apple.itunes.has-video,daap.songdatereleased,dmap.itemcount,daap.songtime,dmap.persistentid,daap.songalbum"
                + "&type=music"
                + "&sort=name"
                + "&include-sort-headers=1"
                + "&query=(('daap.songartist:" + Name + "','daap.songalbumartist:" + Name + "')+('com.apple.itunes.mediakind:1','com.apple.itunes.mediakind:32'))"
                + "&session-id=" + Server.SessionID;
            Server.SubmitHTTPRequest(url, null, this);
        }

        protected void ProcessSongsResponse(HTTPRequestInfo requestInfo)
        {
            foreach (var kvp in requestInfo.ResponseNodes)
            {
                switch (kvp.Key)
                {
                    case "mlcl":
                        ObservableCollection<Song> songs = new ObservableCollection<Song>();

                        var songNodes = Utility.GetResponseNodes(kvp.Value);
                        foreach (var songData in songNodes)
                        {
                            songs.Add(new Song(songData.Value));
                        }

                        Songs = songs;
                        break;
                    default:
                        break;
                }
            }
            retrievingSongs = false;
        }

        #endregion

        #region Play Song Command

        public void SendPlaySongCommand(Song song)
        {
            try
            {
                int songIndex = Songs.IndexOf(song);

                string url = "/ctrl-int/1/cue?command=play"
                    + "&query=(('daap.songartist:" + Name + "','daap.songalbumartist:" + Name + "')+('com.apple.itunes.mediakind:1','com.apple.itunes.mediakind:32'))"
                    + "&index=" + songIndex
                    + "&sort=name"
                    //+ "&srcdatabase=0xC2C1E50F13CF1F5C"
                    +"&clear-first=1"
                    + "&session-id=" + Server.SessionID;

                Server.SubmitHTTPRequest(url);
            }
            catch { }
        }

        #endregion

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
