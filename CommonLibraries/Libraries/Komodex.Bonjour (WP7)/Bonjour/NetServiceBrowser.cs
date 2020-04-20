using Komodex.Bonjour.DNS;
using Komodex.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

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
        Dictionary<string, List<string>> _discoveredIPs = new Dictionary<string, List<string>>();

        // Logger instance
        private static readonly Log _log = new Log("Bonjour NSBrowser");

        #region Public Methods

        /// <summary>
        /// Searches for services on the local network.
        /// </summary>
        /// <param name="type">The type of the service to search for, e.g., "_touch-able._tcp.local."</param>
        public async void SearchForServices(string type)
        {
            _discoveredServices.Clear();

            _currentServiceType = BonjourUtility.FormatLocalHostname(type);

            _log.Info("Searching for service type '{0}'...", _currentServiceType);

            // Create the DNS message to send
            _currentServiceSearchMessage = new Message();
            _currentServiceSearchMessage.Questions.Add(new Question(_currentServiceType, ResourceRecordType.PTR));

            IsRunning = true;

            // Listen for MDNS messages and notifications
            await MulticastDNSChannel.AddListenerAsync(this).ConfigureAwait(false);

            await SendServiceSearchMessageAsync().ConfigureAwait(false);
            await TaskUtility.Delay(FirstRebroadcastInterval).ConfigureAwait(false);
            if (!IsRunning)
                return;
            await SendServiceSearchMessageAsync().ConfigureAwait(false);
            await TaskUtility.Delay(SecondRebroadcastInterval).ConfigureAwait(false);
            if (!IsRunning)
                return;
            await SendServiceSearchMessageAsync().ConfigureAwait(false);

            StartRunLoop();
        }

        public void Stop()
        {
            IsRunning = false;

            MulticastDNSChannel.RemoveListener(this);
            _discoveredServices.Clear();
            _discoveredIPs.Clear();
            _currentServiceSearchMessage = null;

            _log.Info("Stopped search for service type '{0}'.", _currentServiceType);
            _currentServiceType = null;
        }

        #endregion

        #region IMulticastDNSListener Members

        void IMulticastDNSListener.MulticastDNSMessageReceived(Message message)
        {
            // Make sure this is a response
            if (!message.QueryResponse)
                return;

            ProcessMessage(message);
        }

        #endregion

        #region Message Processing

        private async Task SendServiceSearchMessageAsync()
        {
            if (!MulticastDNSChannel.IsJoined || _currentServiceSearchMessage == null)
                return;

            _log.Debug("Sending search message for '{0}'...", _currentServiceType);
            await MulticastDNSChannel.SendMessageAsync(_currentServiceSearchMessage).ConfigureAwait(false);
            _lastServiceBroadcast = DateTime.Now;
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
                        service = ProcessPTRRecord((PTRRecord)record);
                        if (service != null)
                            updatedServices.AddOnce(service);
                        break;
                    case ResourceRecordType.A:
                        ProcessARecord((ARecord)record);
                        break;
                    case ResourceRecordType.SRV:
                        service = ProcessSRVRecord((SRVRecord)record);
                        if (service != null)
                            updatedServices.AddOnce(service);
                        break;
                    case ResourceRecordType.TXT:
                        service = ProcessTXTRecord((TXTRecord)record);
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

        private NetService ProcessPTRRecord(PTRRecord record)
        {
            if (record.Name != _currentServiceType)
                return null;

            NetService service = null;
            string serviceInstanceName = record.DomainName;

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

        private void ProcessARecord(ARecord record)
        {
            string hostname = record.Name;
            string ip = record.Address;

            if (!_discoveredIPs.ContainsKey(hostname))
                _discoveredIPs.Add(hostname, new List<string>());
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

        private NetService ProcessSRVRecord(SRVRecord record)
        {
            string serviceInstanceName = record.Name;

            if (_discoveredServices.ContainsKey(serviceInstanceName))
            {
                NetService service = _discoveredServices[serviceInstanceName];
                service.Hostname = record.Target;
                service.Port = record.Port;
                service.IPAddresses.Clear();
                if (_discoveredIPs.ContainsKey(service.Hostname))
                    service.IPAddresses.AddRange(_discoveredIPs[service.Hostname]);
                return service;
            }

            return null;
        }

        private NetService ProcessTXTRecord(TXTRecord record)
        {
            string serviceInstanceName = record.Name;

            if (_discoveredServices.ContainsKey(serviceInstanceName))
            {
                NetService service = _discoveredServices[serviceInstanceName];
                service.TXTRecordData = record.Data;
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

        private CancellationTokenSource _runLoopCancellationTokenSource;

        private async void StartRunLoop()
        {
            var cts = _runLoopCancellationTokenSource;
            if (cts != null)
                cts.Cancel();

            cts = new CancellationTokenSource();
            _runLoopCancellationTokenSource = cts;
            var token = cts.Token;

            // Initial delay
            await TaskUtility.Delay(RunLoopInterval).ConfigureAwait(false);

            while (IsRunning && !token.IsCancellationRequested)
            {
                if (_lastServiceBroadcast.AddMilliseconds(RepeatedRebroadcastInterval) < DateTime.Now)
                    await SendServiceSearchMessageAsync().ConfigureAwait(false);

                await TaskUtility.Delay(RunLoopInterval).ConfigureAwait(false);
            }
        }

        #endregion
    }
}
