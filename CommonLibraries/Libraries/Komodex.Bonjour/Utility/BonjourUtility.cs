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
using System.Text;

namespace Komodex.Bonjour
{
    internal static class BonjourUtility
    {
        #region Constants

        public static readonly IPAddress MulticastDNSAddress = IPAddress.Parse("224.0.0.251");
        public const int MulticastDNSPort = 5353;

        #endregion

        #region List<byte> Extensions

        public static void AddNetworkOrderBytes(this List<byte> bytes, UInt16 value)
        {
            bytes.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(value)));
        }

        #endregion

        #region Hostname Parsing

        public static string FormatLocalHostname(string hostname)
        {
            if (!hostname.EndsWith("."))
                hostname += ".";
            if (!hostname.EndsWith(".local."))
                hostname += "local.";
            return hostname;
        }

        public static byte[] HostnameToBytes(string hostname)
        {
            List<byte> result = new List<byte>(Encoding.UTF8.GetByteCount(hostname));

            var parts = hostname.Split('.');
            var partCount = parts.Length;
            byte[] partBytes;
            for (int i = 0; i < partCount; i++)
            {
                partBytes = Encoding.UTF8.GetBytes(parts[i]);
                result.Add((byte)partBytes.Length);
                result.AddRange(partBytes);
            }

            return result.ToArray();
        }

        #endregion
    }
}
