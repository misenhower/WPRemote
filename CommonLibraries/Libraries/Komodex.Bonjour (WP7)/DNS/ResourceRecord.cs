using Komodex.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace Komodex.Bonjour.DNS
{
    internal class ResourceRecord
    {
        public ResourceRecord()
        {
            Class = 0x01; // IN
        }

        #region Properties

        public string Name { get; set; }

        public ResourceRecordType Type { get; set; }

        public int Class { get; set; }

        public bool CacheFlush { get; set; }

        public TimeSpan TimeToLive { get; set; }

        #endregion

        #region Methods

        public static ResourceRecord FromBinaryReader(BinaryReader reader)
        {
            // Hostname
            string hostname = BonjourUtility.ReadHostnameFromBytes(reader);

            // Record Type
            ResourceRecordType type = (ResourceRecordType)reader.ReadNetworkOrderUInt16();

            ResourceRecord record;
            switch (type)
            {
                case ResourceRecordType.A:
                    record = new ARecord();
                    break;
                case ResourceRecordType.PTR:
                    record = new PTRRecord();
                    break;
                case ResourceRecordType.SRV:
                    record = new SRVRecord();
                    break;
                case ResourceRecordType.TXT:
                    record = new TXTRecord();
                    break;
                default:
                    record = new ResourceRecord();
                    record.Type = type;
                    break;
            }

            record.Name = hostname;

            // Class and Cache Flush bit
            ushort rrclass = reader.ReadNetworkOrderUInt16();
            record.Class = (rrclass & 0x7fff);
            record.CacheFlush = rrclass.GetBit(15);

            // TTL
            record.TimeToLive = TimeSpan.FromSeconds(reader.ReadNetworkOrderInt32());

            // Data
            ushort dataLength = reader.ReadNetworkOrderUInt16();
            record.SetDataFromReader(reader, dataLength);

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

            // Class and Cache Flush bit
            ushort rrclass = (ushort)Class;
            BitUtility.SetBit(ref rrclass, 15, CacheFlush);
            result.AddNetworkOrderBytes(rrclass);

            // TTL
            result.AddNetworkOrderBytes((int)TimeToLive.TotalSeconds);

            // Data
            byte[] dataBytes = GetDataBytes();

            result.AddNetworkOrderBytes((ushort)dataBytes.Length);
            result.AddRange(dataBytes);

            return result.ToArray();
        }

        private byte[] _dataBytes;
        protected virtual void SetDataFromReader(BinaryReader reader, int length)
        {
            _dataBytes = reader.ReadBytes(length);
        }

        protected virtual byte[] GetDataBytes()
        {
            return _dataBytes;
        }

        #endregion

        #region Summary Strings

        /// <summary>
        /// Returns a short summary of this record, e.g., "A: 10.0.0.1".
        /// </summary>
        public virtual string Summary
        {
            get { return string.Format("(Unknown RR type {0})", Type); }
        }

        /// <summary>
        /// Returns a detailed summary of this record, e.g., "A: computer-name.local. => 10.0.0.1 (TTL: 00:02:00)".
        /// </summary>
        public override string ToString()
        {
            string cacheFlush = (CacheFlush) ? ", cache flush" : string.Empty;
            return string.Format("[{0}] {1} (TTL: {2}{3})", Type, Name, TimeToLive, cacheFlush);
        }

        #endregion
    }
}
