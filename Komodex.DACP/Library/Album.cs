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

        private ObservableCollection<MediaItem> _Songs = null;
        public ObservableCollection<MediaItem> Songs
        {
            get { return _Songs; }
            protected set
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
                    + "/extra_data/artwork?mw=175&mh=175&group-type=albums&session-id=" + Server.SessionID;
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
                case "asaa": // Artist name
                    ArtistName = kvp.Value.GetStringValue();
                    return true;
                case "mper": // Persistent ID
                    PersistentID = (UInt64)kvp.Value.GetInt64Value();
                    return true;
                default:
                    return false;
            }
        }

        #endregion

        #region Album ID Persistence

        protected Artist _artist = null;

        protected void PersistAlbumID()
        {
            if (ID != 0)
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
                _artist.PropertyChanged += new PropertyChangedEventHandler(artist_PropertyChanged);
                _artist.GetAlbums();
            }
        }

        void artist_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Albums")
            {
                GetAlbumID(false);
                _artist.PropertyChanged -= new PropertyChangedEventHandler(artist_PropertyChanged);
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
            string encodedArtistName = Utility.QueryEncodeString(ArtistName);
            string url = "/databases/" + Server.DatabaseID + "/containers/" + Server.BasePlaylist.ID + "/items"
                + "?meta=dmap.itemname,dmap.itemid,daap.songartist,daap.songalbum,dmap.containeritemid,com.apple.itunes.has-video,daap.songdatereleased,dmap.itemcount,daap.songtime,dmap.persistentid,daap.songalbum"
                + "&type=music"
                + "&sort=album"
                + "&query=(('daap.songartist:" + encodedArtistName + "','daap.songalbumartist:" + encodedArtistName + "')+('com.apple.itunes.mediakind:1','com.apple.itunes.mediakind:32')+'daap.songalbumid:" + PersistentID + "')"
                + "&session-id=" + Server.SessionID;

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
            SendPlaySongCommand("&index=" + index);
        }

        public void SendShuffleSongsCommand()
        {
            SendPlaySongCommand("&dacp.shufflestate=1");
        }

        protected void SendPlaySongCommand(string input)
        {
            string encodedArtistName = Utility.QueryEncodeString(ArtistName);
            string url = "/ctrl-int/1/cue"
                + "?command=play"
                + "&query=(('daap.songartist:" + encodedArtistName + "','daap.songalbumartist:" + encodedArtistName + "')+('com.apple.itunes.mediakind:1','com.apple.itunes.mediakind:32')+'daap.songalbumid:" + PersistentID + "')"
                + input
                + "&sort=album"
                //+ "&srcdatabase=0xC2C1E50F13CF1F5C"
                + "&clear-first=1"
                + "&session-id=" + Server.SessionID;

            Server.SubmitHTTPRequest(url);
        }

        #endregion

        #endregion

    }
}
