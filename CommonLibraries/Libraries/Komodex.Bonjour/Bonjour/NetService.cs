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

        /// <summary>
        /// The full server instance name, e.g., "17B30BC453C4B6A0._touch-able._tcp.local."
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
        /// The name of the service, e.g., "17B30BC453C4B6A0"
        /// </summary>
        public string Name { get { return _name; } }

        /// <summary>
        /// The service type, e.g., "_touch-able._tcp."
        /// </summary>
        public string Type { get { return _type; } }

        /// <summary>
        /// The service domain, e.g., "local."
        /// </summary>
        public string Domain { get { return _domain; } }

        /// <summary>
        /// The resolved hostname, e.g., "ike-mbp.local."
        /// </summary>
        public string Hostname { get; set; }

        /// <summary>
        /// All IP addresses for this service.
        /// </summary>
        public List<IPAddress> IPAddresses { get { return _ipAddresses; } }

        /// <summary>
        /// The port this service is listening on.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// TXT Record data for this service.
        /// </summary>
        public Dictionary<string, string> TXTRecordData { get; set; }

        internal DateTime TTLExpires { get; set; }

        #endregion

        #region Methods

        public void Publish()
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            string ips = string.Join(", ", IPAddresses);
            return string.Format("NetService: {0}\r\nHostname: {1}\r\nIP Addresses: {2}\r\nPort: {3}", FullServerInstanceName, Hostname, ips, Port);
        }

        #endregion
    }
}
