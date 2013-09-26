using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Komodex.DACP
{
    public class DACPNode
    {
        public DACPNode(string key, byte[] value)
        {
            Key = key;
            Value = value;
        }

        public string Key { get; protected set; }
        public byte[] Value { get; protected set; }
    }
}
