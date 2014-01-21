using Komodex.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Komodex.DACP
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public abstract class DacpElement : INotifyPropertyChanged
    {
        public DacpElement(DacpClient client, DacpNodeDictionary nodes)
        {
            Client = client;
            ProcessNodes(nodes);
        }

        public DacpClient Client { get; private set; }

        public int ID { get; private set; }
        public UInt64 PersistentID { get; private set; }
        public string Name { get; protected set; }

        protected virtual void ProcessNodes(DacpNodeDictionary nodes)
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
