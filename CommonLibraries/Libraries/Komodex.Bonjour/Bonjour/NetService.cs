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
using System.Collections.Generic;
using System.Linq;

namespace Komodex.Bonjour
{
    public class NetService
    {
        public NetService()
        { }

        internal NetService(NetServiceBrowser browser)
        {
            _browser = browser;
        }

        #region Fields

        private string _fullServerInstanceName, _name, _type, _domain;

        protected NetServiceBrowser _browser;

        private List<IPAddress> _ipAddresses = new List<IPAddress>();

        #endregion

        #region Properties

        // TODO: Rename this to FullServiceInstanceName
        /// <summary>
        /// Gets or sets the full server instance name, e.g., "17B30BC453C4B6A0._touch-able._tcp.local."
        /// </summary>
        public string FullServerInstanceName
        {
            get { return _fullServerInstanceName; }
            set
            {
                if (_fullServerInstanceName == value)
                    return;

                if (string.IsNullOrEmpty(value))
                {
                    _fullServerInstanceName = null;
                    _name = null;
                    _type = null;
                    _domain = null;
                    return;
                }

                if (!value.EndsWith("."))
                    value += ".";

                _fullServerInstanceName = value;
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

        #region Broadcasting

        private static readonly TimeSpan BroadcastTTL = TimeSpan.FromMinutes(1);

        public void Publish()
        {
            throw new NotImplementedException();
        }

        private Message GetServicePublishMessage()
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
            record.Data = FullServerInstanceName;
            message.AnswerRecords.Add(record);

            // SRV Record
            record = new ResourceRecord();
            record.Type = ResourceRecordType.SRV;
            record.TimeToLive = BroadcastTTL;
            record.Name = FullServerInstanceName;
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
                record.Name = FullServerInstanceName;
                record.Data = TXTRecordData;
                message.AnswerRecords.Add(record);
            }

            return message;
        }

        private Message GetServiceCloseMessage()
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
            record.Data = FullServerInstanceName;
            message.AnswerRecords.Add(record);

            return message;
        }

        #endregion

        #region Other Methods

        public override string ToString()
        {
            string ips = string.Join(", ", IPAddresses);
            return string.Format("NetService: {0}\r\nHostname: {1}\r\nIP Addresses: {2}\r\nPort: {3}", FullServerInstanceName, Hostname, ips, Port);
        }

        #endregion
    }
}
