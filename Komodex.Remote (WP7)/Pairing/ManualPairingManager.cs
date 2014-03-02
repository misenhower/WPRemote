using Komodex.Bonjour;
using Komodex.Common;
using Komodex.Remote.ServerManagement;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Komodex.Remote.Pairing
{
    public static class ManualPairingManager
    {
        private static readonly Log _log = new Log("Manual Pairing");
        private const string PairingUtilityServiceName = "_komodex-pairing._tcp.local.";

        private static NetServiceBrowser _pairingUtilityBrowser;

        private static readonly ObservableCollection<DiscoveredPairingUtility> _discoveredPairingUtilities = new ObservableCollection<DiscoveredPairingUtility>();
        public static ObservableCollection<DiscoveredPairingUtility> DiscoveredPairingUtilities
        {
            get { return _discoveredPairingUtilities; }
        }

        public static void SearchForPairingUtility()
        {
            if (_pairingUtilityBrowser != null)
                return;

            _log.Info("Searching for pairing utility...");

            _pairingUtilityBrowser = new NetServiceBrowser();
            HookEvents();

            // Begin searching for services
            if (NetworkManager.IsLocalNetworkAvailable)
                _pairingUtilityBrowser.SearchForServices(PairingUtilityServiceName);
        }

        public static void StopSearchingForPairingUtility()
        {
            if (_pairingUtilityBrowser == null)
                return;

            _log.Info("Stopping search for pairing utility...");

            UnhookEvents();
            _pairingUtilityBrowser.Stop();

            ThreadUtility.RunOnUIThread(() =>
            {
                DiscoveredPairingUtilities.Clear();
            });

            _pairingUtilityBrowser = null;
        }

        private static void NetworkManager_NetworkAvailabilityChanged(object sender, NetworkAvailabilityChangedEventArgs e)
        {
            if (_pairingUtilityBrowser == null)
                return;

            if (e.IsLocalNetworkAvailable)
            {
                if (!_pairingUtilityBrowser.IsRunning)
                    _pairingUtilityBrowser.SearchForServices(PairingUtilityServiceName);
            }
            else
            {
                _pairingUtilityBrowser.Stop();
                ThreadUtility.RunOnUIThread(() =>
                {
                    DiscoveredPairingUtilities.Clear();
                });
            }
        }

        private static async void Browser_ServiceFound(object sender, NetServiceEventArgs e)
        {
            await e.Service.ResolveAsync().ConfigureAwait(false);

            ThreadUtility.RunOnUIThread(() =>
            {
                if (DiscoveredPairingUtilities.Any(u => u.Service == e.Service))
                    return;

                var utility = new DiscoveredPairingUtility(e.Service);
                DiscoveredPairingUtilities.Add(utility);
                _log.Info("Added service {0} ({1})", utility.ServiceID, utility.Name);
            });
        }

        private static void Browser_ServiceRemoved(object sender, NetServiceEventArgs e)
        {
            ThreadUtility.RunOnUIThread(() =>
            {
                var utility = DiscoveredPairingUtilities.FirstOrDefault(u => u.Service == e.Service);
                if (utility != null)
                {
                    DiscoveredPairingUtilities.Remove(utility);
                    _log.Info("Removed service {0} ({1})", utility.ServiceID, utility.Name);
                }
            });
        }

        #region Phone Events

        private static void HookEvents()
        {
            NetworkManager.NetworkAvailabilityChanged += NetworkManager_NetworkAvailabilityChanged;
            _pairingUtilityBrowser.ServiceFound += Browser_ServiceFound;
            _pairingUtilityBrowser.ServiceRemoved += Browser_ServiceRemoved;
        }

        private static void UnhookEvents()
        {
            NetworkManager.NetworkAvailabilityChanged -= NetworkManager_NetworkAvailabilityChanged;
            _pairingUtilityBrowser.ServiceFound -= Browser_ServiceFound;
            _pairingUtilityBrowser.ServiceRemoved -= Browser_ServiceRemoved;
        }

        #endregion
    }
}
