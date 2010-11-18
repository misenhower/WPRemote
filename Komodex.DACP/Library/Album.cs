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
    public class Album : IDACPResponseHandler,INotifyPropertyChanged
    {
        private Album()
        { }

        public Album(DACPServer server, string name)
        {
            Server = server;
            Name = name;
        }

        public Album(DACPServer server, byte[] data)
        {
            Server = server;
            ParseByteData(data);
        }

        #region Properties

        public string Name { get; protected set; }
        public DACPServer Server { get; protected set; }

        //private ObservableCollection<Album> _Albums = null;
        //public ObservableCollection<Album> Albums
        //{
        //    get { return _Albums; }
        //    protected set
        //    {
        //        if (_Albums == value)
        //            return;
        //        _Albums = value;
        //        SendPropertyChanged("Albums");
        //    }
        //}

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
                        Name = kvp.Value.GetStringValue();
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
            throw new NotImplementedException();
        }

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
