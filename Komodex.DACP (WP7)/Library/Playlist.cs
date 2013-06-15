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
using Komodex.DACP.Localization;
using Komodex.Common;

namespace Komodex.DACP.Library
{
    public class Playlist : INotifyPropertyChanged
    {
        private Playlist()
        { }

        public Playlist(DACPServer server, byte[] data)
        {
            Server = server;
            ParseByteData(data);
        }

        public Playlist(DACPServer server, int id, string name, UInt64 persistentID)
        {
            Server = server;
            ID = id;
            Name = name;
            PersistentID = persistentID;
        }

        #region Properties

        public int ID { get; protected set; }
        public UInt64 PersistentID { get; protected set; }
        public string Name { get; protected set; }
        public string GeniusMixDescription { get; protected set; }
        public DACPServer Server { get; protected set; }
        public bool BasePlaylist { get; protected set; }
        public int SpecialPlaylistType { get; protected set; }

        public int ItemCount { get; protected set; }
        public string ItemCountString
        {
            get
            {
                if (ItemCount == 1)
                    return LocalizedDACPStrings.OneItem;
                return string.Format(LocalizedDACPStrings.MultipleItems, ItemCount);
            }
        }
        public string SecondLine { get { return ItemCountString; } }
        public bool SmartPlaylist { get; protected set; }

        private ObservableCollection<MediaItem> _Songs = null;
        public ObservableCollection<MediaItem> Songs
        {
            get { return _Songs; }
            protected set
            {
                if (_Songs == value)
                    return;
                _Songs = value;
                PropertyChanged.RaiseOnUIThread(this, "Songs");
            }
        }

        public string AlbumArtURL
        {
            get
            {
                int pixels = ResolutionUtility.GetScaledPixels(350);
                return Server.HTTPPrefix + "/databases/" + Server.DatabaseID + "/containers/" + ID
                    + "/extra_data/artwork?mw=" + pixels + "&mh=" + pixels + "&session-id=" + Server.SessionID;
            }
        }

        #endregion

        #region Methods

        protected void ParseByteData(byte[] data)
        {
            var nodes = DACPUtility.GetResponseNodes(data);
            foreach (var kvp in nodes)
            {
                switch (kvp.Key)
                {
                    case "miid": // ID
                        ID = kvp.Value.GetInt32Value();
                        break;
                    case "mper": // Persistent ID
                        PersistentID = (UInt64)kvp.Value.GetInt64Value();
                        break;
                    case "minm": // Name
                        Name = kvp.Value.GetStringValue();
                        break;
                    case "abpl": // Base playlist
                        if (kvp.Value[0] >= 1)
                            BasePlaylist = true;
                        break;
                    case "aePS": // Playlist type ID
                        SpecialPlaylistType = (int)kvp.Value[0];
                        break;
                    case "mimc": // Item count
                        ItemCount = kvp.Value.GetInt32Value();
                        break;
                    case "aeSP": // Smart playlist
                        if (kvp.Value[0] >= 1)
                            SmartPlaylist = true;
                        break;
                    case "ascn": // Genius Mix description
                        string description = kvp.Value.GetStringValue();
                        if (string.IsNullOrEmpty(description))
                            break;
                        GeniusMixDescription = description.Replace(",,,", ", ") + " " + LocalizedDACPStrings.GeniusMixAndOthers;
                        break;
                    default:
                        break;
                }
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

        protected void SubmitSongsRequest()
        {
            retrievingSongs = true;
            string url = "/databases/" + Server.DatabaseID + "/containers/" + ID + "/items"
                + "?meta=dmap.itemname,dmap.itemid,daap.songartist,daap.songalbum,dmap.containeritemid,com.apple.itunes.has-video"
                + "&type=music"
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
                SendPlayQueueCommand(PlayQueueMode.Replace, song.ContainerItemID);
            else
                SendPlaySongCommand(song.ContainerItemID);
        }

        public void SendShuffleSongsCommand()
        {
            if (Server.SupportsPlayQueue)
                SendPlayQueueCommand(PlayQueueMode.Shuffle);
            else
                SendCueCommand("&dacp.shufflestate=1");
        }

        protected void SendPlaySongCommand(int containerItemID)
        {
            if (Server.SupportsPlayQueue)
                return; // This should never happen
            else
                SendCueCommand("&container-item-spec='dmap.containeritemid:0x" + containerItemID.ToString("x8") + "'");
        }

        protected void SendPlayQueueCommand(PlayQueueMode mode, int? songID = null)
        {
            string query;

            if (songID.HasValue)
                query = string.Format("&query='dmap.containeritemid:{0}'&queuefilter=playlist:{1}&sort=physical", songID.Value, ID);
            else
                query = string.Format("&query='dmap.itemid:{0}'&query-modifier=containers", ID);

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
            string url = "/ctrl-int/1/playspec"
                + "?database-spec='dmap.persistentid:0x" + Server.DatabasePersistentID.ToString("x16") + "'"
                + "&container-spec='dmap.persistentid:0x" + PersistentID.ToString("x16") + "'"
                + input
                + "&session-id=" + Server.SessionID;

            Server.SubmitHTTPPlayRequest(url);
        }

        #endregion

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

    }
}
