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
    public class Artist : LibraryElementBase
    {
        private Artist()
            : base()
        { }

        public Artist(DACPServer server, string name)
            : this()
        {
            Server = server;
            Name = name;

            GetArtistID();
        }

        public Artist(DACPServer server, byte[] data)
            : base(server, data)
        {
            if (ArtistID != 0)
                PersistArtistID();
            else
                GetArtistID();
        }

        #region Properties

        /// <summary>
        /// iTunes 11 Artist ID (asri, daap.songartistid)
        /// </summary>
        public UInt64 ArtistID { get; protected set; }

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

        public override string AlbumArtURL
        {
            get
            {
                return Server.HTTPPrefix + "/databases/" + Server.DatabaseID + "/groups/" + ID
                    + "/extra_data/artwork?mw=175&mh=175&group-type=artists&session-id=" + Server.SessionID;
            }
        }

        #endregion

        #region Methods

        protected override bool ProcessByteKVP(System.Collections.Generic.KeyValuePair<string, byte[]> kvp)
        {
            if (base.ProcessByteKVP(kvp))
                return true;

            switch (kvp.Key)
            {
                case "asri":
                    ArtistID = (UInt64)kvp.Value.GetInt64Value();
                    return true;
                default:
                    return false;
            }
        }

        #endregion

        #region ArtistID Persistence

        protected void PersistArtistID()
        {
            if (Server != null && ArtistID != 0)
                Server.ArtistIDs[Name] = ArtistID;
        }

        protected void GetArtistID()
        {
            if (Server != null && Server.ArtistIDs.ContainsKey(Name))
                ArtistID = Server.ArtistIDs[Name];
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
            string encodedName = DACPUtility.QueryEncodeString(Name);
            string url;

            if (Server.SupportsPlayQueue)
            {
                url = "/databases/" + Server.DatabaseID + "/groups"
                    + "?meta=dmap.itemname,dmap.itemid,dmap.persistentid,daap.songartist,com.apple.itunes.cloud-id,daap.songartistid,daap.songalbumid,dmap.persistentid,dmap.downloadstatus"
                    + "&type=music"
                    + "&group-type=albums"
                    + "&sort=album"
                    + "&include-sort-headers=0"
                    + "&query=('daap.songartistid:" + ArtistID + "'+'daap.songalbum!:'+('com.apple.itunes.extended-media-kind:1','com.apple.itunes.extended-media-kind:32'))"
                    + "&session-id=" + Server.SessionID;
            }
            else
            {
                url = "/databases/" + Server.DatabaseID + "/groups"
                     + "?meta=dmap.itemname,daap.songartist,dmap.itemid,dmap.persistentid,daap.songartist,daap.songdatereleased,dmap.itemcount,daap.songtime,dmap.persistentid"
                     + "&type=music"
                     + "&group-type=albums"
                     + "&sort=album"
                     + "&include-sort-headers=1"
                     + "&query=(('daap.songartist:" + encodedName + "','daap.songalbumartist:" + encodedName + "')+('com.apple.itunes.mediakind:1','com.apple.itunes.mediakind:32')+'daap.songalbum!:')"
                     + "&session-id=" + Server.SessionID;
            }
            Server.SubmitHTTPRequest(url, new HTTPResponseHandler(ProcessAlbumsResponse), true);
        }

        protected void ProcessAlbumsResponse(HTTPRequestInfo requestInfo)
        {
            foreach (var kvp in requestInfo.ResponseNodes)
            {
                switch (kvp.Key)
                {
                    case "mlcl":
                        ObservableCollection<Album> albums = new ObservableCollection<Album>();

                        var albumNodes = DACPUtility.GetResponseNodes(kvp.Value);
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
            string encodedName = DACPUtility.QueryEncodeString(Name);
            string url;

            if (Server.SupportsPlayQueue)
            {
                url = "/databases/" + Server.DatabaseID + "/containers/" + Server.BasePlaylist.ID + "/items"
                    + "?meta=dmap.itemname,dmap.itemid,daap.songartist,daap.songalbumartist,daap.songalbum,com.apple.itunes.cloud-id,dmap.containeritemid,com.apple.itunes.has-video,com.apple.itunes.itms-songid,com.apple.itunes.extended-media-kind,dmap.downloadstatus,daap.songdisabled,com.apple.itunes.cloud-id,daap.songartistid,daap.songalbumid,dmap.persistentid,dmap.downloadstatus,daap.songalbum"
                    + "&type=music"
                    + "&sort=album"
                    + "&query=('daap.songartistid:" + ArtistID + "'+('com.apple.itunes.extended-media-kind:1','com.apple.itunes.extended-media-kind:32'))"
                    + "&session-id=" + Server.SessionID;
            }
            else
            {
                url = "/databases/" + Server.DatabaseID + "/containers/" + Server.BasePlaylist.ID + "/items"
                    + "?meta=dmap.itemname,dmap.itemid,daap.songartist,daap.songalbum,dmap.containeritemid,com.apple.itunes.has-video,daap.songdatereleased,dmap.itemcount,daap.songtime,dmap.persistentid,daap.songalbum"
                    + "&type=music"
                    + "&sort=album"
                    + "&include-sort-headers=1"
                    + "&query=(('daap.songartist:" + encodedName + "','daap.songalbumartist:" + encodedName + "')+('com.apple.itunes.mediakind:1','com.apple.itunes.mediakind:32'))"
                    + "&session-id=" + Server.SessionID;
            }

            Server.SubmitHTTPRequest(url, new HTTPResponseHandler(ProcessSongsResponse), true);
        }

        protected void ProcessSongsResponse(HTTPRequestInfo requestInfo)
        {
            foreach (var kvp in requestInfo.ResponseNodes)
            {
                switch (kvp.Key)
                {
                    case "mlcl":
                        ObservableCollection<MediaItem> songs = new ObservableCollection<MediaItem>();

                        var songNodes = DACPUtility.GetResponseNodes(kvp.Value);
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

        public void SendPlayCommand()
        {
            if (Server.SupportsPlayQueue)
                SendPlayQueueCommand(PlayQueueMode.Replace);
            else
                SendPlaySongCommand(0);
        }

        public void SendPlaySongCommand(MediaItem song)
        {
            if (song == null)
                return;

            if (Server.SupportsPlayQueue)
                SendPlayQueueCommand(PlayQueueMode.Replace, song.ID);
            else
            {
                if (Songs == null)
                    return;
                if (!Songs.Contains(song))
                    return;

                int songIndex = Songs.IndexOf(song);
                SendPlaySongCommand(songIndex);
            }
        }

        protected void SendPlaySongCommand(int index)
        {
            if (Server.SupportsPlayQueue)
                return; // This should never happen
            else
                SendCueCommand("&index=" + index);
        }

        public void SendShuffleSongsCommand()
        {
            if (Server.SupportsPlayQueue)
                SendPlayQueueCommand(PlayQueueMode.Shuffle);
            else
                SendCueCommand("&dacp.shufflestate=1");
        }

        protected void SendPlayQueueCommand(PlayQueueMode mode, int? songID = null)
        {
            string query;

            if (songID.HasValue)
                query = string.Format("&query='dmap.itemid:{0}'&queuefilter=artist:{1}", songID.Value, ArtistID);
            else
                query = string.Format("&query='daap.songartistid:{0}'", ArtistID);

            string url = "/ctrl-int/1/playqueue-edit"
                + "?command=add"
                + query
                + "&sort=album"
                + "&mode=" + (int)mode
                + "&session-id=" + Server.SessionID;

            if (mode == PlayQueueMode.Replace)
                url += "&clear-previous=1";

            Server.SubmitHTTPPlayRequest(url);
        }

        protected void SendCueCommand(string input)
        {
            string encodedName = DACPUtility.QueryEncodeString(Name);
            string url = "/ctrl-int/1/cue?command=play"
                + "&query=(('daap.songartist:" + encodedName + "','daap.songalbumartist:" + encodedName + "')+('com.apple.itunes.mediakind:1','com.apple.itunes.mediakind:32'))"
                + input
                + "&sort=album"
                //+ "&srcdatabase=0xC2C1E50F13CF1F5C"
                + "&clear-first=1"
                + "&session-id=" + Server.SessionID;

            Server.SubmitHTTPPlayRequest(url);
        }

        #endregion

        #endregion

    }
}
