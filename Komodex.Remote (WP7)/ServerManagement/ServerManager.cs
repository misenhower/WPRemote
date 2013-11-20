using Komodex.Analytics;
using Komodex.Bonjour;
using Komodex.Common;
using Komodex.Common.Phone;
using Komodex.DACP;
using Komodex.Networking;
using Komodex.Remote.Settings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
            Utility.BeginInvokeOnUIThread(() =>
            {
                _log.Info("Saving server info: '{0}' ({1})", info.Name, info.ServiceID);

                var oldServerInfo = PairedServers.FirstOrDefault(si => si.ServiceID == info.ServiceID);
                if (oldServerInfo != null)
                    RemoveServerInfo(oldServerInfo);

                PairedServers.Add(info);

                info.IsAvailable = BonjourManager.DiscoveredServers.ContainsKey(info.ServiceID);
                UpdateServerInfoFromBonjour(info);
            });
        }

        public static void RemoveServerInfo(ServerConnectionInfo info)
        {
            Utility.BeginInvokeOnUIThread(() =>
            {
                _log.Info("Removing server info: '{0}' ({1})", info.Name, info.ServiceID);

                if (SelectedServerInfo == info)
                    ChooseServer(null);

                PairedServers.Remove(info);
            });
        }

        private static void UpdateServerInfoFromBonjour(ServerConnectionInfo info)
        {
            var service = BonjourManager.DiscoveredServers.GetValueOrDefault(info.ServiceID);
            if (service == null)
                return;

            // Check whether any of the stored data for this server is out of date
            bool dirty = false;

            string serviceName = service.TXTRecordData.GetValueOrDefault("CtlN", info.Name);
            if (info.Name != serviceName)
            {
                info.Name = serviceName;
                dirty = true;
            }
            if (info.LastHostname != service.Hostname)
            {
                info.LastHostname = service.Hostname;
                dirty = true;
            }
            if (info.LastPort != service.Port)
            {
                info.LastPort = service.Port;
                dirty = true;
            }

            // Determine the server type
            ServerType serverType = ServerType.iTunes;
            string dvTy = service.TXTRecordData.GetValueOrDefault("DvTy");
            if (!string.IsNullOrEmpty(dvTy))
            {
                dvTy = dvTy.ToLower();
                if (dvTy.Contains("touchremote"))
                    serverType = ServerType.Foobar;
                else if (dvTy.Contains("monkeytunes"))
                    serverType = ServerType.MediaMonkey;
                else if (dvTy.Contains("albumplayer"))
                    serverType = ServerType.AlbumPlayer;
            }
            if (info.ServerType != serverType)
            {
                info.ServerType = serverType;
                dirty = true;
            }

            // Save the paired servers list if necessary
            if (dirty)
                _pairedServers.Save();
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
                _log.Info("Bonjour found service: '{0}' ({1})", info.Name, info.ServiceID);

                info.IsAvailable = true;

                UpdateServerInfoFromBonjour(info);

                // Connect to the server if necessary
                if (SelectedServerInfo == info)
                    AutoConnect();
            }
        }

        private static void BonjourManager_ServiceRemoved(object sender, NetServiceEventArgs e)
        {
            ServerConnectionInfo info = PairedServers.FirstOrDefault(si => si.ServiceID == e.Service.Name);
            if (info != null)
            {
                _log.Info("Bonjour removed service: '{0}' ({1})", info.Name, info.ServiceID);

                info.IsAvailable = false;
            }
        }

        #endregion

        #region Network Availability

        private static void NetworkManager_NetworkAvailabilityChanged(object sender, NetworkAvailabilityChangedEventArgs e)
        {
            if (e.IsLocalNetworkAvailable)
            {
                if (ConnectionState == ServerConnectionState.WaitingForWiFiConnection)
                {
                    WakeServer();
                    AutoConnect();
                }
            }
            else
            {
                if (ConnectionState == ServerConnectionState.LookingForLibrary)
                    ConnectionState = ServerConnectionState.WaitingForWiFiConnection;

                // Set all services to unavailable
                foreach (ServerConnectionInfo info in PairedServers)
                    info.IsAvailable = false;
            }
        }

        #endregion

        #region Server Selection

        private static readonly Setting<string> _selectedServerID = new Setting<string>("SelectedServerID");
        public static ServerConnectionInfo SelectedServerInfo
        {
            get { return PairedServers.FirstOrDefault(si => si.ServiceID == _selectedServerID.Value); }
            private set
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
                    _currentServer.ConnectionError -= DACPServer_ConnectionError;
                    _currentServer.Disconnect();
                }

                _currentServer = value;

                if (_currentServer != null)
                    _currentServer.ConnectionError += DACPServer_ConnectionError;

                CurrentServerChanged.RaiseOnUIThread(null, new EventArgs());
            }
        }

        public static void ChooseServer(ServerConnectionInfo info)
        {
            DisconnectCurrentServer();
            CurrentServer = null;
            ConnectionState = ServerConnectionState.NoLibrarySelected;

            if (!PairedServers.Contains(info))
            {
                _log.Info("Setting current server to null...");
                SelectedServerInfo = null;
                return;
            }

            _log.Info("Setting current server to: '{0}' ({1})", info.Name, info.ServiceID);
            SelectedServerInfo = info;
            WakeServer();
            AutoConnect();
        }

        #endregion

        #region Server Connection

        private static readonly TimeSpan _reconnectionDelay = TimeSpan.FromSeconds(2);
        private static CancellationTokenSource _currentConnectionCancellationTokenSource;

        private static void AutoConnect()
        {
            if (SelectedServerInfo == null)
                return;

            if (CurrentServer == null)
            {
                DACPServer newServer = GetDACPServer(SelectedServerInfo);
                if (newServer == null)
                {
                    ConnectionState = ServerConnectionState.LookingForLibrary;
                    return;
                }

                CurrentServer = newServer;
            }
            else
            {
                if (CurrentServer.IsConnected)
                    return;

                bool forceReconnect = false;

                // Update the IP from Bonjour if we can
                if (BonjourManager.DiscoveredServers.ContainsKey(SelectedServerInfo.ServiceID))
                {
                    var service = BonjourManager.DiscoveredServers[SelectedServerInfo.ServiceID];
                    var ips = service.IPAddresses.Select(ip => ip.ToString()).ToList();
                    if (ips.Count > 0 && !ips.Contains(CurrentServer.Hostname))
                    {
                        CurrentServer.Hostname = ips[0];
                        forceReconnect = true;
                    }
                    if (CurrentServer.Port != service.Port)
                    {
                        CurrentServer.Port = service.Port;
                        forceReconnect = true;
                    }
                }

                if (ConnectionState == ServerConnectionState.ConnectingToLibrary && !forceReconnect)
                    return;

                if (forceReconnect)
                {
                    _log.Info("Forcing reconnection with a new address/port from Bonjour...");
                    DisconnectCurrentServer();
                }
            }

            TrialManager.StartTrial();
            if (TrialManager.TrialState == TrialState.Expired)
                return;

            ConnectionState = ServerConnectionState.ConnectingToLibrary;
            _log.Info("Connecting to server...");

            ConnectCurrentServer();
        }


        private static async void ConnectCurrentServer()
        {
            DisconnectCurrentServer();

            // Set up a cancellation token source
            _currentConnectionCancellationTokenSource = new CancellationTokenSource();
            var token = _currentConnectionCancellationTokenSource.Token;
            var server = CurrentServer;

            while (!token.IsCancellationRequested && server == CurrentServer)
            {
                // Attempt to connect to the server
                _log.Info("Attempting to connect to server at {0}:{1}", server.Hostname, server.Port);
                var result = await server.ConnectAsync(token);

                // If this connection attempt has been cancelled, don't do anything else
                if (token.IsCancellationRequested || server != CurrentServer)
                    return;

                switch (result)
                {
                    case ConnectionResult.Success:
                        server.StartUpdateRequests();
                        HandleServerConnected();
                        return;

                    case ConnectionResult.InvalidPIN:
                        HandleInvalidPIN();
                        return;

                    case ConnectionResult.ConnectionError:
                        NetService service = BonjourManager.DiscoveredServers.GetValueOrDefault(SelectedServerInfo.ServiceID);
                        if (service == null)
                        {
                            _log.Info("Server is not currently available via Bonjour.");
                            ConnectionState = ServerConnectionState.LookingForLibrary;
                            return;
                        }

                        // A connection attempt failed, but we have the service in Bonjour.
                        _log.Info("Server connection failed, but server is available via Bonjour.");

                        // Select the next IP address and try to connect to it.
                        var ips = service.IPAddresses.Select(ip => ip.ToString()).ToList();
                        bool hasNewIP = false;
                        if (ips.Count > 0)
                        {
                            int index = ips.IndexOf(CurrentServer.Hostname);
                            index++;
                            if (index >= ips.Count)
                                index = 0;

                            if (ips[index] != CurrentServer.Hostname)
                            {
                                _log.Info("Trying new IP: {0} (Previous: {1})", ips[index], CurrentServer.Hostname);
                                CurrentServer.Hostname = ips[index];
                                hasNewIP = true;
                            }
                        }

                        // Update the port if necessary
                        CurrentServer.Port = service.Port;

                        // Delay reconnection if the IP hasn't changed
                        if (!hasNewIP)
                        {
                            _log.Info("Delaying reconnection...");
#if WP7
                            await TaskEx.Delay(_reconnectionDelay);
#else
                            await Task.Delay(_reconnectionDelay);
#endif
                        }
                        break;
                }
            }
        }

        private static void DisconnectCurrentServer()
        {
            if (_currentConnectionCancellationTokenSource != null)
            {
                _currentConnectionCancellationTokenSource.Cancel();
                _currentConnectionCancellationTokenSource = null;
            }

            if (CurrentServer != null)
                CurrentServer.Disconnect();
        }

        private static void WakeServer()
        {
            if (SelectedServerInfo == null)
                return;

            if (SelectedServerInfo.MACAddresses == null)
                return;

            foreach (var address in SelectedServerInfo.MACAddresses)
            {
                if (address == null || address.Length != 12)
                    continue;

                try
                {
                    byte[] addressBytes = BitUtility.FromHexString(address);
                    WakeOnLAN.SendWOLPacket(addressBytes);
                }
                catch { }
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

                if (ip == null)
                    return null;

                return new DACPServer(ip, service.Port, info.PairingCode);
            }
            else if (info.LastIPAddress != null && info.LastPort > 0)
            {
                return new DACPServer(info.LastIPAddress, info.LastPort, info.PairingCode);
            }

            return null;
        }

        private static void DACPServer_ConnectionError(object sender, EventArgs e)
        {
            string details = null;
            _log.Info("Server connection error.");

            // If the network is no longer available, wait for it to become available again
            if (!NetworkManager.IsLocalNetworkAvailable)
            {
                ConnectionState = ServerConnectionState.WaitingForWiFiConnection;
                return;
            }

            // Report the error
            bool reportError = SettingsManager.Current.ExtendedErrorReporting;
            // Disable error reporting for now
            reportError = false;
#if DEBUG
            // Always report errors in debug builds
            reportError = true;
#endif
            if (reportError)
            {
                if (!string.IsNullOrEmpty(details) && !details.Contains("/server-info"))
                    CrashReporter.LogMessage(details, "DACP Server Error", true);
            }

            // The server was previously connected, just try to reconnect
            ConnectionState = ServerConnectionState.ConnectingToLibrary;
            _log.Info("Server disconnected, reconnecting...");
            ConnectCurrentServer();
        }

        private static void HandleServerConnected()
        {
            ConnectionState = ServerConnectionState.Connected;
            _log.Info("Server connected successfully.");

            // Update saved data
            SelectedServerInfo.LastIPAddress = CurrentServer.Hostname;
            SelectedServerInfo.LastPort = CurrentServer.Port;
            SelectedServerInfo.Name = CurrentServer.LibraryName;
            SelectedServerInfo.MACAddresses = CurrentServer.MACAddresses;
            _pairedServers.Save();
        }

        private static void HandleInvalidPIN()
        {
            _log.Warning("Invalid PIN error.");

            if (SelectedServerInfo.IsAvailable)
            {
                // Server is no longer paired with this client
                RemoveServerInfo(SelectedServerInfo);
            }
            else
            {
                // We may have connected to the wrong server
                if (ConnectionState == ServerConnectionState.ConnectingToLibrary)
                    ConnectionState = ServerConnectionState.LookingForLibrary;
            }

            // TODO: Notify the user
        }

        #endregion

    }
}
