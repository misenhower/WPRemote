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
            byte[] dataBytes = reader.ReadBytes(dataLength);

            switch (record.Type)
            {
                //case RRType.A:
                //    break;
                //case RRType.PTR:
                //    break;
                //case RRType.SRV:
                //    break;
                //case RRType.TXT:
                //    break;
                default:
                    record.Data = dataBytes;
                    break;
            }

            return record;
        }

        public byte[] GetBytes()
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return Type + " " + Name;
        }

        #endregion
    }
}
