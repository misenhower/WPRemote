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

namespace Komodex.DACP.Library
{
    public class Genre : LibraryElementBase
    {
        public Genre(DACPServer server, string name)
            : base()
        {
            Server = server;
            Name = name;
        }

        public Genre(DACPServer server, byte[] data)
            : base() // Genres are a bit different so don't call the base(server, data) constructor
        {
            Server = server;
            Name = data.GetStringValue();
        }

        #region Properties

        private GroupedItems<Artist> _Artists = null;
        public GroupedItems<Artist> Artists
        {
            get { return _Artists; }
            protected set
            {
                if (_Artists == value)
                    return;
                _Artists = value;
                SendPropertyChanged("Artists");
            }
        }

        private GroupedItems<Album> _Albums = null;
        public GroupedItems<Album> Albums
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

        private GroupedItems<MediaItem> _Songs = null;
        public GroupedItems<MediaItem> Songs
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

        #region HTTP Requests and Responses

        #region Artists

        private bool retrievingArtists = false;

        public void GetArtists()
        {
            if (!retrievingArtists)
                SubmitArtistsRequest();
        }

        protected void SubmitArtistsRequest()
        {
            retrievingArtists = true;
            string encodedName = DACPUtility.QueryEncodeString(Name);
            string url = "/databases/" + Server.DatabaseID + "/groups"
                + "?meta=dmap.itemname,dmap.itemid,dmap.persistentid,daap.songartist,daap.groupalbumcount,daap.songartistid"
                + "&type=music"
                + "&group-type=artists"
                + "&sort=album"
                + "&include-sort-headers=1"
                + "&query=('daap.songartist!:'+('com.apple.itunes.mediakind:1','com.apple.itunes.mediakind:32')+'daap.songgenre:" + encodedName + "')"
                + "&session-id=" + Server.SessionID;
            Server.SubmitHTTPRequest(url, ProcessArtistsResponse, true);
        }

        protected void ProcessArtistsResponse(HTTPRequestInfo requestInfo)
        {
            Artists = GroupedItems<Artist>.HandleResponseNodes(requestInfo.ResponseNodes, bytes => new Artist(this.Server, bytes));

            retrievingArtists = false;
        }

        #endregion

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
            string url = "/databases/" + Server.DatabaseID + "/groups"
                + "?meta=dmap.itemname,dmap.itemid,dmap.persistentid,daap.songartist,daap.songdatereleased,dmap.itemcount,daap.songtime,dmap.persistentid"
                + "&type=music"
                + "&group-type=albums"
                + "&sort=album"
                + "&include-sort-headers=1"
                + "&query=('daap.songalbum!:'+('com.apple.itunes.mediakind:1','com.apple.itunes.mediakind:32')+'daap.songgenre:" + encodedName + "')"
                + "&session-id=" + Server.SessionID;
            Server.SubmitHTTPRequest(url, ProcessAlbumsResponse, true);
        }

        protected void ProcessAlbumsResponse(HTTPRequestInfo requestInfo)
        {
            Albums = GroupedItems<Album>.HandleResponseNodes(requestInfo.ResponseNodes, bytes => new Album(this.Server, bytes));

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
            string url = "/databases/" + Server.DatabaseID + "/containers/" + Server.BasePlaylist.ID + "/items"
                + "?meta=dmap.itemname,dmap.itemid,daap.songartist,daap.songalbum,dmap.containeritemid,com.apple.itunes.has-video,daap.songdisabled,com.apple.itunes.mediakind,daap.songdatereleased,dmap.itemcount,daap.songtime,dmap.persistentid,daap.songalbum"
                + "&type=music"
                + "&sort=name"
                + "&include-sort-headers=1"
                + "&query=(('com.apple.itunes.mediakind:1','com.apple.itunes.mediakind:32')+'daap.songgenre:" + encodedName + "')"
                + "&session-id=" + Server.SessionID;
            Server.SubmitHTTPRequest(url, ProcessSongsResponse, true);
        }

        protected void ProcessSongsResponse(HTTPRequestInfo requestInfo)
        {
            int index = 0;
            Songs = GroupedItems<MediaItem>.HandleResponseNodes(requestInfo.ResponseNodes, bytes => new MediaItem(this.Server, bytes) { ListIndex = index++ });

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
                SendPlaySongCommand(song.ListIndex);
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
            string encodedName = DACPUtility.QueryEncodeString(Name);

            if (songID.HasValue)
                query = string.Format("&query='dmap.itemid:{0}'&queuefilter=genre:{1}", songID.Value, encodedName);
            else
                query = string.Format("&query='daap.songgenre:{0}'", encodedName);

            string url = "/ctrl-int/1/playqueue-edit"
                + "?command=add"
                + query
                + "&sort=name"
                + "&mode=" + (int)mode
                + "&session-id=" + Server.SessionID;

            if (mode == PlayQueueMode.Replace)
                url += "&clear-previous=1";

            Server.SubmitHTTPPlayRequest(url);
        }

        protected void SendCueCommand(string input)
        {
            string encodedName = DACPUtility.QueryEncodeString(Name);
            string url = "/ctrl-int/1/cue"
                + "?command=play"
                + "&query=(('com.apple.itunes.mediakind:1','com.apple.itunes.mediakind:32')+'daap.songgenre:" + encodedName + "')"
                + input
                + "&sort=name"
                //+ "&srcdatabase=0xD18B9763F4D90887"
                + "&clear-first=1"
                + "&session-id=" + Server.SessionID;

            Server.SubmitHTTPPlayRequest(url);
        }

        #endregion

        #endregion

    }
}
