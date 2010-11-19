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

namespace Komodex.DACP.Library
{
    public class Playlist
    {
        private Playlist()
        { }

        public Playlist(DACPServer server, byte[] data)
        {
            Server = server;
            ParseByteData(data);
        }

        #region Properties

        public int ID { get; protected set; }
        public string Name { get; protected set; }
        public DACPServer Server { get; protected set; }
        public bool BasePlaylist { get; protected set; }

        // TODO: Observable collection of songs...

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
                    case "minm": // Name
                        Name = kvp.Value.GetStringValue();
                        break;
                    case "abpl": // Base playlist
                        if (kvp.Value[0] >= 1)
                            BasePlaylist = true;
                        break;
                    default:
                        break;
                }
            }
        }

        #endregion
    }
}
