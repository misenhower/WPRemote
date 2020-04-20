using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Komodex.Bonjour.DNS
{
    internal class PTRRecord : ResourceRecord
    {
        public PTRRecord()
        {
            Type = ResourceRecordType.PTR;
        }

        public string DomainName { get; set; }

        protected override void SetDataFromReader(BinaryReader reader, int length)
        {
            DomainName = BonjourUtility.ReadHostnameFromBytes(reader);
        }

        protected override byte[] GetDataBytes()
        {
            return BonjourUtility.HostnameToBytes(BonjourUtility.FormatLocalHostname(DomainName));
        }

        public override string Summary
        {
            get { return string.Format("{0}: {1}", Type, DomainName ?? "(No data)"); }
        }

        public override string ToString()
        {
            string cacheFlush = (CacheFlush) ? ", cache flush" : string.Empty;
            return string.Format("{0}: {1} => {2} (TTL: {3}{4})", Type, Name, DomainName ?? "(No data)", TimeToLive, cacheFlush);
        }
    }
}
