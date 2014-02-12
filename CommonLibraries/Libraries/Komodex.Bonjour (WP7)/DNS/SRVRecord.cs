using System.Collections.Generic;
using System.IO;

namespace Komodex.Bonjour.DNS
{
    internal class SRVRecord : ResourceRecord
    {
        public SRVRecord()
        {
            Type = ResourceRecordType.SRV;
        }

        #region Properties

        public int Priority { get; set; }

        public int Weight { get; set; }

        public int Port { get; set; }

        public string Target { get; set; }

        #endregion

        #region Methods

        protected override void SetDataFromReader(BinaryReader reader, int length)
        {
            Priority = reader.ReadNetworkOrderUInt16();
            Weight = reader.ReadNetworkOrderUInt16();
            Port = reader.ReadNetworkOrderUInt16();
            Target = BonjourUtility.ReadHostnameFromBytes(reader);
        }

        protected override byte[] GetDataBytes()
        {
            List<byte> result = new List<byte>();

            // Priority
            result.AddNetworkOrderBytes((ushort)Priority);

            // Weight
            result.AddNetworkOrderBytes((ushort)Weight);

            // Port
            result.AddNetworkOrderBytes((ushort)Port);

            // Target
            result.AddRange(BonjourUtility.HostnameToBytes(Target));

            return result.ToArray();
        }

        public override string Summary
        {
            get { return string.Format("{0}: Target: {1} Port: {2}", Type, Target, Port); }
        }

        public override string ToString()
        {
            string cacheFlush = (CacheFlush) ? ", cache flush" : string.Empty;
            return string.Format("{0}: {1} => Target: {2} Port: {3} (TTL: {4}{5})", Type, Name, Target, Port, TimeToLive, cacheFlush);
        }

        #endregion
    }
}
