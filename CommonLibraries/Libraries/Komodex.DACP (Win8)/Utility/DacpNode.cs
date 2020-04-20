using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Komodex.DACP
{
    public class DacpNode
    {
        public DacpNode(string key, byte[] value)
        {
            Key = key;
            Value = value;
        }

        public string Key { get; protected set; }
        public byte[] Value { get; protected set; }
    }
}
