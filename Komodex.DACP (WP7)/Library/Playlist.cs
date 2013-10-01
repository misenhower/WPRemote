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
using System.Linq;
using System.Collections.Generic;

namespace Komodex.DACP.Library
{
    public class Playlist : INotifyPropertyChanged
    {
        private Playlist()
        { }

        public Playlist(DACPServer server, byte[] data)
        {
            Server = server;
            DACPNodeDictionary nodes = DACPNodeDictionary.Parse(data);
            ProcessDACPNodes(nodes);
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

        private List<MediaItem> _songs = null;
        public List<MediaItem> Songs
        {
            get { return _songs; }
            protected set
            {
                if (_songs == value)
                    return;
                _songs = value;
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

        protected virtual void ProcessDACPNodes(DACPNodeDictionary nodes)
        {
            ID = nodes.GetInt("miid");
            PersistentID = (UInt64)nodes.GetLong("mper");
            Name = nodes.GetString("minm");
            BasePlaylist = nodes.GetBool("abpl");
            SpecialPlaylistType = (int)nodes.GetByte("aePS");
            ItemCount = nodes.GetInt("mimc");
            SmartPlaylist = nodes.GetBool("aeSP");
            string description = nodes.GetString("ascn");
            if (!string.IsNullOrEmpty(description))
                GeniusMixDescription = description.Replace(",,,", ", ") + " " + LocalizedDACPStrings.GeniusMixAndOthers;
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
            Songs = DACPUtility.GetItemsFromNodes(requestInfo.ResponseNodes, data => new MediaItem(Server, data)).ToList();
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
                SendPlayQueueCommand(mode, song.ContainerItemID);
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
