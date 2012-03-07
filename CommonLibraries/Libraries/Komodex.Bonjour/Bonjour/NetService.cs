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

namespace Komodex.Bonjour
{
    public class NetService
    {
        #region Fields

        private List<IPAddress> _ipAddresses = new List<IPAddress>();

        #endregion

        #region Properties

        /// <summary>
        /// The name of the service, e.g., "17B30BC453C4B6A0"
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The service type, e.g., "_touch-able._tcp."
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// The service domain, e.g., "local."
        /// </summary>
        public string Domain { get; set; }

        /// <summary>
        /// The resolved hostname, e.g., "ike-mbp.local."
        /// </summary>
        public string Hostname { get; set; }

        /// <summary>
        /// All IP addresses for this service.
        /// </summary>
        public List<IPAddress> Addresses { get { return _ipAddresses; } }

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

        #endregion
    }
}
