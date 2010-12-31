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

namespace Komodex.DACP.Library
{
    public class MediaItem : LibraryElementBase
    {
        private MediaItem()
        { }

        //public Song(DACPServer server, string name)
        //{
        //    Server = server;
        //    Name = name;
        //}

        public MediaItem(DACPServer server, byte[] data)
        {
            Server = server;
            ParseByteData(data);
        }

        #region Properties

        public int ContainerItemID { get; protected set; }
        public string ArtistName { get; protected set; }
        public string AlbumName { get; protected set; }
        public string ArtistAndAlbum
        {
            get { return ArtistName + " – " + AlbumName; }
        }

        public override string SecondLine
        {
            get { return ArtistName; }
        }

        public override string AlbumArtURL
        {
            get
            {
                return Server.HTTPPrefix + "/databases/" + Server.DatabaseID + "/items/" + ID
                    + "/extra_data/artwork?mw=75&mh=75&group-type=albums&session-id=" + Server.SessionID;
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
                case "mcti": // Container item ID
                    ContainerItemID = kvp.Value.GetInt32Value();
                    return true;
                case "asar": // Artist name
                    ArtistName = kvp.Value.GetStringValue();
                    return true;
                case "asal": // Album name
                    AlbumName = kvp.Value.GetStringValue();
                    return true;
                default:
                    return false;
            }
        }

        #endregion

        #region HTTP Requests and Responses

        #region Play Song Command

        /// <summary>
        /// This method should only be used for playing a single, specific media item (such as a video).
        /// The methods in the Artist and Album classes should be used for songs so the necessary playlist is generated.
        /// </summary>
        public void SendPlayMediaItemCommand()
        {
            // TODO: In the iPhone Remote app, the specific playlist persistent id is sent for the container spec rather
            // than the base playlist persistent id.  (For example, the Movies special playlist ID would be sent.)
            // Sending the base playlist ID seems to work but this may require further attention in the future.

            string url = "/ctrl-int/1/playspec"
                + "?database-spec='dmap.persistentid:0x" + Server.DatabasePersistentID.ToString("x16") + "'"
                + "&container-spec='dmap.persistentid:0x" + Server.BasePlaylist.PersistentID.ToString("x16") + "'"
                + "&item-spec='dmap.itemid:0x" + ID.ToString("x8") + "'"
                + "&session-id=" + Server.SessionID;

            Server.SubmitHTTPRequest(url);
        }

        #endregion

        #endregion
    }
}
