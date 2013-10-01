using Komodex.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Komodex.DACP
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public abstract class DACPElement : INotifyPropertyChanged
    {
        public DACPElement(DACPServer server, DACPNodeDictionary nodes)
        {
            Server = server;
            ProcessNodes(nodes);
        }

        public DACPServer Server { get; private set; }

        public int ID { get; private set; }
        public UInt64 PersistentID { get; private set; }
        public string Name { get; private set; }

        protected virtual void ProcessNodes(DACPNodeDictionary nodes)
        {
            ID = nodes.GetInt("miid");
            PersistentID = (UInt64)nodes.GetLong("mper");
            Name = nodes.GetString("minm");
        }

        protected virtual string DebuggerDisplay
        {
            get { return string.Format("{0} ID: {1}, Name: \"{2}\"", this.GetType().Name, ID, Name); }
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected void SendPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (propertyName == null)
                throw new ArgumentNullException("propertyName");

            PropertyChanged.RaiseOnUIThread(this, propertyName);
        }

        #endregion
    }
}
