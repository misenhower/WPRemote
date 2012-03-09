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
using Komodex.Common;
using System.Threading;

namespace Komodex.Bonjour
{
    public class NetServiceBrowser
    {
        // Run loop time interval (ms)
        private const int RunLoopFirstInterval = 1000;
        private const int RunLoopRecurringInterval = 2000;

        // Multicast DNS Channel for service discovery and resolution
        private MulticastDNSChannel _channel;

        // Service search parameters
        private string _currentServiceName;
        private Message _currentServiceSearchMessage;

        // Discovered services list
        Dictionary<string, NetService> _discoveredServices = new Dictionary<string, NetService>();

        // Known IP addresses
        Dictionary<string, List<IPAddress>> _discoveredIPs = new Dictionary<string, List<IPAddress>>();

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

            StartRunLoop();
        }

        public void Stop()
        {
            StopRunLoop();

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
            if (record.Name != _currentServiceName)
                return;

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

            if (!_discoveredIPs.ContainsKey(hostname))
                _discoveredIPs.Add(hostname, new List<IPAddress>());
            if (!_discoveredIPs[hostname].Contains(ip))
                _discoveredIPs[hostname].Insert(0, ip);

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
                service.IPAddresses.Clear();
                if (_discoveredIPs.ContainsKey(service.Hostname))
                    service.IPAddresses.AddRange(_discoveredIPs[service.Hostname]);
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

        #region Service Notification

        public event EventHandler<NetServiceEventArgs> ServiceFound;
        public event EventHandler<NetServiceEventArgs> ServiceRemoved;

        private List<NetService> _notifiedServices = new List<NetService>();

        private void NotifyServices()
        {
            // Determine which services have been removed
            var removedServices = _notifiedServices.Where(s => !_discoveredServices.ContainsValue(s)).ToArray();

            // Determine which services have been added
            var addedServices = _discoveredServices.Values.Where(s => !_notifiedServices.Contains(s)).ToArray();

            // Send removed notifications
            foreach (var service in removedServices)
            {
                ServiceRemoved.Raise(this, new NetServiceEventArgs(service));
                _notifiedServices.Remove(service);
            }

            // Send added notifications
            foreach (var service in addedServices)
            {
                ServiceFound.Raise(this, new NetServiceEventArgs(service));
                _notifiedServices.Add(service);
            }
        }

        #endregion

        #region Run Loop

        private Timer _runLoopTimer;

        private void StartRunLoop()
        {
            if (_runLoopTimer != null)
                return;

            _runLoopTimer = new Timer(RunLoop, null, RunLoopFirstInterval, RunLoopRecurringInterval);
        }

        private void StopRunLoop()
        {
            if (_runLoopTimer == null)
                return;

            _runLoopTimer.Dispose();
            _runLoopTimer = null;
        }

        private void RunLoop(object state)
        {
            // TODO: Check TTLs, etc.

            NotifyServices();
        }

        #endregion

    }
}
