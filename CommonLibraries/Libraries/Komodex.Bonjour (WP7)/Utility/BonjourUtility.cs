using Komodex.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace Komodex.Bonjour
{
    internal static class BonjourUtility
    {
        #region Constants

        public const string MulticastDNSAddress = "224.0.0.251";
        public const int MulticastDNSPort = 5353;

        #endregion

        #region List<byte> Extensions

        public static void AddNetworkOrderBytes(this List<byte> bytes, Int16 value)
        {
            bytes.AddRange(BitConverter.GetBytes(BitUtility.HostToNetworkOrder(value)));
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
            bytes.AddRange(BitConverter.GetBytes(BitUtility.HostToNetworkOrder(value)));
        }

        #endregion

        #region BinaryReader Extensions

        public static Int16 ReadNetworkOrderInt16(this BinaryReader reader)
        {
            return BitUtility.NetworkToHostOrder(reader.ReadInt16());
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
            return BitUtility.NetworkToHostOrder(reader.ReadInt32());
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


        /// <summary>
        /// <para>Parses server instance names with the following format: &lt;Instance&gt;.&lt;Service&gt;.&lt;Domain&gt;</para>
        /// <para>Example: "BDB3DEDD8FDC6E13._touch-able._tcp.local." returns Name: "BDB3DEDD8FDC6E13", Type: "_touch-able._tcp.", Domain: "local."</para>
        /// </summary>
        public static void ParseServiceInstanceName(string instanceName, out string name, out string type, out string domain)
        {
            name = null;
            type = null;
            domain = null;

            var parts = instanceName.Split('.');
            Array.Reverse(parts);

            for (int i = 0; i < parts.Length; i++)
            {
                if (string.IsNullOrEmpty(parts[i]))
                    continue;

                if (type == null)
                {
                    if (parts[i].StartsWith("_"))
                    {
                        type = string.Format("{0}.{1}.", parts[i + 1], parts[i]);
                        i++;
                    }
                    else
                    {
                        domain = parts[i] + "." + domain;
                    }
                }
                else
                {
                    name = parts[i] + "." + name;
                }
            }

            if (name != null)
                name = name.TrimEnd(new char[] { '.' });
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
