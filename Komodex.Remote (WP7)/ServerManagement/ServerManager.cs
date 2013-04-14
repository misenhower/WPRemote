using Komodex.Bonjour;
using Komodex.Common;
using Komodex.DACP;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Komodex.Remote.ServerManagement
{
    public static class ServerManager
    {
        private static readonly Log _log = new Log("Server Manager");

        public static void Initialize()
        {
            NetworkManager.NetworkAvailabilityChanged += NetworkManager_NetworkAvailabilityChanged;
            BonjourManager.ServiceAdded += BonjourManager_ServiceAdded;
            BonjourManager.ServiceRemoved += BonjourManager_ServiceRemoved;

            if (SelectedServerInfo == null)
                ConnectionState = ServerConnectionState.NoLibrarySelected;
            else
                ConnectionState = ServerConnectionState.WaitingForWiFiConnection;
        }

        #region Paired Servers

        private static readonly Setting<ServerConnectionInfoCollection> _pairedServers = new Setting<ServerConnectionInfoCollection>("PairedServerList", new ServerConnectionInfoCollection());
        public static ServerConnectionInfoCollection PairedServers
        {
            get { return _pairedServers.Value; }
        }

        public static void AddServerInfo(ServerConnectionInfo info)
        {
            var oldServerInfo = PairedServers.FirstOrDefault(si => si.ServiceID == info.ServiceID);
            if (oldServerInfo != null)
                RemoveServerInfo(oldServerInfo);

            PairedServers.Add(info);
        }

        public static void RemoveServerInfo(ServerConnectionInfo info)
        {
            if (SelectedServerInfo == info)
                ChooseServer(null);

            PairedServers.Remove(info);
        }

        #endregion

        #region Connection State

        public static event EventHandler<ConnectionStateChangedEventArgs> ConnectionStateChanged;

        private static ServerConnectionState _connectionState;
        public static ServerConnectionState ConnectionState
        {
            get { return _connectionState; }
            set
            {
                if (_connectionState == value)
                    return;

                _connectionState = value;
                ConnectionStateChanged.Raise(null, new ConnectionStateChangedEventArgs(_connectionState));
            }
        }

        #endregion

        #region Bonjour

        private static void BonjourManager_ServiceAdded(object sender, NetServiceEventArgs e)
        {
            ServerConnectionInfo info = PairedServers.FirstOrDefault(si => si.ServiceID == e.Service.Name);
            if (info != null)
            {
                info.IsAvailable = true;

                // Check whether any of the stored data for this server is out of date
                bool dirty = false;

                string serviceName = e.Service.TXTRecordData.GetValueOrDefault("CtlN", info.Name);
                if (info.Name != serviceName)
                {
                    info.Name = serviceName;
                    dirty = true;
                }
                if (info.LastHostname != e.Service.Hostname)
                {
                    info.LastHostname = e.Service.Hostname;
                    dirty = true;
                }
                if (info.LastPort != e.Service.Port)
                {
                    info.LastPort = e.Service.Port;
                    dirty = true;
                }

                // Save the paired servers list if necessary
                if (dirty)
                    _pairedServers.Save();

                // Connect to the server if necessary
                if (SelectedServerInfo == info && CurrentServer == null)
                {
                    CurrentServer = GetDACPServer(info);
                    ConnectToServer();
                }
            }
        }

        private static void BonjourManager_ServiceRemoved(object sender, NetServiceEventArgs e)
        {
            ServerConnectionInfo info = PairedServers.FirstOrDefault(si => si.ServiceID == e.Service.Name);
            if (info != null)
            {
                info.IsAvailable = false;
            }
        }

        #endregion

        #region Server Connection

        private static readonly Setting<string> _selectedServerID = new Setting<string>("SelectedServerID");
        public static ServerConnectionInfo SelectedServerInfo
        {
            get { return PairedServers.FirstOrDefault(si => si.ServiceID == _selectedServerID.Value); }
            set
            {
                if (value == null)
                    _selectedServerID.Value = null;
                else
                    _selectedServerID.Value = value.ServiceID;
            }
        }

        public static event EventHandler<EventArgs> CurrentServerChanged;

        private static DACPServer _currentServer;
        public static DACPServer CurrentServer
        {
            get { return _currentServer; }
            private set
            {
                if (_currentServer == value)
                    return;

                _currentServer = value;
                CurrentServerChanged.RaiseOnUIThread(null, new EventArgs());
            }
        }

        private static DACPServer GetDACPServer(ServerConnectionInfo info)
        {
            if (BonjourManager.DiscoveredServers.ContainsKey(info.ServiceID))
            {
                var service = BonjourManager.DiscoveredServers[info.ServiceID];
                string ip;
                if (service.IPAddresses.Count > 0)
                    ip = service.IPAddresses[0].ToString();
                else
                    ip = info.LastIPAddress;

                return new DACPServer(ip, service.Port, info.PairingCode);
            }
            else
            {
                return new DACPServer(info.LastIPAddress, info.LastPort, info.PairingCode);
            }
        }

        public static void ChooseServer(ServerConnectionInfo info)
        {
            if (!PairedServers.Contains(info))
            {
                CurrentServer = null;
                // TODO: Disconnect from server
                SelectedServerInfo = null;
                return;
            }

            SelectedServerInfo = info;

            CurrentServer = GetDACPServer(info);

            ConnectToServer();
        }

        public static void ConnectToServer()
        {
        }

        #endregion

        private static void NetworkManager_NetworkAvailabilityChanged(object sender, NetworkAvailabilityChangedEventArgs e)
        {
            if (e.IsLocalNetworkAvailable)
            {
                if (ConnectionState == ServerConnectionState.WaitingForWiFiConnection)
                    ConnectionState = ServerConnectionState.LookingForLibrary;
            }
            else
            {
                // TODO
                // Set all services to unavailable
            }
        }
    }
}
