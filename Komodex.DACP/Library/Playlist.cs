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
        public DACPServer Server { get; protected set; }
        public bool BasePlaylist { get; protected set; }
        public int SpecialPlaylistType { get; protected set; }

        public int ItemCount { get; protected set; }
        public string ItemCountString
        {
            get
            {
                if (ItemCount == 1)
                    return "1 item";
                return ItemCount + " items";
            }
        }
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
                SendPropertyChanged("Songs");
            }
        }

        #endregion

        #region Methods

        protected void ParseByteData(byte[] data)
        {
            var nodes = Utility.GetResponseNodes(data);
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
                    case "aePS":
                        SpecialPlaylistType = (int)kvp.Value[0];
                        break;
                    case "mimc": // Item count
                        ItemCount = kvp.Value.GetInt32Value();
                        break;
                    case "aeSP": // Smart playlist
                        if (kvp.Value[0] >= 1)
                            SmartPlaylist = true;
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
        }

        #endregion

        #region Play Song Command

        public void SendPlaySongCommand()
        {
            SendPlaySongCommand(0);
        }

        public void SendPlaySongCommand(MediaItem song)
        {
            SendPlaySongCommand(song.ContainerItemID);
        }

        public void SendShuffleSongsCommand()
        {
            SendPlaySongCommand("&dacp.shufflestate=1");
        }

        protected void SendPlaySongCommand(int containerItemID)
        {
            SendPlaySongCommand("&container-item-spec='dmap.containeritemid:0x" + containerItemID.ToString("x8") + "'");
        }

        protected void SendPlaySongCommand(string input)
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

        #region Notify Property Changed

        protected void SendPropertyChanged(string propertyName)
        {
            // TODO: Is this the best way to execute this on the UI thread?
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            });
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #endregion

    }
}
