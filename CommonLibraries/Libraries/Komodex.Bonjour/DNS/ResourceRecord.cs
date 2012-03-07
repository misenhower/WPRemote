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
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace Komodex.Bonjour.DNS
{
    internal class ResourceRecord
    {
        #region Properties

        public string Name { get; set; }

        public RRType Type { get; set; }

        public ushort Class { get; set; }

        public TimeSpan TimeToLive { get; set; }

        public object Data { get; set; }

        #endregion

        #region Methods

        public static ResourceRecord FromBinaryReader(BinaryReader reader)
        {
            ResourceRecord record = new ResourceRecord();

            record.Name = BonjourUtility.ReadHostnameFromBytes(reader);
            record.Type = (RRType)reader.ReadNetworkOrderUInt16();
            record.Class = reader.ReadNetworkOrderUInt16();
            record.TimeToLive = TimeSpan.FromSeconds(reader.ReadNetworkOrderInt32());

            ushort dataLength = reader.ReadNetworkOrderUInt16();

            switch (record.Type)
            {
                case RRType.A:
                    record.Data = new IPAddress(reader.ReadBytes(dataLength));
                    break;
                case RRType.PTR:
                    record.Data = BonjourUtility.ReadHostnameFromBytes(reader);
                    break;
                case RRType.SRV:
                    record.Data = SRVRecordData.FromBinaryReader(reader);
                    break;
                case RRType.TXT:
                    record.Data = BonjourUtility.GetDictionaryFromTXTRecordBytes(reader, dataLength);
                    break;
                default:
                    record.Data = reader.ReadBytes(dataLength);
                    break;
            }

            return record;
        }

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

            // TTL
            result.AddNetworkOrderBytes((int)TimeToLive.TotalSeconds);

            // Data
            byte[] dataBytes;

            switch (Type)
            {
                case RRType.A:
                    dataBytes = ((IPAddress)Data).GetAddressBytes();
                    break;
                case RRType.PTR:
                    dataBytes = BonjourUtility.HostnameToBytes(BonjourUtility.FormatLocalHostname((string)Data));
                    break;
                case RRType.SRV:
                    dataBytes = ((SRVRecordData)Data).GetBytes();
                    break;
                case RRType.TXT:
                    dataBytes = BonjourUtility.GetTXTRecordBytesFromDictionary((Dictionary<string, string>)Data);
                    break;
                default:
                    dataBytes = (byte[])Data;
                    break;
            }

            result.AddNetworkOrderBytes((ushort)dataBytes.Length);
            result.AddRange(dataBytes);

            return result.ToArray();
        }

        public override string ToString()
        {
            string result = Type + " " + Name;
            if (Data != null)
            {
                result += " Data: ";
                if (Data is Dictionary<string, string>)
                    result += string.Join(", ", ((Dictionary<string, string>)Data).Select(kvp => kvp.Key + "=" + kvp.Value));
                else
                    result += Data.ToString();
            }
            return result;
        }

        #endregion
    }
}
