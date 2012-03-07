﻿using System;
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

namespace Komodex.Bonjour.DNS
{
    internal class SRVRecordData
    {
        #region Properties

        public ushort Priority { get; set; }

        public ushort Weight { get; set; }

        public ushort Port { get; set; } // TODO: Should I just convert to ints throughout the classes?

        public string Target { get; set; }

        #endregion

        #region Methods

        public static SRVRecordData FromBinaryReader(BinaryReader reader)
        {
            SRVRecordData srv = new SRVRecordData();

            srv.Priority = reader.ReadNetworkOrderUInt16();
            srv.Weight = reader.ReadNetworkOrderUInt16();
            srv.Port = reader.ReadNetworkOrderUInt16();
            srv.Target = BonjourUtility.ReadHostnameFromBytes(reader);

            return srv;
        }

        public byte[] GetBytes()
        {
            List<byte> result = new List<byte>();

            // Priority
            result.AddNetworkOrderBytes(Priority);

            // Weight
            result.AddNetworkOrderBytes(Weight);

            // Port
            result.AddNetworkOrderBytes(Port);

            // Target
            result.AddRange(BonjourUtility.HostnameToBytes(Target));

            return result.ToArray();
        }

        public override string ToString()
        {
            return string.Format("Target: {0} Port: {1}", Target, Port);
        }

        #endregion
    }
}
