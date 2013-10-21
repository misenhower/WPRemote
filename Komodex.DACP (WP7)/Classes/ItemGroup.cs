using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
        public bool HasItems { get { return Count > 0; } }
    }
}
