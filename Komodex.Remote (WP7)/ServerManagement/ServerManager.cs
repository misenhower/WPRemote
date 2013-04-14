using Komodex.Bonjour;
using Komodex.Common;
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

            if (SelectedServerInfo != null)
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
            // TODO: Check whether this is the server we're connected to

            PairedServers.Remove(info);
        }

        #endregion

        #region State

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
                // TODO: Save new host. (New IP is saved if we connect to it.)
                if (SelectedServerInfo == info)
                {
                    // TODO
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

        #region Server Connections

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

        public static void ChooseServer(ServerConnectionInfo info)
        {
            SelectedServerInfo = info;
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
            }
        }
    }
}
