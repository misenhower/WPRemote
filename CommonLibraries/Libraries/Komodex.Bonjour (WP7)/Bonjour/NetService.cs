using Komodex.Bonjour.DNS;
using Komodex.Common;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;

namespace Komodex.Bonjour
{
    public class NetService : IMulticastDNSListener
    {
        // Service resolve timeout (ms)
        private const int ServiceResolveTimeout = 250;

        // Run loop time interval (ms)
        private const int RunLoopInterval = 2000;

        // Rebroadcast times (ms)
        private const int FirstRebroadcastInterval = 1000;
        private const int SecondRebroadcastInterval = 2000;
        private const int RepeatedRebroadcastInterval = 20000;

        // Minimum time between (prompted) rebroadcasts (ms)
        private const int MinimumRebroadcastTime = 2000;

        private static readonly Log _log = new Log("Bonjour Service");

        public NetService()
        { }

        internal NetService(NetServiceBrowser browser)
        {
            _browser = browser;
        }

        #region Fields

        private string _fullServiceInstanceName, _name, _type, _domain;
        private Message _currentServicePublishMessage;
        private DateTime _lastServicePublishBroadcast;

        protected NetServiceBrowser _browser;

#if WINDOWS_PHONE
        private List<IPAddress> _ipAddresses = new List<IPAddress>();
#else
        private List<Windows.Networking.HostName> _ipAddresses = new List<Windows.Networking.HostName>();
#endif

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the full server instance name, e.g., "17B30BC453C4B6A0._touch-able._tcp.local."
        /// </summary>
        public string FullServiceInstanceName
        {
            get { return _fullServiceInstanceName; }
            set
            {
                if (_fullServiceInstanceName == value)
                    return;

                if (string.IsNullOrEmpty(value))
                {
                    _fullServiceInstanceName = null;
                    _name = null;
                    _type = null;
                    _domain = null;
                    return;
                }

                if (!value.EndsWith("."))
                    value += ".";

                _fullServiceInstanceName = value;
                BonjourUtility.ParseServiceInstanceName(value, out _name, out _type, out _domain);
            }
        }

        /// <summary>
        /// Gets the name of the service, e.g., "17B30BC453C4B6A0"
        /// </summary>
        public string Name { get { return _name; } }

        /// <summary>
        /// Gets the service type, e.g., "_touch-able._tcp."
        /// </summary>
        public string Type { get { return _type; } }

        /// <summary>
        /// Gets the service domain, e.g., "local."
        /// </summary>
        public string Domain { get { return _domain; } }

        /// <summary>
        /// Gets the resolved hostname, e.g., "ike-mbp.local."
        /// </summary>
        public string Hostname { get; set; }

        /// <summary>
        /// Gets a list of all IP addresses for this service.
        /// </summary>
#if WINDOWS_PHONE
        public List<IPAddress> IPAddresses { get { return _ipAddresses; } }
#else
        public List<Windows.Networking.HostName> IPAddresses { get { return _ipAddresses; } }
#endif

        /// <summary>
        /// Gets or sets the port this service is listening on.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Gets or sets TXT Record data for this service.
        /// </summary>
        public Dictionary<string, string> TXTRecordData { get; set; }

        internal DateTime TTLExpires { get; set; }

        #endregion

        #region Publish

        private bool _publishing;

        private static readonly TimeSpan BroadcastTTL = TimeSpan.FromMinutes(2);

        public void Publish()
        {
            if (_browser != null)
                throw new InvalidOperationException("Cannot publish services that were discovered by NetServiceBrowser.");

            _log.Info("Publishing service \"{0}\"...", FullServiceInstanceName);

            _publishing = true;
            _currentServicePublishMessage = GetPublishMessage();
            MulticastDNSChannel.AddListener(this);
        }

        public void Stop()
        {
            if (!_publishing)
                return;

            _publishing = false;

            StopRunLoop();

            _currentServicePublishMessage = null;
            AnnounceServiceStopPublishing();

            _log.Info("Stopped publishing service \"{0}\".", FullServiceInstanceName);
        }

        private void AnnounceServicePublish()
        {
            if (!_publishing)
                return;

            if (!MulticastDNSChannel.IsJoined)
                return;

            SendServicePublishMessage();

            ThreadUtility.RunOnBackgroundThread(() =>
            {
                ThreadUtility.Delay(FirstRebroadcastInterval);
                if (!MulticastDNSChannel.IsJoined || !_publishing)
                {
                    Stop();
                    return;
                }

                SendServicePublishMessage();

                ThreadUtility.Delay(SecondRebroadcastInterval);
                if (!MulticastDNSChannel.IsJoined || !_publishing)
                {
                    Stop();
                    return;
                }

                SendServicePublishMessage();
            });
        }

        private void SendServicePublishMessage()
        {
            if (!_publishing || _currentServicePublishMessage == null)
                return;

            _log.Debug("Sending publish message for service \"{0}\"...", FullServiceInstanceName);
            MulticastDNSChannel.SendMessage(_currentServicePublishMessage);
            _lastServicePublishBroadcast = DateTime.Now;
        }

        private void AnnounceServiceStopPublishing()
        {
            Message message = GetStopPublishMessage();

            if (!MulticastDNSChannel.IsJoined)
                return;

            MulticastDNSChannel.SendMessage(message);

            ThreadUtility.RunOnBackgroundThread(() =>
            {
                ThreadUtility.Delay(FirstRebroadcastInterval);
                if (!MulticastDNSChannel.IsJoined || _publishing)
                    return;

                MulticastDNSChannel.SendMessage(message);

                ThreadUtility.Delay(SecondRebroadcastInterval);
                if (!MulticastDNSChannel.IsJoined || _publishing)
                    return;

                MulticastDNSChannel.SendMessage(message);

                MulticastDNSChannel.RemoveListener(this);
            });
        }

        #region Start/Stop DNS Messages

        private Message GetPublishMessage()
        {
            Message message = new Message();
            ResourceRecord record;

            // This message is a response
            message.QueryResponse = true;

            // PTR Record for DNS-SD Service Type Enumeration
            record = new ResourceRecord();
            record.Type = ResourceRecordType.PTR;
            record.TimeToLive = BroadcastTTL;
            record.Name = BonjourUtility.DNSSDServiceTypeEnumerationName;
            record.Data = Type;
            message.AnswerRecords.Add(record);

            // PTR Record
            record = new ResourceRecord();
            record.Type = ResourceRecordType.PTR;
            record.TimeToLive = BroadcastTTL;
            record.Name = Type;
            record.Data = FullServiceInstanceName;
            message.AnswerRecords.Add(record);

            // SRV Record
            record = new ResourceRecord();
            record.Type = ResourceRecordType.SRV;
            record.TimeToLive = BroadcastTTL;
            record.Name = FullServiceInstanceName;
            SRVRecordData srv = new SRVRecordData();
            srv.Target = Hostname;
            srv.Port = Port;
            record.Data = srv;
            message.AnswerRecords.Add(record);

            // A Records
            foreach (var ip in IPAddresses)
            {
                record = new ResourceRecord();
                record.Type = ResourceRecordType.A;
                record.TimeToLive = BroadcastTTL;
                record.Name = Hostname;
                record.Data = ip;
                message.AnswerRecords.Add(record);
            }

            // TXT Record
            if (TXTRecordData != null && TXTRecordData.Count > 0)
            {
                record = new ResourceRecord();
                record.Type = ResourceRecordType.TXT;
                record.TimeToLive = BroadcastTTL;
                record.Name = FullServiceInstanceName;
                record.Data = TXTRecordData;
                message.AnswerRecords.Add(record);
            }

            return message;
        }

        private Message GetStopPublishMessage()
        {
            Message message = new Message();
            ResourceRecord record;

            // This message is a response
            message.QueryResponse = true;

            // PTR Record
            record = new ResourceRecord();
            record.Type = ResourceRecordType.PTR;
            record.TimeToLive = TimeSpan.Zero;
            record.Name = Type;
            record.Data = FullServiceInstanceName;
            message.AnswerRecords.Add(record);

            return message;
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
            if (!_publishing)
                return;

            // Check if we need to rebroadcast the search message
            if (_lastServicePublishBroadcast.AddMilliseconds(RepeatedRebroadcastInterval) < DateTime.Now)
                SendServicePublishMessage();
        }

        #endregion

        #endregion

        #region Resolve

        private bool _resolving;

        /// <summary>
        /// Raised when the service has been resolved. This event may be raised multiple times as new information becomes available.
        /// </summary>
        public event EventHandler<NetServiceEventArgs> ServiceResolved;

        public void Resolve()
        {
            if (_browser == null)
                throw new InvalidOperationException("This operation is only valid on services that were generated by a NetServiceBrowser.");

            // Making sure that the associated NetServiceBrowser is still active ensures that the MulticastDNSChannel has not been shut down.
            if (!_browser.IsRunning)
                throw new InvalidOperationException("The associated NetServiceBrowser has been stopped.");

            // Resolve the service
            _resolving = true;
            Message message = GetResolveMessage();
            _log.Info("Resolving service \"{0}\"...", FullServiceInstanceName);
            MulticastDNSChannel.SendMessage(message);

            ThreadUtility.RunOnBackgroundThread(()=>
            {
                ThreadUtility.Delay(FirstRebroadcastInterval);
                if (!MulticastDNSChannel.IsJoined || !_resolving)
                    return;

                MulticastDNSChannel.SendMessage(message);

                ThreadUtility.Delay(SecondRebroadcastInterval);
                if (!MulticastDNSChannel.IsJoined || !_resolving)
                    return;

                MulticastDNSChannel.SendMessage(message);

            });
        }

        internal void Resolved()
        {
            if (_browser == null || !_browser.IsRunning)
                return;

            _resolving = false;

            _log.Info("Resolved service \"{0}\".", FullServiceInstanceName);
            ServiceResolved.Raise(this, new NetServiceEventArgs(this));
        }

        private Message GetResolveMessage()
        {
            Message message = new Message();

            // SRV query
            Question question = new Question();
            question.Name = FullServiceInstanceName;
            question.Type = ResourceRecordType.SRV;
            message.Questions.Add(question);

            return message;
        }

        #endregion

        #region IMulticastDNSListener Members

        void IMulticastDNSListener.MulticastDNSChannelJoined()
        {
            AnnounceServicePublish();

            StartRunLoop();
        }

        void IMulticastDNSListener.MulticastDNSMessageReceived(Message message)
        {
            if (!_publishing)
                return;

            // We only need to respond to queries, not responses
            if (message.QueryResponse)
                return;

            // TODO: Determine all queries we need to respond to

            bool shouldRespond = false;

            foreach (Question question in message.Questions)
            {
                switch (question.Type)
                {
                    case ResourceRecordType.PTR:
                        if (question.Name == Type + Domain)
                            shouldRespond = true;
                        else if (question.Name == BonjourUtility.DNSSDServiceTypeEnumerationName)
                            shouldRespond = true;
                        break;
                }

                // No need to look through the other questions if we already know we need to respond
                if (shouldRespond)
                    break;
            }

            // Send a response if necessary
            if (shouldRespond && _lastServicePublishBroadcast.AddMilliseconds(MinimumRebroadcastTime) < DateTime.Now)
            {
                _log.Debug("Received query for service \"{0}\".", FullServiceInstanceName);
                SendServicePublishMessage();
            }
        }

        #endregion

        #region Summary Strings

        public override string ToString()
        {
            string ips = string.Join(", ", IPAddresses);
            return string.Format("Net service details: {0}\r\nHostname: {1}\r\nIP Addresses: {2}\r\nPort: {3}", FullServiceInstanceName, Hostname, ips, Port);
        }

        #endregion
    }
}
