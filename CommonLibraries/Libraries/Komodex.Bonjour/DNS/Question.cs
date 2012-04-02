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
using System.IO;

namespace Komodex.Bonjour.DNS
{
    internal class Question
    {
        public Question()
        {
            Class = 1; // IN
        }

        public Question(string name, ResourceRecordType type)
            : this()
        {
            Name = name;
            Type = type;
        }

        #region Properties

        public string Name { get; set; }

        public ResourceRecordType Type { get; set; }

        public int Class { get; set; }

        #endregion

        #region Methods

        public static Question FromBinaryReader(BinaryReader reader)
        {
            Question question = new Question();

            question.Name = BonjourUtility.ReadHostnameFromBytes(reader);
            question.Type = (ResourceRecordType)reader.ReadNetworkOrderUInt16();
            question.Class = reader.ReadNetworkOrderUInt16();

            return question;
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
            result.AddNetworkOrderBytes((ushort)Class);

            return result.ToArray();
        }

        #endregion

        #region Summary Strings

        public override string ToString()
        {
            return string.Format("{0}: {1}", Type, Name);
        }

        #endregion
    }
}
