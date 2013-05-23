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

            Utility.BeginInvokeOnUIThread(() =>
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
                Utility.BeginInvokeOnUIThread(() =>
                {
                    DiscoveredPairingUtilities.Clear();
                });
            }
        }

        private static void Browser_ServiceFound(object sender, NetServiceEventArgs e)
        {
            _log.Info("Found instance '{0}'", e.Service.FullServiceInstanceName);

            e.Service.ServiceResolved += Service_ServiceResolved;
            e.Service.Resolve();
        }

        private static void Service_ServiceResolved(object sender, NetServiceEventArgs e)
        {
            _log.Info("Resolved instance '{0}'", e.Service.FullServiceInstanceName);

            Utility.BeginInvokeOnUIThread(() =>
            {
                if (DiscoveredPairingUtilities.Any(u => u.Service == e.Service))
                    return;

                var utility = new DiscoveredPairingUtility(e.Service);
                DiscoveredPairingUtilities.Add(utility);
            });
        }

        private static void Browser_ServiceRemoved(object sender, NetServiceEventArgs e)
        {
            _log.Info("Removed instance '{0}'", e.Service.FullServiceInstanceName);

            Utility.BeginInvokeOnUIThread(() =>
            {
                var utility = DiscoveredPairingUtilities.FirstOrDefault(u => u.Service == e.Service);
                if (utility != null)
                    DiscoveredPairingUtilities.Remove(utility);
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
