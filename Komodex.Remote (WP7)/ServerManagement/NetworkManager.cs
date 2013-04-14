using Komodex.Common;
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

        public static void Initialize()
        {
            // Hook into events
            DeviceNetworkInformation.NetworkAvailabilityChanged += (sender, e) => UpdateNetworkAvailability();
            PhoneApplicationService.Current.Launching += (sender, e) => UpdateNetworkAvailability();
            PhoneApplicationService.Current.Activated += (sender, e) => UpdateNetworkAvailability();
            PhoneApplicationService.Current.Deactivated += (sender, e) => IsLocalNetworkAvailable = false;
            PhoneApplicationService.Current.Closing += (sender, e) => IsLocalNetworkAvailable = false;
        }

        private static bool _isLocalNetworkAvailable;
        public static bool IsLocalNetworkAvailable
        {
            get { return _isLocalNetworkAvailable; }
            set
            {
                if (_isLocalNetworkAvailable == value)
                    return;

                _log.Info("Local network available: " + value);

                _isLocalNetworkAvailable = value;
                NetworkAvailabilityChanged.Raise(null, new NetworkAvailabilityChangedEventArgs(IsLocalNetworkAvailable));
            }
        }

        private static void UpdateNetworkAvailability()
        {
            _log.Info("Updating local network availability...");

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

                IsLocalNetworkAvailable = interfaces.Any(i => i.InterfaceState == ConnectState.Connected
                    && !(i.Bandwidth == -1 && (i.InterfaceName.Contains("Loopback") || i.Description.Contains("Loopback") || i.InterfaceName.Contains("{22C7611B-530E-11DB-BA31-806E6F6E6963}")))
                    && (i.InterfaceType == NetworkInterfaceType.Wireless80211 || i.InterfaceType == NetworkInterfaceType.Ethernet));
            }
            else
            {
                IsLocalNetworkAvailable = false;
            }
        }
    }

    public class NetworkAvailabilityChangedEventArgs : EventArgs
    {
        public NetworkAvailabilityChangedEventArgs(bool isLocalNetworkAvailable)
        {
            IsLocalNetworkAvailable = isLocalNetworkAvailable;
        }

        public bool IsLocalNetworkAvailable { get; protected set; }
    }
}
