﻿using Komodex.Common;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Net.NetworkInformation;
using Microsoft.Phone.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Komodex.Remote.ServerManagement
{
    public static class NetworkManager
    {
        private static readonly Log _log = new Log("Network Manager");

        public static event EventHandler<NetworkAvailabilityChangedEventArgs> NetworkAvailabilityChanged;

#if WP7
        private static bool _updateOnNavigate;
#endif

        public static void Initialize()
        {
            // Hook into events
            DeviceNetworkInformation.NetworkAvailabilityChanged += (sender, e) => UpdateNetworkAvailability();

#if WP8
            // Windows Phone 8.1 seems to occasionally raise DeviceNetworkInformation.NetworkAvailabilityChanged
            // before the WiFi network actually becomes available. NetworkInformation.NetworkStatusChanged is
            // called a few seconds later.
            Windows.Networking.Connectivity.NetworkInformation.NetworkStatusChanged += (sender) => UpdateNetworkAvailability();
#endif

#if WP7
            App.RootFrame.Navigated += (sender, e) => { if (_updateOnNavigate) { UpdateNetworkAvailability(); _updateOnNavigate = false; } };
            PhoneApplicationService.Current.Launching += (sender, e) => _updateOnNavigate = true;
            PhoneApplicationService.Current.Activated += (sender, e) => _updateOnNavigate = true;
#else
            PhoneApplicationService.Current.Launching += (sender, e) => UpdateNetworkAvailability();
            PhoneApplicationService.Current.Activated += (sender, e) => UpdateNetworkAvailability();
#endif
            PhoneApplicationService.Current.Deactivated += (sender, e) => SetLocalNetworkAvailability(false, true);
            PhoneApplicationService.Current.Closing += (sender, e) => SetLocalNetworkAvailability(false, true);
        }

        public static bool IsLocalNetworkAvailable { get; private set; }

        private static void SetLocalNetworkAvailability(bool networkAvailable, bool shuttingDown)
        {
            if (IsLocalNetworkAvailable == networkAvailable)
                return;

            _log.Info("Local network available: " + networkAvailable + ((shuttingDown) ? " (Shutting down)" : ""));

            IsLocalNetworkAvailable = networkAvailable;
            NetworkAvailabilityChanged.Raise(null, new NetworkAvailabilityChangedEventArgs(IsLocalNetworkAvailable, shuttingDown));
        }

        private static void UpdateNetworkAvailability()
        {
            _log.Info("Updating local network availability...");

            if (Microsoft.Devices.Environment.DeviceType == Microsoft.Devices.DeviceType.Emulator)
            {
                _log.Info("Running in emulator, so assuming network is available...");
                SetLocalNetworkAvailability(true, false);
                return;
            }

            NetworkInterfaceList interfaces = new NetworkInterfaceList();

            if (interfaces != null)
            {
#if DEBUG
                if (_log.EffectiveLevel <= LogLevel.Debug)
                {
                    string interfaceFormat = "[{0}]\n\tDescription: {1}\n\tType: {2}\n\tSubtype: {3}\n\tState: {4}\n\tBandwidth: {5}\n\n";
                    string interfaceList = string.Empty;
                    foreach (var i in interfaces)
                        interfaceList += string.Format(interfaceFormat, i.InterfaceName, i.Description, i.InterfaceType, i.InterfaceSubtype, i.InterfaceState, i.Bandwidth);
                    _log.Debug("Network interfaces:\n" + interfaceList.TrimEnd());
                }
#endif

                bool networkAvailable = interfaces.Any(i => i.InterfaceState == ConnectState.Connected && i.InterfaceType == NetworkInterfaceType.Wireless80211);
                SetLocalNetworkAvailability(networkAvailable, false);
            }
            else
            {
                SetLocalNetworkAvailability(false, false);
            }
        }
    }

    public class NetworkAvailabilityChangedEventArgs : EventArgs
    {
        public NetworkAvailabilityChangedEventArgs(bool isLocalNetworkAvailable, bool shuttingDown)
        {
            IsLocalNetworkAvailable = isLocalNetworkAvailable;
            ShuttingDown = shuttingDown;
        }

        public bool IsLocalNetworkAvailable { get; protected set; }
        public bool ShuttingDown { get; protected set; }
    }
}
