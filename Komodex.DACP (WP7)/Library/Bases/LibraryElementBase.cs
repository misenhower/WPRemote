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
using Komodex.Common;

namespace Komodex.DACP.Library
{
    public abstract class LibraryElementBase : ILibraryElement
    {
        protected LibraryElementBase()
        { }

        protected LibraryElementBase(DACPServer server, byte[] data)
            : this()
        {
            Server = server;
            DACPNodeDictionary nodes = DACPNodeDictionary.Parse(data);
            ProcessDACPNodes(nodes);
        }

        #region Properties

        public DACPServer Server { get; protected set; }
        public int ListIndex { get; internal set; }

        #endregion

        #region Methods

        protected virtual void ProcessDACPNodes(DACPNodeDictionary nodes)
        {
            ID = nodes.GetInt("miid");
            Name = nodes.GetString("minm");
        }

        #endregion

        #region ILibraryElement Members

        public virtual int ID { get; protected set; }

        private string _Name = null;
        public virtual string Name
        {
            get { return _Name ?? string.Empty; }
            protected set { _Name = value; }
        }

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

        // This method is used by the classes that inherit from LibraryElementBase
        protected void SendPropertyChanged(string propertyName)
        {
            PropertyChanged.RaiseOnUIThread(this, propertyName);
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #endregion
    }
}
