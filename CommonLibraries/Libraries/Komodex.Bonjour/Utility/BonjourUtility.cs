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
using System.IO;

namespace Komodex.Bonjour
{
    internal static class BonjourUtility
    {
        #region Constants

        public static readonly IPAddress MulticastDNSAddress = IPAddress.Parse("224.0.0.251");
        public const int MulticastDNSPort = 5353;

        #endregion

        #region List<byte> Extensions

        public static void AddNetworkOrderBytes(this List<byte> bytes, Int16 value)
        {
            bytes.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(value)));
        }

        public static void AddNetworkOrderBytes(this List<byte> bytes, UInt16 value)
        {
            unchecked
            {
                bytes.AddNetworkOrderBytes((short)value);
            }
        }

        public static void AddNetworkOrderBytes(this List<byte> bytes, Int32 value)
        {
            bytes.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(value)));
        }

        #endregion

        #region BinaryReader Extensions

        public static Int16 ReadNetworkOrderInt16(this BinaryReader reader)
        {
            return IPAddress.NetworkToHostOrder(reader.ReadInt16());
        }

        public static UInt16 ReadNetworkOrderUInt16(this BinaryReader reader)
        {
            unchecked
            {
                return (UInt16)reader.ReadNetworkOrderInt16();
            }
        }

        public static Int32 ReadNetworkOrderInt32(this BinaryReader reader)
        {
            return IPAddress.NetworkToHostOrder(reader.ReadInt32());
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

        /// <summary>
        /// Reads a hostname from the current position of the specified BinaryReader.
        /// </summary>
        public static string ReadHostnameFromBytes(BinaryReader reader)
        {
            string result = string.Empty;

            byte length = reader.ReadByte();

            // Check whether this is a pointer to a hostname that appeared earlier in the message
            if ((length & 0xC0) == 0xC0)
            {
                // Get the new position (from the remaining 6 bytes + the next 8 bytes)
                int newPosition = (length & ~0xC0) << 8;
                newPosition = newPosition | reader.ReadByte();
                // Store the current stream position
                var oldPosition = reader.BaseStream.Position;

                // Seek to the new position and read the hostname
                reader.BaseStream.Position = newPosition;
                result = ReadHostnameFromBytes(reader);

                // Return to the previous position and return the result
                reader.BaseStream.Position = oldPosition;
                return result;
            }

            // Get the label content and recurse to read the next label or pointer
            if (length > 0)
            {
                var bytes = reader.ReadBytes(length);
                result = Encoding.UTF8.GetString(bytes, 0, bytes.Length);
                result += "." + ReadHostnameFromBytes(reader);
            }

            return result;
        }

        #endregion

        #region TXT Record Parsing

        public static Dictionary<string, string> GetDictionaryFromTXTRecordBytes(BinaryReader reader, int length)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();

            var stopPosition = reader.BaseStream.Position + length;
            while (reader.BaseStream.Position < stopPosition)
            {
                var txtLength = reader.ReadByte();
                string txt = Encoding.UTF8.GetString(reader.ReadBytes(txtLength), 0, txtLength);
                int eqIndex = txt.IndexOf('=');
                if (eqIndex < 1)
                    continue; // Skip records that aren't in key=value format
                result.Add(txt.Substring(0, eqIndex), txt.Substring(eqIndex + 1));
            }

            return result;
        }

        public static byte[] GetTXTRecordBytesFromDictionary(Dictionary<string, string> data)
        {
            List<byte> result = new List<byte>();

            foreach (var item in data)
            {
                string txt = item.Key + "=" + item.Value;
                byte[] txtBytes = Encoding.UTF8.GetBytes(txt);
                result.Add((byte)txtBytes.Length);
                result.AddRange(txtBytes);
            }

            return result.ToArray();
        }

        #endregion
    }
}
