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
            _log.Info("Saving server info: {0} ({1})", info.ServiceID, info.Name);

            var oldServerInfo = PairedServers.FirstOrDefault(si => si.ServiceID == info.ServiceID);
            if (oldServerInfo != null)
                RemoveServerInfo(oldServerInfo);

            PairedServers.Add(info);

            info.IsAvailable = BonjourManager.DiscoveredServers.ContainsKey(info.ServiceID);
        }

        public static void RemoveServerInfo(ServerConnectionInfo info)
        {
            _log.Info("Removing server info: {0} ({1})", info.ServiceID, info.Name);

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

                _log.Info("Updating connection state: " + value);

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
                _log.Info("Bonjour found service: {0} ({1})", info.ServiceID, info.Name);

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
                if (SelectedServerInfo == info)
                {
                    if (CurrentServer == null)
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
                _log.Info("Bonjour removed service: {0} ({1})", info.ServiceID, info.Name);

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

                if (_currentServer != null)
                {
                    _currentServer.ServerUpdate -= DACPServer_ServerUpdate;
                    _currentServer.Stop();
                }

                _currentServer = value;

                if (_currentServer != null)
                    _currentServer.ServerUpdate += DACPServer_ServerUpdate;

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
                _log.Info("Setting current server to null...");

                CurrentServer = null;
                // TODO: Disconnect from server
                SelectedServerInfo = null;
                ConnectionState = ServerConnectionState.NoLibrarySelected;
                return;
            }

            _log.Info("Setting current server to: {0} ({1})", info.ServiceID, info.Name);

            SelectedServerInfo = info;

            CurrentServer = GetDACPServer(info);

            ConnectToServer();
        }

        public static void ConnectToServer()
        {
            if (CurrentServer == null || CurrentServer.IsConnected)
                return;

            if (ConnectionState == ServerConnectionState.ConnectingToLibrary)
                return;

            ConnectionState = ServerConnectionState.ConnectingToLibrary;
            _log.Info("Connecting to server...");
            CurrentServer.Start();
        }

        private static void DACPServer_ServerUpdate(object sender, ServerUpdateEventArgs e)
        {
            if (sender != CurrentServer)
                return;

            switch (e.Type)
            {
                case ServerUpdateType.ServerConnected:
                    ServerConnected();
                    break;

                case ServerUpdateType.Error:
                    ServerError(e.ErrorType);
                    break;

                case ServerUpdateType.LibraryError:
                    break;
            }
        }

        private static void ServerConnected()
        {
            ConnectionState = ServerConnectionState.Connected;
            _log.Info("Server connected successfully.");

            // Update saved data
            SelectedServerInfo.LastIPAddress = CurrentServer.Hostname;
            SelectedServerInfo.LastPort = CurrentServer.Port;
            SelectedServerInfo.Name = CurrentServer.LibraryName;
            _pairedServers.Save();
        }

        private static void ServerError(ServerErrorType type)
        {
            if (type == ServerErrorType.InvalidPIN)
            {
                _log.Warning("Invalid PIN error.");

                // Server is no longer paired with this client
                RemoveServerInfo(SelectedServerInfo);

                // TODO: Notify the user
            }
            else
            {
                if (!NetworkManager.IsLocalNetworkAvailable)
                {
                    ConnectionState = ServerConnectionState.WaitingForWiFiConnection;
                    return;
                }

                if (ConnectionState == ServerConnectionState.Connected)
                {
                    // The server was previously connected, just try to reconnect
                    ConnectionState = ServerConnectionState.ConnectingToLibrary;
                    _log.Info("Server disconnected, reconnecting...");
                    CurrentServer.Start();
                }
                else if (BonjourManager.DiscoveredServers.ContainsKey(SelectedServerInfo.ServiceID))
                {
                    _log.Info("Server reconnection failed, but still available via Bonjour.");

                    // A connection attempt failed, but we have the service in Bonjour.
                    NetService service = BonjourManager.DiscoveredServers[SelectedServerInfo.ServiceID];

                    // Select the next IP address and try to connect to it.
                    var ips = service.IPAddresses.Select(ip => ip.ToString()).ToList();
                    if (ips.Count > 0)
                    {
                        int index = ips.IndexOf(CurrentServer.Hostname);
                        index++;
                        if (index >= ips.Count)
                            index = 0;

                        _log.Info("Trying new IP: {0} (Previous: {1})", ips[index], CurrentServer.Hostname);

                        CurrentServer.Hostname = ips[index];
                    }

                    // Update the port if necessary
                    CurrentServer.Port = service.Port;

                    // TODO: Delay reconnection requests (particularly if the IP didn't change)

                    CurrentServer.Start();
                }
                else
                {
                    _log.Info("Server is no longer available via Bonjour.");
                    ConnectionState = ServerConnectionState.LookingForLibrary;
                }
            }
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
