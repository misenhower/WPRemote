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
    public class Artist : LibraryGroupElementBase
    {
        private Artist()
            : base()
        { }

        public Artist(DACPServer server, string name)
            : this()
        {
            Server = server;
            Name = name;
        }

        public Artist(DACPServer server, byte[] data)
            : this()
        {
            Server = server;
            ParseByteData(data);
        }

        #region Properties

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

        private ObservableCollection<MediaItem> _Songs = null;
        public ObservableCollection<MediaItem> Songs
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
            string encodedName = Utility.QueryEncodeString(Name);
            string url = "/databases/" + Server.DatabaseID + "/groups"
                + "?meta=dmap.itemname,daap.songartist,dmap.itemid,dmap.persistentid,daap.songartist,daap.songdatereleased,dmap.itemcount,daap.songtime,dmap.persistentid"
                + "&type=music"
                + "&group-type=albums"
                + "&sort=album"
                + "&include-sort-headers=1"
                + "&query=(('daap.songartist:" + encodedName + "','daap.songalbumartist:" + encodedName + "')+('com.apple.itunes.mediakind:1','com.apple.itunes.mediakind:32')+'daap.songalbum!:')"
                + "&session-id=" + Server.SessionID;
            Server.SubmitHTTPRequest(url, new HTTPResponseHandler(ProcessAlbumsResponse));
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
            string encodedName = Utility.QueryEncodeString(Name);
            string url = "/databases/" + Server.DatabaseID + "/containers/" + Server.BasePlaylist.ID + "/items"
                + "?meta=dmap.itemname,dmap.itemid,daap.songartist,daap.songalbum,dmap.containeritemid,com.apple.itunes.has-video,daap.songdatereleased,dmap.itemcount,daap.songtime,dmap.persistentid,daap.songalbum"
                + "&type=music"
                + "&sort=name"
                + "&include-sort-headers=1"
                + "&query=(('daap.songartist:" + encodedName + "','daap.songalbumartist:" + encodedName + "')+('com.apple.itunes.mediakind:1','com.apple.itunes.mediakind:32'))"
                + "&session-id=" + Server.SessionID;
            Server.SubmitHTTPRequest(url, new HTTPResponseHandler(ProcessSongsResponse));
        }

        protected void ProcessSongsResponse(HTTPRequestInfo requestInfo)
        {
            foreach (var kvp in requestInfo.ResponseNodes)
            {
                switch (kvp.Key)
                {
                    case "mlcl":
                        ObservableCollection<MediaItem> songs = new ObservableCollection<MediaItem>();

                        var songNodes = Utility.GetResponseNodes(kvp.Value);
                        foreach (var songData in songNodes)
                        {
                            songs.Add(new MediaItem(Server, songData.Value));
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

        public void SendPlaySongCommand()
        {
            SendPlaySongCommand(0);
        }

        public void SendPlaySongCommand(MediaItem song)
        {
            try
            {
                int songIndex = Songs.IndexOf(song);
                SendPlaySongCommand(songIndex);
            }
            catch { }
        }

        protected void SendPlaySongCommand(int index)
        {
            string encodedName = Utility.QueryEncodeString(Name);
            string url = "/ctrl-int/1/cue?command=play"
                + "&query=(('daap.songartist:" + encodedName + "','daap.songalbumartist:" + encodedName + "')+('com.apple.itunes.mediakind:1','com.apple.itunes.mediakind:32'))"
                + "&index=" + index
                + "&sort=name"
                //+ "&srcdatabase=0xC2C1E50F13CF1F5C"
                + "&clear-first=1"
                + "&session-id=" + Server.SessionID;

            Server.SubmitHTTPRequest(url);
        }

        #endregion

        #endregion

    }
}
