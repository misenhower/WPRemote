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
using System.Collections.Generic;

namespace Komodex.DACP.Library
{
    public abstract class LibraryElementBase : ILibraryElement
    {
        #region Properties

        public DACPServer Server { get; protected set; }

        #endregion

        #region Methods

        protected void ParseByteData(byte[] data)
        {
            var nodes = Utility.GetResponseNodes(data);
            foreach (var kvp in nodes)
                ProcessByteKVP(kvp);
        }

        protected virtual bool ProcessByteKVP(KeyValuePair<string, byte[]> kvp)
        {
            switch (kvp.Key)
            {
                case "miid": // ID
                    ID = kvp.Value.GetInt32Value();
                    return true;
                case "minm": // Name
                    Name = kvp.Value.GetStringValue();
                    return true;
                default:
                    return false;
            }
        }

        #endregion

        #region ILibraryElement Members

        public virtual int ID { get; protected set; }

        public virtual string Name { get; protected set; }

        public virtual string SecondLine
        {
            get { return null; }
        }

        public virtual string AlbumArtURL
        {
            get { return null; }
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
