using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Komodex.DACP
{
    public class ItemGroup<T> : List<T>
    {
        public ItemGroup(string key)
        {
            Key = key;
        }

        public ItemGroup(string key, IEnumerable<T> collection)
            : base(collection)
        {
            Key = key;
        }

        public string Key { get; private set; }
        public object KeyObject { get; internal set; }
        public bool HasItems { get { return Count > 0; } }
    }
}
