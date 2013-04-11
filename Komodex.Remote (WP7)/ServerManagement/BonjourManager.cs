using Komodex.Bonjour;
using Komodex.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Komodex.Remote.ServerManagement
{
    public static class BonjourManager
    {
        private static Log _log = new Log("Bonjour Manager");

        private static NetServiceBrowser _serverBrowser;

        static BonjourManager()
        {
            _serverBrowser = new NetServiceBrowser();
            _serverBrowser.ServiceFound += ServerBrowser_ServiceFound;
            _serverBrowser.ServiceRemoved += ServerBrowser_ServiceRemoved;
        }

        private static readonly Dictionary<string, NetService> _discoveredServers = new Dictionary<string, NetService>();
        public static Dictionary<string, NetService> DiscoveredServers
        {
            get { return _discoveredServers; }
        }

        public static void Start()
        {
            _serverBrowser.SearchForServices("_touch-able._tcp.local.");
        }

        public static void Stop()
        {
            _serverBrowser.Stop();

            lock (DiscoveredServers)
                DiscoveredServers.Clear();
        }

        private static void ServerBrowser_ServiceFound(object sender, NetServiceEventArgs e)
        {
            e.Service.ServiceResolved += Service_ServiceResolved;
            e.Service.Resolve();
        }

        private static void Service_ServiceResolved(object sender, NetServiceEventArgs e)
        {
            _log.Info("Added service '{0}'.", e.Service.FullServiceInstanceName);
            
            e.Service.ServiceResolved -= Service_ServiceResolved;

            lock (DiscoveredServers)
                DiscoveredServers[e.Service.Name] = e.Service;
        }

        private static void ServerBrowser_ServiceRemoved(object sender, NetServiceEventArgs e)
        {
            _log.Info("Removed service '{0}'.", e.Service.FullServiceInstanceName);

            lock (DiscoveredServers)
            {
                if (DiscoveredServers.ContainsKey(e.Service.Name))
                    DiscoveredServers.Remove(e.Service.Name);
            }
        }
    }
}
