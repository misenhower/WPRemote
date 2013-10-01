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
using Komodex.Common;
using System.Collections.Generic;
using System.Linq;

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

        public string ArtistName { get; protected set; }
        public override string SecondLine { get { return ArtistName; } }
        public UInt64 PersistentID { get; protected set; }

        private List<MediaItem> _episodes = null;
        public List<MediaItem> Episodes
        {
            get { return _episodes; }
            protected set
            {
                if (_episodes == value)
                    return;
                _episodes = value;
                SendPropertyChanged("Episodes");
            }
        }

        public override string AlbumArtURL
        {
            get
            {
                int pixels = ResolutionUtility.GetScaledPixels(75);
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
                + "?meta=dmap.itemname,dmap.itemid,daap.songartist,daap.songalbum,dmap.containeritemid,com.apple.itunes.has-video,daap.songdisabled,com.apple.itunes.mediakind,daap.songtime,daap.songhasbeenplayed,daap.songuserplaycount,daap.songdatereleased,daap.sortartist,daap.songcontentdescription,daap.songalbum"
                + "&type=music"
                + "&sort=releasedate"
                + "&invert-sort-order=1"
                + "&query=(('com.apple.itunes.mediakind:4','com.apple.itunes.mediakind:36','com.apple.itunes.mediakind:6','com.apple.itunes.mediakind:7')+'daap.songalbumid:" + PersistentID + "')"
                + "&session-id=" + Server.SessionID;
            Server.SubmitHTTPRequest(url, new HTTPResponseHandler(ProcessEpisodesResponse), true);
        }

        protected void ProcessEpisodesResponse(HTTPRequestInfo requestInfo)
        {
            Episodes = DACPUtility.GetItemsFromNodes(requestInfo.ResponseNodes, data => new MediaItem(Server, data)).ToList();
        }

        #endregion

        #endregion
    }
}
