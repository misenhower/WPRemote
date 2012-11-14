using Komodex.Bonjour.DNS;
using Komodex.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
        private const int RepeatedRebroadcastInterval = 30000;

        // Running
        public bool IsRunning { get; protected set; }

        // Service search parameters
        private string _currentServiceType;
        private Message _currentServiceSearchMessage;
        private DateTime _lastServiceBroadcast;

        // Discovered services list
        Dictionary<string, NetService> _discoveredServices = new Dictionary<string, NetService>();

        // Known IP addresses
        // TODO: Make this static
#if WINDOWS_PHONE
        Dictionary<string, List<IPAddress>> _discoveredIPs = new Dictionary<string, List<IPAddress>>();
#else
        Dictionary<string, List<Windows.Networking.HostName>> _discoveredIPs = new Dictionary<string, List<Windows.Networking.HostName>>();
#endif

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

            IsRunning = true;

            // Listen for MDNS messages and notifications
            MulticastDNSChannel.AddListener(this);

            // The message will be sent when we receive a joined notification from the MDNS channel (which could be immediately)
        }

        public void Stop()
        {
            IsRunning = false;

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
            if (_currentServiceSearchMessage == null)
                return;
            SendServiceSearchMessage();

            // Rebroadcasts
            ThreadUtility.RunOnBackgroundThread(() =>
            {
                ThreadUtility.Delay(FirstRebroadcastInterval);
                if (!IsRunning)
                    return;

                SendServiceSearchMessage();

                ThreadUtility.Delay(SecondRebroadcastInterval);
                if (!IsRunning)
                    return;

                SendServiceSearchMessage();
            });

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
            {
                _log.Debug("Sending search message for \"{0}\"...", _currentServiceType);
                MulticastDNSChannel.SendMessage(_currentServiceSearchMessage);
                _lastServiceBroadcast = DateTime.Now;
            }
        }

        private void ProcessMessage(Message message)
        {
            NetService[] existingServices;
            lock (_discoveredServices)
                existingServices = _discoveredServices.Values.ToArray();
            List<NetService> updatedServices = new List<NetService>();

            // Get a list of all records in the message
            var records = message.AnswerRecords.Concat(message.AdditionalRecords);

            // Sort the list so PTR records appear first
            records = records.OrderBy(r => (r.Type == ResourceRecordType.PTR) ? 0 : 1);

            foreach (var record in records)
            {
                NetService service;
                switch (record.Type)
                {
                    case ResourceRecordType.PTR:
                        service = ProcessPTRRecord(record);
                        if (service != null)
                            updatedServices.AddOnce(service);
                        break;
                    case ResourceRecordType.A:
                        ProcessARecord(record);
                        break;
                    case ResourceRecordType.SRV:
                        service = ProcessSRVRecord(record);
                        if (service != null)
                            updatedServices.AddOnce(service);
                        break;
                    case ResourceRecordType.TXT:
                        service = ProcessTXTRecord(record);
                        if (service != null)
                            updatedServices.AddOnce(service);
                        break;
                    default:
                        break;
                }
            }

            foreach (var service in updatedServices)
            {
                if (!existingServices.Contains(service))
                    FoundService(service);
                else
                {
                    // If the message contained a SRV response in the answer section (rather than the additional records section),
                    // this is a response to a resolve request.
                    if (message.AnswerRecords.Any(rr => rr.Type == ResourceRecordType.SRV))
                        service.Resolved();
                }
            }

        }

        private NetService ProcessPTRRecord(ResourceRecord record)
        {
            if (record.Name != _currentServiceType)
                return null;

            NetService service = null;
            string serviceInstanceName = (string)record.Data;

            // TTL of zero means we need to remove the service from the list
            if (record.TimeToLive == TimeSpan.Zero)
            {
                RemoveService(serviceInstanceName);
                return null;
            }

            if (_discoveredServices.ContainsKey(serviceInstanceName))
                service = _discoveredServices[serviceInstanceName];
            else
            {
                service = new NetService(this);
                service.FullServiceInstanceName = serviceInstanceName;

                lock (_discoveredServices)
                    _discoveredServices[serviceInstanceName] = service;
            }

            service.TTLExpires = DateTime.Now + record.TimeToLive;

            return service;
        }

        private void ProcessARecord(ResourceRecord record)
        {
            string hostname = record.Name;
#if WINDOWS_PHONE
            IPAddress ip = (IPAddress)record.Data;
#else
            Windows.Networking.HostName ip = (Windows.Networking.HostName)record.Data;
#endif

            if (!_discoveredIPs.ContainsKey(hostname))
#if WINDOWS_PHONE
                _discoveredIPs.Add(hostname, new List<IPAddress>());
#else
                _discoveredIPs.Add(hostname, new List<Windows.Networking.HostName>());
#endif
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

        private NetService ProcessSRVRecord(ResourceRecord record)
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
                return service;
            }

            return null;
        }

        private NetService ProcessTXTRecord(ResourceRecord record)
        {
            string serviceInstanceName = record.Name;

            if (_discoveredServices.ContainsKey(serviceInstanceName))
            {
                NetService service = _discoveredServices[serviceInstanceName];
                service.TXTRecordData = (Dictionary<string, string>)record.Data;
                return service;
            }

            return null;
        }

        #endregion

        #region Service Notification

        public event EventHandler<NetServiceEventArgs> ServiceFound;
        public event EventHandler<NetServiceEventArgs> ServiceRemoved;

        private void FoundService(NetService service)
        {
            _log.Info("Found service \"{0}\"", service.FullServiceInstanceName);
            ServiceFound.Raise(this, new NetServiceEventArgs(service));
        }

        internal void RemoveService(string serviceInstanceName)
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

#if WINDOWS_PHONE
        private Timer _runLoopTimer;
#else
        private Windows.System.Threading.ThreadPoolTimer _runLoopTimer;
#endif

        private void StartRunLoop()
        {
            if (_runLoopTimer != null)
                return;

#if WINDOWS_PHONE
            _runLoopTimer = new Timer((state) => RunLoop(), null, RunLoopInterval, RunLoopInterval);
#else
            _runLoopTimer = Windows.System.Threading.ThreadPoolTimer.CreatePeriodicTimer((timer) => RunLoop(), TimeSpan.FromMilliseconds(RunLoopInterval));
#endif
        }

        private void StopRunLoop()
        {
            if (_runLoopTimer == null)
                return;

#if WINDOWS_PHONE
            _runLoopTimer.Dispose();
#else
            _runLoopTimer.Cancel();
#endif
            _runLoopTimer = null;
        }

        private void RunLoop()
        {
            if (!IsRunning)
                return;

            // TODO: Check TTLs, etc.

            // Check if we need to rebroadcast the search message
            if (_lastServiceBroadcast.AddMilliseconds(RepeatedRebroadcastInterval) < DateTime.Now)
                SendServiceSearchMessage();
        }

        #endregion
    }
}
