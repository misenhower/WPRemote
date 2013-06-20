using Komodex.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Komodex.DACP
{
    public class PlayQueueItem
    {
        internal PlayQueueItem(DACPServer server, byte[] data)
        {
            Server = server;

            var nodes = DACPUtility.GetResponseNodes(data);
            foreach (var node in nodes)
            {
                switch (node.Key)
                {
                    case "ceQs":
                        byte[] value = node.Value;
                        byte[] dbID = { value[0], value[1], value[2], value[3] };
                        byte[] songID = { value[12], value[13], value[14], value[15] };
                        DatabaseID = dbID.GetInt32Value();
                        SongID = songID.GetInt32Value();
                        break;

                    case "ceQn":
                        SongName = node.Value.GetStringValue();
                        break;

                    case "ceQr":
                        ArtistName = node.Value.GetStringValue();
                        break;

                    case "ceQa":
                        AlbumName = node.Value.GetStringValue();
                        break;

                    case "ceQI":
                        // ceQI is a queue index value. The "currently playing" song has an index of 1. The first queued item has
                        // an index of 2. The first "history" item has an index of 0.
                        // This appears to be one off from how the index values are dealt with elsewhere, so I'm subtracting 1
                        // from the ceQI value to get the item's queue index.
                        QueueIndex = node.Value.GetInt32Value() - 1;
                        break;
                }
            }
        }

        public DACPServer Server { get; protected set; }

        public int DatabaseID { get; protected set; }
        public int SongID { get; protected set; }

        public string ArtistName { get; protected set; }
        public string SongName { get; protected set; }
        public string AlbumName { get; protected set; }
        public int QueueIndex { get; protected set; }

        public string SecondLine
        {
            get
            {
                if (!string.IsNullOrEmpty(AlbumName))
                    return ArtistName + " – " + AlbumName;
                return ArtistName;
            }
        }

        public string AlbumArtURL
        {
            get
            {
                int pixels = ResolutionUtility.GetScaledPixels(75);
                return Server.HTTPPrefix + "/databases/" + DatabaseID + "/items/" + SongID
                    + "/extra_data/artwork?mw=" + pixels + "&mh=" + pixels + "&session-id=" + Server.SessionID;
            }
        }

        public void SendPlayCommand()
        {
            string url = "/ctrl-int/1/playqueue-edit"
                + "?command=playnow"
                + "&index=" + QueueIndex
                + "&session-id=" + Server.SessionID;
            Server.SubmitHTTPPlayRequest(url);
        }

        public void SendDeleteCommand()
        {
            string url = "/ctrl-int/1/playqueue-edit"
                + "?command=remove"
                + "&items=" + QueueIndex
                + "&session-id=" + Server.SessionID;
            Server.SubmitHTTPPlayRequest(url);
        }
    }
}
