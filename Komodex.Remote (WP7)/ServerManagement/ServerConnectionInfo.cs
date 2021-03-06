﻿using Komodex.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Komodex.Remote.ServerManagement
{
    [XmlType("ServerConnectionInfo")]
    public class ServerConnectionInfo : INotifyPropertyChanged
    {
        /// <summary>
        /// Gets the service ID, e.g., "17B30BC453C4B6A0"
        /// </summary>
        public string ServiceID { get; set; }

        /// <summary>
        /// Gets the pairing code, e.g., "7474C23600F8710E"
        /// </summary>
        public string PairingCode { get; set; }

        private ServerType _serverType;
        /// <summary>
        /// Gets or sets the server type.
        /// </summary>
        public ServerType ServerType
        {
            get { return _serverType; }
            set
            {
                if (_serverType == value)
                    return;

                _serverType = value;
                PropertyChanged.RaiseOnUIThread(this, "ServerType");
            }
        }

        private string _name;
        /// <summary>
        /// Gets or sets the server name, e.g., "Matt's Library"
        /// </summary>
        public string Name
        {
            get { return _name; }
            set
            {
                if (_name == value)
                    return;

                _name = value;
                PropertyChanged.RaiseOnUIThread(this, "Name");
            }
        }

        /// <summary>
        /// Gets or sets the last known hostname, e.g., "mycomputer.local."
        /// </summary>
        public string LastHostname { get; set; }

        /// <summary>
        /// Gets or sets the last known IP address, e.g., "10.0.0.1"
        /// </summary>
        public string LastIPAddress { get; set; }

        /// <summary>
        /// Gets or sets the last known port, e.g., 3689
        /// </summary>
        public int LastPort { get; set; }

        /// <summary>
        /// Gets or sets the list of MAC addresses.
        /// </summary>
        [XmlArrayItem("MACAddress")]
        public string[] MACAddresses { get; set; }

        private bool _isAvailable;
        /// <summary>
        /// Gets or sets whether the server is currently available
        /// </summary>
        [XmlIgnore]
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

    public class ServerConnectionInfoEventArgs : EventArgs
    {
        public ServerConnectionInfoEventArgs(ServerConnectionInfo info)
        {
            ConnectionInfo = info;
        }

        public ServerConnectionInfo ConnectionInfo { get; protected set; }
    }
}
