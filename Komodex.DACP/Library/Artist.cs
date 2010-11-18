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
    public class Artist
    {
        private Artist()
        { }

        public Artist(DACPServer server, string name)
        {
            ArtistName = name;
        }

        public Artist(DACPServer server, byte[] data)
        {
            ParseByteData(data);
        }

        #region Properties

        public string ArtistName { get; protected set; }

        #endregion

        #region Methods

        private void ParseByteData(byte[] data)
        {
            var nodes = Utility.GetResponseNodes(data);
            foreach (var kvp in nodes)
            {
                switch (kvp.Key)
                {
                    case "minm":
                        ArtistName = kvp.Value.GetStringValue();
                        break;
                    default:
                        break;
                }
            }
        }

        #endregion
    }
}
