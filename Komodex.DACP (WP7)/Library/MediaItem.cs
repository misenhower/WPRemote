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
using Komodex.Common;

namespace Komodex.DACP.Library
{
    public class MediaItem : LibraryElementBase
    {
        protected MediaItem()
        { }

        //public Song(DACPServer server, string name)
        //{
        //    Server = server;
        //    Name = name;
        //}

        public MediaItem(DACPServer server, byte[] data)
            : base(server, data)
        { }

        #region Properties

        public int ContainerItemID { get; protected set; }

        public int UserRating { get; protected set; }

        private string _ArtistName = null;
        public string ArtistName
        {
            get { return _ArtistName ?? string.Empty; }
            protected set { _ArtistName = value; }
        }

        private string _AlbumName = null;
        public string AlbumName
        {
            get { return _AlbumName ?? string.Empty; }
            protected set { _AlbumName = value; }
        }

        public string ArtistAndAlbum
        {
            get
            {
                bool hasArtist = !string.IsNullOrEmpty(ArtistName);
                bool hasAlbum = !string.IsNullOrEmpty(AlbumName);

                if (hasArtist && hasAlbum)
                    return ArtistName + " – " + AlbumName;
                if (hasArtist)
                    return ArtistName;
                if (hasAlbum)
                    return AlbumName;
                return string.Empty;
            }
        }

        public override string SecondLine
        {
            get { return ArtistName; }
        }

        public override string AlbumArtURL
        {
            get
            {
                int pixels = ResolutionUtility.GetScaledPixels(175);
                return Server.HTTPPrefix + "/databases/" + Server.DatabaseID + "/items/" + ID
                    + "/extra_data/artwork?mw=" + pixels + "&mh=" + pixels + "&session-id=" + Server.SessionID;
            }
        }

        public bool IsDisabled { get; protected set; }
        public bool HasBeenPlayed { get; protected set; }
        public int PlayCount { get; protected set; }
        public DateTime? DateReleased { get; protected set; }

        #endregion

        #region Methods

        protected override bool ProcessByteKVP(System.Collections.Generic.KeyValuePair<string, byte[]> kvp)
        {
            if (base.ProcessByteKVP(kvp))
                return true;

            switch (kvp.Key)
            {
                case "mcti": // Container item ID
                    ContainerItemID = kvp.Value.GetInt32Value();
                    return true;
                case "asar": // Artist name
                    ArtistName = kvp.Value.GetStringValue();
                    return true;
                case "asal": // Album name
                    AlbumName = kvp.Value.GetStringValue();
                    return true;
                case "asur": // User rating
                    UserRating = kvp.Value[0];
                    return true;
                case "asdb": // Disabled
                    IsDisabled = !(kvp.Value[0] == 0);
                    return true;

                // The following were added for podcast episodes, may need to move them if episodes become their own class
                case "ashp": // Has been played
                    HasBeenPlayed = !(kvp.Value[0] == 0);
                    return true;
                case "aspc": // Play count
                    PlayCount = kvp.Value.GetInt32Value();
                    return true;
                case "asdr": // Date released
                    DateReleased = kvp.Value.GetDateTimeValue();
                    return true;
            }

            return false;
        }

        #endregion

        #region HTTP Requests and Responses

        #region Play Song Command

        /// <summary>
        /// This method should only be used for playing a single, specific media item (such as a video).
        /// The methods in the Artist and Album classes should be used for songs so the necessary playlist is generated.
        /// </summary>
        public void SendPlayMediaItemCommand(Playlist basePlaylist)
        {
            // TODO: In the iPhone Remote app, the specific playlist persistent id is sent for the container spec rather
            // than the base playlist persistent id.  (For example, the Movies special playlist ID would be sent.)
            // Sending the base playlist ID seems to work but this may require further attention in the future.

            if (basePlaylist == null)
                basePlaylist = Server.BasePlaylist;

            string url = "/ctrl-int/1/playspec"
                + "?database-spec='dmap.persistentid:0x" + Server.DatabasePersistentID.ToString("x16") + "'"
                + "&container-spec='dmap.persistentid:0x" + basePlaylist.PersistentID.ToString("x16") + "'"
                + "&item-spec='dmap.itemid:0x" + ID.ToString("x8") + "'"
                + "&session-id=" + Server.SessionID;

            Server.SubmitHTTPPlayRequest(url);
        }

        #endregion

        #region Play Queue Command

        public void SendPlayQueueCommand(PlayQueueMode mode = PlayQueueMode.Replace)
        {
            if (!Server.SupportsPlayQueue)
                return;

            string url = "/ctrl-int/1/playqueue-edit"
                + "?command=add"
                + "&query='dmap.itemid:" + ID + "'"
                + "&mode=" + (int)mode
                + "&session-id=" + Server.SessionID;

            if (mode == PlayQueueMode.Replace)
                url += "&clear-previous=1";

            Server.SubmitHTTPPlayRequest(url);
        }

        #endregion

        #endregion
    }
}
