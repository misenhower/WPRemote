using Komodex.Bonjour;
using Komodex.Common;
using Komodex.Remote.ServerManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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

        public async void SendPairedNotification(string pairingCode)
        {
            if (Service.IPAddresses == null || Service.IPAddresses.Count < 1)
                return;

            string ip = Service.IPAddresses[0].ToString();
            string uri = string.Format("http://{0}:{1}/pairingcomplete?pairingcode={2}", ip, Service.Port, pairingCode);

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    var response = await client.GetStringAsync(uri);
                }
            }
            catch { }
        }
    }
}
