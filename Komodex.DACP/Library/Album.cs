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
    public class Album : IDACPResponseHandler,INotifyPropertyChanged
    {
        private Album()
        { }

        public Album(DACPServer server, int id, string name, string artistName, UInt64 persistentID)
        {
            ID = id;
            Server = server;
            Name = name;
            ArtistName = artistName;
            PersistentID = persistentID;
        }

        public Album(DACPServer server, byte[] data)
        {
            Server = server;
            ParseByteData(data);
        }

        #region Properties

        public int ID { get; protected set; }
        public string Name { get; protected set; }
        private string _ArtistName = null;
        public string ArtistName
        {
            get { return _ArtistName; }
            protected set
            {
                _ArtistName = value;
                EncodedArtistName = Uri.EscapeDataString(Utility.EscapeSingleQuotes(ArtistName));
            }
        }
        public string EncodedArtistName { get; protected set; }
        public UInt64 PersistentID { get; protected set; }
        public DACPServer Server { get; protected set; }

        private ObservableCollection<Song> _Songs = null;
        public ObservableCollection<Song> Songs
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

        private void ParseByteData(byte[] data)
        {
            var nodes = Utility.GetResponseNodes(data);
            foreach (var kvp in nodes)
            {
                switch (kvp.Key)
                {
                    case "miid": // ID
                        ID = kvp.Value.GetInt32Value();
                        break;
                    case "minm": // Name
                        Name = kvp.Value.GetStringValue();
                        break;
                    case "asaa": // Artist name
                        ArtistName = kvp.Value.GetStringValue();
                        break;
                    case "mper": // Persistent ID
                        PersistentID = (UInt64)kvp.Value.GetInt64Value();
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
                case "apso": // Songs
                    ProcessSongsResponse(requestInfo);
                    break;
                default:
                    break;
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
            string url = "/databases/" + Server.DatabaseID + "/containers/" + Server.BasePlaylistID + "/items"
                + "?meta=dmap.itemname,dmap.itemid,daap.songartist,daap.songalbum,dmap.containeritemid,com.apple.itunes.has-video,daap.songdatereleased,dmap.itemcount,daap.songtime,dmap.persistentid,daap.songalbum"
                + "&type=music"
                + "&sort=album"
                + "&query=(('daap.songartist:" + EncodedArtistName + "','daap.songalbumartist:" + EncodedArtistName + "')+('com.apple.itunes.mediakind:1','com.apple.itunes.mediakind:32')+'daap.songalbumid:" + PersistentID + "')"
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

                string url = "/ctrl-int/1/cue"
                    + "?command=play"
                    + "&query=(('daap.songartist:" + EncodedArtistName + "','daap.songalbumartist:" + EncodedArtistName + "')+('com.apple.itunes.mediakind:1','com.apple.itunes.mediakind:32')+'daap.songalbumid:" + PersistentID + "')"
                    + "&index=" + songIndex
                    + "&sort=album"
                    //+ "&srcdatabase=0xC2C1E50F13CF1F5C"
                    + "&clear-first=1"
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
