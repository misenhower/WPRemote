using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Komodex.Bonjour.DNS
{
    internal class ARecord : ResourceRecord
    {
        public ARecord()
        {
            Type = ResourceRecordType.A;
        }

        public string Address { get; set; }

        protected override void SetDataFromReader(BinaryReader reader, int length)
        {
            Address = BonjourUtility.IPAddressFromBytes(reader.ReadBytes(length));
        }

        protected override byte[] GetDataBytes()
        {
            return BonjourUtility.BytesFromIPAddress(Address);
        }

        public override string Summary
        {
            get { return string.Format("{0}: {1}", Type, Address ?? "(No data)"); }
        }

        public override string ToString()
        {
            string cacheFlush = (CacheFlush) ? ", cache flush" : string.Empty;
            return string.Format("{0}: {1} => {2} (TTL: {3}{4})", Type, Name, Address ?? "(No data)", TimeToLive, cacheFlush);
        }
    }
}
