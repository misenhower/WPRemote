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
using System.Collections.ObjectModel;
using Komodex.Common;
using System.Collections.Generic;

namespace Komodex.DACP.Library
{
    public class Album : LibraryElementBase
    {
        private Album()
            : base()
        { }

        public Album(DACPServer server, int id, string name, string artistName, UInt64 persistentID)
            : this()
        {
            ID = id;
            Server = server;
            Name = name;
            ArtistName = artistName;
            PersistentID = persistentID;

            if (id == 0)
                GetAlbumID(true);
            else
                PersistAlbumID();
        }

        public Album(DACPServer server, byte[] data)
            : base(server, data)
        {
            PersistAlbumID();
        }

        #region Properties

        private string _ArtistName = null;
        public string ArtistName
        {
            get { return _ArtistName ?? string.Empty; }
            protected set { _ArtistName = value; }
        }

        public override string SecondLine
        {
            get { return ArtistName; }
        }

        public UInt64 PersistentID { get; protected set; }

        private List<MediaItem> _songs = null;
        public List<MediaItem> Songs
        {
            get { return _songs; }
            protected set
            {
                if (_songs == value)
                    return;
                _songs = value;
                SendPropertyChanged("Songs");
            }
        }

        public override string AlbumArtURL
        {
            get
            {
                int pixels = ResolutionUtility.GetScaledPixels(175);
                return Server.HTTPPrefix + "/databases/" + Server.DatabaseID + "/groups/" + ID
                    + "/extra_data/artwork?mw=" + pixels + "&mh=" + pixels + "&group-type=albums&session-id=" + Server.SessionID;
            }
        }

        #endregion

        #region Methods

        protected override void ProcessDACPNodes(DACPNodeDictionary nodes)
        {
            base.ProcessDACPNodes(nodes);

            ArtistName = nodes.GetString("asaa");
            PersistentID = (UInt64)nodes.GetLong("mper");
        }

        #endregion

        #region Album ID Persistence

        protected Artist _artist = null;

        protected void PersistAlbumID()
        {
            if (Server != null && ID != 0)
                Server.AlbumIDs[PersistentID] = ID;
        }

        protected void GetAlbumID(bool submitHttpRequestIfNeeded = false)
        {
            if (Server.AlbumIDs.ContainsKey(PersistentID))
            {
                ID = Server.AlbumIDs[PersistentID];
                SendPropertyChanged("AlbumArtURL");
            }
            else if (submitHttpRequestIfNeeded)
            {
                // To get the DB ID of this album, create an Artist object for this album's ArtistName and get that artist's albums.
                // It's not the most efficient way possible, but it works and reduces the amount of extra code needed for a special case.
                // Also this may end up being more reasonable if object caching is implemented.
                _artist = new Artist(Server, ArtistName);
                _artist.PropertyChanged += artist_PropertyChanged;
                _artist.GetAlbums();
            }
        }

        void artist_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Albums")
            {
                GetAlbumID(false);
                _artist.PropertyChanged -= artist_PropertyChanged;
            }
        }

        #endregion

        #region HTTP Requests and Responses

        #region Song List

        private bool retrievingSongs = false;

        public void GetSongs()
        {
            if (!retrievingSongs)
                SubmitSongsRequest();
        }

        private void SubmitSongsRequest()
        {
            retrievingSongs = true;
            string url;

            if (Server.SupportsPlayQueue)
            {
                url = "/databases/" + Server.DatabaseID + "/containers/" + Server.BasePlaylist.ID + "/items"
                    + "?meta=dmap.itemname,dmap.itemid,daap.songartist,daap.songalbumartist,daap.songalbum,com.apple.itunes.cloud-id,dmap.containeritemid,com.apple.itunes.has-video,com.apple.itunes.itms-songid,com.apple.itunes.mediakind,dmap.downloadstatus,daap.songdisabled,com.apple.itunes.cloud-id,daap.songartistid,daap.songalbumid,dmap.persistentid,dmap.downloadstatus,daap.songalbum"
                    + "&type=music"
                    + "&sort=album"
                    + "&query=(('com.apple.itunes.mediakind:1','com.apple.itunes.mediakind:32')+'daap.songalbumid:" + PersistentID + "')"
                    + "&session-id=" + Server.SessionID;
            }
            else
            {
                string encodedArtistName = DACPUtility.QueryEncodeString(ArtistName);
                url = "/databases/" + Server.DatabaseID + "/containers/" + Server.BasePlaylist.ID + "/items"
                    + "?meta=dmap.itemname,dmap.itemid,daap.songartist,daap.songalbum,dmap.containeritemid,com.apple.itunes.has-video,daap.songdatereleased,dmap.itemcount,daap.songtime,dmap.persistentid,daap.songalbum"
                    + "&type=music"
                    + "&sort=album"
                    + "&query=(('daap.songartist:" + encodedArtistName + "','daap.songalbumartist:" + encodedArtistName + "')+('com.apple.itunes.mediakind:1','com.apple.itunes.mediakind:32')+'daap.songalbumid:" + PersistentID + "')"
                    + "&session-id=" + Server.SessionID;

            }

            Server.SubmitHTTPRequest(url, new HTTPResponseHandler(ProcessSongsResponse), true);
        }

        protected void ProcessSongsResponse(HTTPRequestInfo requestInfo)
        {
            Songs = DACPUtility.GetListFromNodes(requestInfo.ResponseNodes, b => new MediaItem(Server, b));

            retrievingSongs = false;
        }

        #endregion

        #region Play Song Command

        public void SendPlayCommand(PlayQueueMode mode = PlayQueueMode.Replace)
        {
            if (Server.SupportsPlayQueue)
                SendPlayQueueCommand(mode);
            else
                SendPlaySongCommand(0);
        }

        public void SendPlaySongCommand(MediaItem song, PlayQueueMode mode = PlayQueueMode.Replace)
        {
            if (song == null)
                return;

            if (Server.SupportsPlayQueue)
                SendPlayQueueCommand(mode, song.ID);
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
                query = string.Format("&query='dmap.itemid:{0}'&queuefilter=album:{1}", songID.Value, PersistentID);
            else
                query = string.Format("&query='daap.songalbumid:{0}'", PersistentID);

            string url = "/ctrl-int/1/playqueue-edit"
                    + "?command=add"
                    + query
                    + "&mode=" + (int)mode
                    + "&session-id=" + Server.SessionID;

            if (mode == PlayQueueMode.Replace)
                url += "&clear-previous=1";

            Server.SubmitHTTPPlayRequest(url);
        }

        protected void SendCueCommand(string input)
        {
            string encodedArtistName = DACPUtility.QueryEncodeString(ArtistName);
            string url = "/ctrl-int/1/cue"
                + "?command=play"
                + "&query=(('daap.songartist:" + encodedArtistName + "','daap.songalbumartist:" + encodedArtistName + "')+('com.apple.itunes.mediakind:1','com.apple.itunes.mediakind:32')+'daap.songalbumid:" + PersistentID + "')"
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
