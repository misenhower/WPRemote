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
    public class Song : ILibraryItem
    {
        private Song()
        { }

        //public Song(DACPServer server, string name)
        //{
        //    Server = server;
        //    Name = name;
        //}

        public Song(DACPServer server, byte[] data)
        {
            Server = server;
            ParseByteData(data);
        }

        #region Properties

        public DACPServer Server { get; protected set; }
        public int ID { get; protected set; }
        public string Name { get; protected set; }
        public string ArtistName { get; protected set; }
        public string AlbumName { get; protected set; }

        public string SecondLine
        {
            get { return ArtistName; }
        }

        public string AlbumArtURL
        {
            get
            {
                return Server.HTTPPrefix + "/databases/" + Server.DatabaseID + "/items/" + ID
                    + "/extra_data/artwork?mw=75&mh=75&group-type=albums&session-id=" + Server.SessionID;
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
                    case "asar": // Artist name
                        ArtistName = kvp.Value.GetStringValue();
                        break;
                    case "asal": // Album name
                        AlbumName = kvp.Value.GetStringValue();
                        break;
                    default:
                        break;
                }
            }
        }

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
