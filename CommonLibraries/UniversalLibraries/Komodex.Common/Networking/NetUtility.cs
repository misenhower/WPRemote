using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.Networking;
using Windows.Networking.Connectivity;

namespace Komodex.Common.Networking
{
    public static class NetUtility
    {
        private static readonly Log _log = new Log("NetUtility");

        public static IEnumerable<HostName> GetLocalHostNames()
        {
            var hostnames = NetworkInformation.GetHostNames();

            List<HostName> results = new List<HostName>();

            foreach (var host in hostnames)
            {
                if (host.Type != HostNameType.Ipv4)
                {
                    _log.Debug("Skipping non-IPv4 hostname '{0}'...", host.DisplayName);
                    continue;
                }

                // Only use Wi-Fi and Ethernet IPs, and ignore any other types (e.g., cellular)
                uint ianaType = host.IPInformation.NetworkAdapter.IanaInterfaceType;
                // 6: Ethernet
                // 71: Wi-Fi
                if (ianaType != 6 && ianaType != 71)
                {
                    _log.Debug("Hostname '{0}' (IANA interface type: {1}) is not on an Ethernet or Wi-Fi interface, skipping...", host.DisplayName, ianaType);
                    continue;
                }

                _log.Debug("Found local hostname: '{0}' (IANA interface type: {1})", host.DisplayName, ianaType);
                results.Add(host);
            }

            // Usually the IP address we want is at the end of the list
            results.Reverse();

            return results;
        }
    }
}
