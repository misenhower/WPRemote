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
    public class Podcast : LibraryElementBase
    {
        private Podcast()
            : base()
        { }

        public Podcast(DACPServer server, byte[] data)
            : base(server, data)
        { }

        public Podcast(DACPServer server, int id, string name, UInt64 persistentID)
            :this()
        {
            ID = id;
            Server = server;
            Name = name;
            PersistentID = persistentID;
        }

        #region Properties

        public UInt64 PersistentID { get; protected set; }

        private ObservableCollection<MediaItem> _Episodes = null;
        public ObservableCollection<MediaItem> Episodes
        {
            get { return _Episodes; }
            protected set
            {
                if (_Episodes == value)
                    return;
                _Episodes = value;
                SendPropertyChanged("Episodes");
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
                case "mper": // Persistent ID
                    PersistentID = (UInt64)kvp.Value.GetInt64Value();
                    return true;
                default:
                    return false;
            }
        }

        #endregion

        #region HTTP Requests and Responses

        #region Episode List

        private bool retrievingEpisodes = false;

        public void GetEpisodes()
        {
            if (!retrievingEpisodes)
                SubmitEpisodesRequest();
        }

        protected void SubmitEpisodesRequest()
        {
            retrievingEpisodes = true;

            // TODO: In the iPhone Remote app, the specific playlist persistent id is sent for the container spec rather
            // than the base playlist persistent id.  (For example, the Movies special playlist ID would be sent.)
            // Sending the base playlist ID seems to work but this may require further attention in the future.

            string url = "/databases/" + Server.DatabaseID + "/containers/" + Server.BasePlaylist.ID + "/items"
                + "?meta=dmap.itemname,dmap.itemid,daap.songartist,daap.songalbum,dmap.containeritemid,com.apple.itunes.has-video,daap.songdisabled,com.apple.itunes.mediakind,daap.songtime,daap.songhasbeenplayed,daap.songdatereleased,daap.sortartist,daap.songcontentdescription,daap.songalbum"
                + "&type=music"
                + "&query=(('com.apple.itunes.mediakind:4','com.apple.itunes.mediakind:36','com.apple.itunes.mediakind:6','com.apple.itunes.mediakind:7')+'daap.songalbumid:" + PersistentID + "')"
                + "&session-id=" + Server.SessionID;
            Server.SubmitHTTPRequest(url, new HTTPResponseHandler(ProcessEpisodesResponse), true);
        }

        protected void ProcessEpisodesResponse(HTTPRequestInfo requestInfo)
        {
            foreach (var kvp in requestInfo.ResponseNodes)
            {
                switch (kvp.Key)
                {
                    case "mlcl":
                        ObservableCollection<MediaItem> episodes = new ObservableCollection<MediaItem>();

                        var episodeNodes = DACPUtility.GetResponseNodes(kvp.Value);
                        foreach (var episodeData in episodeNodes)
                            episodes.Add(new MediaItem(Server, episodeData.Value));

                        Episodes = episodes;
                        break;
                    default:
                        break;
                }
            }
        }

        #endregion

        #endregion
    }
}
