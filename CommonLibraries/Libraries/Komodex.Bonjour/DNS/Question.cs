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
using System.Collections.Generic;

namespace Komodex.Bonjour.DNS
{
    internal class Question
    {
        public Question()
        {
            Class = 1; // IN
        }

        public Question(string name, RRType type)
            : this()
        {
            Name = name;
            Type = type;
        }

        #region Properties

        public string Name { get; set; }

        public RRType Type { get; set; }

        public ushort Class { get; set; }

        #endregion

        #region Methods

        public byte[] GetBytes()
        {
            List<byte> result = new List<byte>(512);

            // Add the hostname
            string hostname = BonjourUtility.FormatLocalHostname(Name);
            result.AddRange(BonjourUtility.HostnameToBytes(hostname));

            // Record Type
            result.AddNetworkOrderBytes((ushort)Type);

            // Class
            result.AddNetworkOrderBytes(Class);

            return result.ToArray();
        }

        public override string ToString()
        {
            return string.Format("Question Name: {0} ({1})", Name, Type);
        }

        #endregion
    }
}
