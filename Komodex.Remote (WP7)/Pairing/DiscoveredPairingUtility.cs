using Komodex.Bonjour;
using Komodex.Common;
using Komodex.Remote.ServerManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Komodex.Remote.Pairing
{
    public class DiscoveredPairingUtility
    {
        public DiscoveredPairingUtility(NetService service)
        {
            Service = service;

            Name = service.TXTRecordData.GetValueOrDefault("CtlN", service.Hostname);
            ServiceID = service.TXTRecordData.GetValueOrDefault("ServiceID", service.Name);
        }

        public string Name { get; protected set; }
        public string ServiceID { get; protected set; }
        public NetService Service { get; protected set; }
        public ServerType ServerType { get; protected set; }
    }
}
