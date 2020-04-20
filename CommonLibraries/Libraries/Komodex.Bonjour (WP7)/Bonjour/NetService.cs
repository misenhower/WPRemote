using Komodex.Bonjour.DNS;
using Komodex.Common;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

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

        private List<string> _ipAddresses = new List<string>();

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
        public List<string> IPAddresses { get { return _ipAddresses; } }

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

        public async void Publish()
        {
            if (_browser != null)
                throw new InvalidOperationException("Cannot publish services that were discovered by NetServiceBrowser.");

            _log.Info("Publishing service '{0}'...", FullServiceInstanceName);

            _publishing = true;
            await MulticastDNSChannel.AddListenerAsync(this).ConfigureAwait(false);
            await AnnounceServicePublishAsync().ConfigureAwait(false);
            StartRunLoop();
        }

        public async Task StopPublishingAsync()
        {
            if (!_publishing)
                return;

            _publishing = false;
            _currentServicePublishMessage = null;

            await AnnounceServiceStopPublishingAsync().ConfigureAwait(false);
            MulticastDNSChannel.RemoveListener(this);
            _log.Info("Stopped publishing service '{0}'.", FullServiceInstanceName);
        }

        public async void StopPublishing()
        {
            await StopPublishingAsync().ConfigureAwait(false);
        }

        private async Task AnnounceServicePublishAsync()
        {
            if (!_publishing || !MulticastDNSChannel.IsJoined)
                return;

            var initialMessage = GetPublishMessage(PublishMessageType.Initial);
            _currentServicePublishMessage = GetPublishMessage(PublishMessageType.Normal);

            // Send initial message immediately followed by the "normal" message
            await MulticastDNSChannel.SendMessageAsync(initialMessage).ConfigureAwait(false);
            await SendServicePublishMessageAsync().ConfigureAwait(false);
            await TaskUtility.Delay(FirstRebroadcastInterval).ConfigureAwait(false);

            if (!_publishing || !MulticastDNSChannel.IsJoined)
                return;

            await SendServicePublishMessageAsync().ConfigureAwait(false);
            await TaskUtility.Delay(SecondRebroadcastInterval).ConfigureAwait(false);

            if (!_publishing || !MulticastDNSChannel.IsJoined)
                return;

            await SendServicePublishMessageAsync().ConfigureAwait(false);
        }

        private async Task SendServicePublishMessageAsync()
        {
            if (!_publishing || _currentServicePublishMessage == null)
                return;

            _log.Debug("Sending publish message for service \"{0}\"...", FullServiceInstanceName);
            await MulticastDNSChannel.SendMessageAsync(_currentServicePublishMessage).ConfigureAwait(false);
            _lastServicePublishBroadcast = DateTime.Now;
        }

        private async Task AnnounceServiceStopPublishingAsync()
        {
            Message message = GetPublishMessage(PublishMessageType.Stop);

            if (_publishing || !MulticastDNSChannel.IsJoined)
                return;

            await MulticastDNSChannel.SendMessageAsync(message).ConfigureAwait(false);
            await TaskUtility.Delay(FirstRebroadcastInterval).ConfigureAwait(false);

            if (_publishing || !MulticastDNSChannel.IsJoined)
                return;

            await MulticastDNSChannel.SendMessageAsync(message).ConfigureAwait(false);
            await TaskUtility.Delay(SecondRebroadcastInterval).ConfigureAwait(false);

            if (_publishing || !MulticastDNSChannel.IsJoined)
                return;

            await MulticastDNSChannel.SendMessageAsync(message).ConfigureAwait(false);
        }

        #region Start/Stop DNS Messages

        private enum PublishMessageType
        {
            /// <summary>
            /// Initial message: Includes SRV, TXT, and A records, but not any PTR records.
            /// </summary>
            Initial,

            /// <summary>
            /// Normal (repeated) publish message. Includes all records with standard TTL.
            /// </summary>
            Normal,

            /// <summary>
            /// Stop publishing message. Sets TTL to zero where necessary.
            /// </summary>
            Stop,
        }

        private Message GetPublishMessage(PublishMessageType type)
        {
            Message message = new Message();
            ResourceRecord record;

            // This message is a response
            message.QueryResponse = true;
            // This is an authoritative response
            message.AuthoritativeAnswer = true;

            // SRV Record
            record = new SRVRecord()
            {
                TimeToLive = (type == PublishMessageType.Stop) ? TimeSpan.Zero : BroadcastTTL,
                CacheFlush = true,
                Name = FullServiceInstanceName,
                Target = Hostname,
                Port = Port,
            };
            message.AnswerRecords.Add(record);

            // TXT Record
            if (TXTRecordData != null && TXTRecordData.Count > 0)
            {
                record = new TXTRecord()
                {
                    TimeToLive = (type == PublishMessageType.Stop) ? TimeSpan.Zero : BroadcastTTL,
                    CacheFlush = true,
                    Name = FullServiceInstanceName,
                    Data = TXTRecordData,
                };
                message.AnswerRecords.Add(record);
            }

            // PTR Record for DNS-SD Service Type Enumeration
            if (type == PublishMessageType.Normal)
            {
                record = new PTRRecord()
                {
                    TimeToLive = BroadcastTTL,
                    Name = BonjourUtility.DNSSDServiceTypeEnumerationName,
                    DomainName = Type,
                };
                message.AnswerRecords.Add(record);
            }

            // Service PTR Record
            if (type != PublishMessageType.Initial)
            {
                record = new PTRRecord()
                {
                    TimeToLive = (type == PublishMessageType.Stop) ? TimeSpan.Zero : BroadcastTTL,
                    Name = Type,
                    DomainName = FullServiceInstanceName,
                };
                message.AnswerRecords.Add(record);
            }

            // A Records
            if (type != PublishMessageType.Stop)
            {
                foreach (var ip in IPAddresses)
                {
                    record = new ARecord()
                    {
                        TimeToLive = BroadcastTTL,
                        CacheFlush = true,
                        Name = Hostname,
                        Address = ip,
                    };
                    message.AdditionalRecords.Add(record);
                }
            }

            return message;
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

            while (_publishing && !token.IsCancellationRequested)
            {
                if (_lastServicePublishBroadcast.AddMilliseconds(RepeatedRebroadcastInterval) < DateTime.Now)
                    await SendServicePublishMessageAsync().ConfigureAwait(false);

                await TaskUtility.Delay(RunLoopInterval).ConfigureAwait(false);
            }
        }

        #endregion

        #endregion

        #region Resolve

        private TaskCompletionSource<object> _resolveTaskCompletionSource;

        public async Task<bool> ResolveAsync()
        {
            if (_browser == null)
                throw new InvalidOperationException("This operation is only valid on services that were generated by a NetServiceBrowser.");

            // Making sure that the associated NetServiceBrowser is still active ensures that the MulticastDNSChannel has not been shut down.
            if (!_browser.IsRunning)
                throw new InvalidOperationException("The associated NetServiceBrowser has been stopped.");

            // Resolve the service
            _log.Info("Resolving service '{0}'...", FullServiceInstanceName);
            Message message = GetResolveMessage();

            var tcs = new TaskCompletionSource<object>();
            _resolveTaskCompletionSource = tcs;

            // First resolution attempt
            await MulticastDNSChannel.SendMessageAsync(message).ConfigureAwait(false);

            Task delayTask = TaskUtility.Delay(FirstRebroadcastInterval);
            await TaskUtility.WhenAny(delayTask, tcs.Task).ConfigureAwait(false);

            if (tcs.Task.IsCompleted)
                return true;

            // Second resolution attempt
            await MulticastDNSChannel.SendMessageAsync(message).ConfigureAwait(false);

            delayTask = TaskUtility.Delay(SecondRebroadcastInterval);
            await TaskUtility.WhenAny(delayTask, tcs.Task).ConfigureAwait(false);

            if (tcs.Task.IsCompleted)
                return true;

            // Third resolution attempt
            await MulticastDNSChannel.SendMessageAsync(message).ConfigureAwait(false);

            delayTask = TaskUtility.Delay(ServiceResolveTimeout);
            await TaskUtility.WhenAny(delayTask, tcs.Task).ConfigureAwait(false);

            if (tcs.Task.IsCompleted)
                return true;

            return false;
        }

        internal void Resolved()
        {
            if (_browser == null || !_browser.IsRunning)
                return;

            var tcs = _resolveTaskCompletionSource;
            if (tcs != null)
            {
                _log.Info("Resolved service '{0}'.", FullServiceInstanceName);
                tcs.TrySetResult(null);
                _resolveTaskCompletionSource = null;
            }
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

        async void IMulticastDNSListener.MulticastDNSMessageReceived(Message message)
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
            if (shouldRespond)
            {
                if (_lastServicePublishBroadcast.AddMilliseconds(MinimumRebroadcastTime) < DateTime.Now)
                {
                    _log.Debug("Responding to query for service \"{0}\".", FullServiceInstanceName);
                    await SendServicePublishMessageAsync().ConfigureAwait(false);
                }
                else
                {
                    _log.Debug("Received query for service \"{0}\", but ignoring since we sent a publish message recently.", FullServiceInstanceName);
                }
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
