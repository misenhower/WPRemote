using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Komodex.Bonjour.DNS;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;

namespace Komodex.Bonjour
{
    public class NetServiceBrowser
    {
        // Multicast DNS Channel for service discovery and resolution
        private MulticastDNSChannel _channel;

        // Service search parameters
        private string _currentServiceName;
        private Message _currentServiceSearchMessage;

        // Discovered services list
        Dictionary<string, NetService> _discoveredServices = new Dictionary<string, NetService>();

        // Known IP addresses
        Dictionary<string, IPAddress> _discoveredIPs = new Dictionary<string, IPAddress>();

        #region Public Methods

        public void SearchForServices(string serviceName)
        {
            _discoveredServices.Clear();

            _currentServiceName = BonjourUtility.FormatLocalHostname(serviceName);

            // Create the DNS message to send
            _currentServiceSearchMessage = new Message();
            _currentServiceSearchMessage.Questions.Add(new Question(_currentServiceName, ResourceRecordType.PTR));

            // Create the channel if necessary
            if (_channel == null)
            {
                _channel = new MulticastDNSChannel();
                _channel.Joined += MulticastDNSChannel_Joined;
                _channel.MessageReceived += MulticastDNSChannel_MessageReceived;
                _channel.Start();
            }
            else
            {
                _channel.SendMessage(_currentServiceSearchMessage);
            }
        }

        public void Stop()
        {
            if (_channel != null)
            {
                _channel.Joined -= MulticastDNSChannel_Joined;
                _channel.MessageReceived -= MulticastDNSChannel_MessageReceived;
                _channel.Stop();
                _channel = null;
                _discoveredServices.Clear();
                _discoveredIPs.Clear();
            }
        }

        #endregion

        #region Multicast DNS Channel Events

        private void MulticastDNSChannel_Joined(object sender, EventArgs e)
        {
            if (sender != _channel)
                return;

            if (_currentServiceSearchMessage != null)
                _channel.SendMessage(_currentServiceSearchMessage);
        }

        private void MulticastDNSChannel_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            Message message = e.Message;

            // Make sure this is a response
            if (!message.QueryResponse)
                return;

            // Make sure this has an answer to what we're looking for
            // TODO: remove this?
            if (!message.AnswerRecords.Any(rr => rr.Name == _currentServiceName))
                return;

#if DEBUG
            // Split the message into separate lines to avoid issues with exceeding the maximum length of Debug.WriteLine
            Debug.WriteLine("Message received:");
            var messageLines = message.ToString().Split('\n');
            foreach (string line in messageLines)
                Debug.WriteLine(line.Trim());
            Debug.WriteLine(string.Empty);
#endif

            ProcessMessage(message);
        }

        #endregion

        #region Message Processing

        private void ProcessMessage(Message message)
        {
            List<NetService> changedServices = new List<NetService>();

            var records = message.AnswerRecords.Concat(message.AdditionalRecords);

            foreach (var record in records)
            {
                switch (record.Type)
                {
                    case ResourceRecordType.PTR:
                        ProcessPTRRecord(record);
                        break;
                    case ResourceRecordType.A:
                        ProcessARecord(record);
                        break;
                    case ResourceRecordType.SRV:
                        ProcessSRVRecord(record);
                        break;
                    case ResourceRecordType.TXT:
                        ProcessTXTRecord(record);
                        break;
                    default:
                        break;
                }
            }
        }

        private void ProcessPTRRecord(ResourceRecord record)
        {
            NetService service = null;
            string serverInstanceName = (string)record.Data;

            // TTL of zero means we need to remove the service from the list
            if (record.TimeToLive == TimeSpan.Zero)
            {
                if (_discoveredServices.ContainsKey(serverInstanceName))
                    _discoveredServices.Remove(serverInstanceName);
            }
            else
            {
                if (_discoveredServices.ContainsKey(serverInstanceName))
                    service = _discoveredServices[serverInstanceName];
                else
                {
                    service = new NetService(this);
                    service.FullServerInstanceName = serverInstanceName;

                    _discoveredServices[serverInstanceName] = service;
                }

                service.TTLExpires = DateTime.Now + record.TimeToLive;
            }
        }

        private void ProcessARecord(ResourceRecord record)
        {
            string hostname = record.Name;
            IPAddress ip = (IPAddress)record.Data;

            _discoveredIPs[hostname] = ip;

            // Update existing services
            var services = _discoveredServices.Values.Where(s => s.Hostname == hostname);
            foreach (var service in services)
            {
                if (!service.IPAddresses.Contains(ip))
                    service.IPAddresses.Insert(0, ip);
            }
        }

        private void ProcessSRVRecord(ResourceRecord record)
        {
            string serverInstanceName = record.Name;
            SRVRecordData srv = (SRVRecordData)record.Data;

            if (_discoveredServices.ContainsKey(serverInstanceName))
            {
                NetService service = _discoveredServices[serverInstanceName];
                service.Hostname = srv.Target;
                service.Port = srv.Port;
            }
        }

        private void ProcessTXTRecord(ResourceRecord record)
        {
            string serverInstanceName = record.Name;

            if (_discoveredServices.ContainsKey(serverInstanceName))
            {
                NetService service = _discoveredServices[serverInstanceName];
                service.TXTRecordData = (Dictionary<string, string>)record.Data;
            }
        }

        #endregion

    }
}
