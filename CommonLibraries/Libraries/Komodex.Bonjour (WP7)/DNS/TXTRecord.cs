using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Komodex.Bonjour.DNS
{
    internal class TXTRecord : ResourceRecord
    {
        public TXTRecord()
        {
            Type = ResourceRecordType.TXT;
        }

        public Dictionary<string, string> Data { get; set; }

        protected override void SetDataFromReader(BinaryReader reader, int length)
        {
            Data = BonjourUtility.GetDictionaryFromTXTRecordBytes(reader, length);
        }

        protected override byte[] GetDataBytes()
        {
            return BonjourUtility.GetTXTRecordBytesFromDictionary(Data);
        }

        public override string Summary
        {
            get { return "TXT"; }
        }

        public override string ToString()
        {
            string cacheFlush = (CacheFlush) ? ", cache flush" : string.Empty;
            string data;
            if (Data != null)
                data = string.Join(", ", ((Dictionary<string, string>)Data).Select(kvp => kvp.Key + "=" + kvp.Value));
            else
                data = "(No data)";
            return string.Format("TXT: {0} => {1} (TTL: {2}{3})", Name, data, TimeToLive, cacheFlush);
        }
    }
}
