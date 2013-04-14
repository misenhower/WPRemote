using Komodex.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Komodex.Remote.ServerManagement
{
    public class ServerConnectionInfo : INotifyPropertyChanged
    {
        /// <summary>
        /// Gets or sets the service ID, e.g., "17B30BC453C4B6A0"
        /// </summary>
        public string ServiceID { get; set; }

        /// <summary>
        /// Gets or sets the server type.
        /// </summary>
        public ServerType ServerType { get; set; }

        /// <summary>
        /// Gets or sets the server name, e.g., "Matt's Library"
        /// </summary>
        public string Name { get; set; }

        private string _lastHostname;
        /// <summary>
        /// Gets or sets the last known hostname, e.g., "mycomputer.local."
        /// </summary>
        public string LastHostname
        {
            get { return _lastHostname; }
            set
            {
                if (_lastHostname == value)
                    return;

                _lastHostname = value;
                PropertyChanged.RaiseOnUIThread(this, "LastHostname");
            }
        }

        /// <summary>
        /// Gets or sets the last known IP address, e.g., "10.0.0.1"
        /// </summary>
        public string LastIPAddress { get; set; }

        private bool _isAvailable;
        /// <summary>
        /// Gets or sets whether the server is currently available
        /// </summary>
        public bool IsAvailable
        {
            get { return _isAvailable; }
            set
            {
                if (_isAvailable == value)
                    return;

                _isAvailable = value;
                PropertyChanged.RaiseOnUIThread(this, "IsAvailable");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
