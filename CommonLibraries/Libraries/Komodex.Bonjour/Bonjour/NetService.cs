﻿using System;
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
using System.Collections.Generic;
using System.Linq;
using Komodex.Common;
using System.Threading;

namespace Komodex.Bonjour
{
    public class NetService : IMulticastDNSListener
    {
        // Service resolve timeout (ms)
        private const int ServiceResolveTimeout = 250;

        // Rebroadcast times (ms)
        private const int FirstRebroadcastInterval = 1000;
        private const int SecondRebroadcastInterval = 2000;

        private static readonly Log.LogInstance _log = Log.GetInstance("Bonjour Service");

        public NetService()
        { }

        internal NetService(NetServiceBrowser browser)
        {
            _browser = browser;
        }

        #region Fields

        private string _fullServiceInstanceName, _name, _type, _domain;

        protected NetServiceBrowser _browser;

        private List<IPAddress> _ipAddresses = new List<IPAddress>();

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
        public List<IPAddress> IPAddresses { get { return _ipAddresses; } }

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

            _publishing = true;
            MulticastDNSChannel.AddListener(this);
        }

        public void Stop()
        {
            if (!_publishing)
                return;

            _publishing = false;
            AnnounceServiceStopPublishing();
        }

        private void AnnounceServicePublish()
        {
            if (!_publishing)
                return;

            Message message = GetPublishMessage();

            if (!MulticastDNSChannel.IsJoined)
                return;

            MulticastDNSChannel.SendMessage(message);

            Thread t = new Thread(() =>
            {
                Thread.Sleep(FirstRebroadcastInterval);
                if (!MulticastDNSChannel.IsJoined || !_publishing)
                {
                    Stop();
                    return;
                }

                MulticastDNSChannel.SendMessage(message);

                Thread.Sleep(SecondRebroadcastInterval);
                if (!MulticastDNSChannel.IsJoined || !_publishing)
                {
                    Stop();
                    return;
                }

                MulticastDNSChannel.SendMessage(message);
            });
            t.Start();
        }

        private void AnnounceServiceStopPublishing()
        {
            Message message = GetStopPublishMessage();

            if (!MulticastDNSChannel.IsJoined)
                return;

            MulticastDNSChannel.SendMessage(message);

            Thread t = new Thread(() =>
            {
                Thread.Sleep(FirstRebroadcastInterval);
                if (!MulticastDNSChannel.IsJoined || _publishing)
                    return;

                MulticastDNSChannel.SendMessage(message);

                Thread.Sleep(SecondRebroadcastInterval);
                if (!MulticastDNSChannel.IsJoined || _publishing)
                    return;

                MulticastDNSChannel.SendMessage(message);

                MulticastDNSChannel.RemoveListener(this);
            });
            t.Start();
        }

        #region Start/Stop DNS Messages

        private Message GetPublishMessage()
        {
            Message message = new Message();
            ResourceRecord record;

            // This message is a response
            message.QueryResponse = true;

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

        #endregion

        #region Resolve

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
            Message message = GetResolveMessage();
            _log.Info("Resolving service \"{0}\"...", FullServiceInstanceName);
            MulticastDNSChannel.SendMessage(message);
        }

        internal void Resolved()
        {
            if (_browser == null || !_browser.IsRunning)
                return;

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
        }

        void IMulticastDNSListener.MulticastDNSMessageReceived(Message message)
        {
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
