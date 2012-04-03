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
    public class NetServiceBrowser : IMulticastDNSListener
    {
        // Run loop time interval (ms)
        private const int RunLoopInterval = 2000;

        // Rebroadcast times (ms)
        private const int FirstRebroadcastInterval = 1000;
        private const int SecondRebroadcastInterval = 3000;

        // Service search parameters
        private string _currentServiceType;
        private Message _currentServiceSearchMessage;

        // Discovered services list
        Dictionary<string, NetService> _discoveredServices = new Dictionary<string, NetService>();

        // Known IP addresses
        // TODO: Make this static
        Dictionary<string, List<IPAddress>> _discoveredIPs = new Dictionary<string, List<IPAddress>>();

        // Logger instance
        private static readonly Log.LogInstance _log = Log.GetInstance("Bonjour NSBrowser");

        #region Public Methods

        /// <summary>
        /// Searches for services on the local network.
        /// </summary>
        /// <param name="type">The type of the service to search for, e.g., "_touch-able._tcp.local."</param>
        public void SearchForServices(string type)
        {
            _discoveredServices.Clear();

            _currentServiceType = BonjourUtility.FormatLocalHostname(type);

            _log.Info("Searching for service type \"{0}\"...", _currentServiceType);

            // Create the DNS message to send
            _currentServiceSearchMessage = new Message();
            _currentServiceSearchMessage.Questions.Add(new Question(_currentServiceType, ResourceRecordType.PTR));

            // Listen for MDNS messages and notifications
            MulticastDNSChannel.AddListener(this);

            // The message will be sent when we receive a joined notification from the MDNS channel (which could be immediately)
        }

        public void Stop()
        {
            StopRunLoop();

            MulticastDNSChannel.RemoveListener(this);
            _discoveredServices.Clear();
            _discoveredIPs.Clear();
            _currentServiceType = null;
            _currentServiceSearchMessage = null;
        }

        #endregion

        #region IMulticastDNSListener Members

        void IMulticastDNSListener.MulticastDNSChannelJoined()
        {
            if (_currentServiceSearchMessage != null)
            {
                SendServiceSearchMessage();

                // Rebroadcasts
                Thread t = new Thread(() =>
                {
                    Thread.Sleep(FirstRebroadcastInterval);
                    if (_currentServiceSearchMessage == null)
                        return;

                    SendServiceSearchMessage();

                    Thread.Sleep(SecondRebroadcastInterval);
                    if (_currentServiceSearchMessage == null)
                        return;

                    SendServiceSearchMessage();
                });

                t.Start();
            }

            StartRunLoop();
        }

        void IMulticastDNSListener.MulticastDNSMessageReceived(Message message)
        {
            // Make sure this is a response
            if (!message.QueryResponse)
                return;

            ProcessMessage(message);
        }

        #endregion

        #region Message Processing

        private void SendServiceSearchMessage()
        {
            if (MulticastDNSChannel.IsJoined && _currentServiceSearchMessage != null)
                MulticastDNSChannel.SendMessage(_currentServiceSearchMessage);
        }

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
            if (record.Name != _currentServiceType)
                return;

            NetService service = null;
            string serviceInstanceName = (string)record.Data;

            // TTL of zero means we need to remove the service from the list
            if (record.TimeToLive == TimeSpan.Zero)
            {
                RemoveService(serviceInstanceName);
                return;
            }

            if (_discoveredServices.ContainsKey(serviceInstanceName))
                service = _discoveredServices[serviceInstanceName];
            else
            {
                service = new NetService(this);
                service.FullServiceInstanceName = serviceInstanceName;
            }

            service.TTLExpires = DateTime.Now + record.TimeToLive;

            AddService(service);
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
            string serviceInstanceName = record.Name;
            SRVRecordData srv = (SRVRecordData)record.Data;

            if (_discoveredServices.ContainsKey(serviceInstanceName))
            {
                NetService service = _discoveredServices[serviceInstanceName];
                service.Hostname = srv.Target;
                service.Port = srv.Port;
                service.IPAddresses.Clear();
                if (_discoveredIPs.ContainsKey(service.Hostname))
                    service.IPAddresses.AddRange(_discoveredIPs[service.Hostname]);
            }
        }

        private void ProcessTXTRecord(ResourceRecord record)
        {
            string serviceInstanceName = record.Name;

            if (_discoveredServices.ContainsKey(serviceInstanceName))
            {
                NetService service = _discoveredServices[serviceInstanceName];
                service.TXTRecordData = (Dictionary<string, string>)record.Data;
            }
        }

        #endregion

        #region Service Notification

        public event EventHandler<NetServiceEventArgs> ServiceFound;
        public event EventHandler<NetServiceEventArgs> ServiceRemoved;

        private void AddService(NetService service)
        {
            lock (_discoveredServices)
            {
                if (_discoveredServices.ContainsKey(service.FullServiceInstanceName))
                    return;

                _discoveredServices.Add(service.FullServiceInstanceName, service);
            }

            _log.Info("Found service \"{0}\"", service.FullServiceInstanceName);
            ServiceFound.Raise(this, new NetServiceEventArgs(service));
        }

        private void RemoveService(string serviceInstanceName)
        {
            NetService service;

            lock (_discoveredServices)
            {
                if (!_discoveredServices.ContainsKey(serviceInstanceName))
                    return;

                service = _discoveredServices[serviceInstanceName];
                _discoveredServices.Remove(serviceInstanceName);
            }

            _log.Info("Removed service \"{0}\"", serviceInstanceName);
            ServiceRemoved.Raise(this, new NetServiceEventArgs(service));
        }

        #endregion

        #region Run Loop

        private Timer _runLoopTimer;

        private void StartRunLoop()
        {
            if (_runLoopTimer != null)
                return;

            _runLoopTimer = new Timer(RunLoop, null, RunLoopInterval, RunLoopInterval);
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
        }

        #endregion

    }
}
