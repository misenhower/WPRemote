using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Komodex.CommonLibrariesTestApp.Lists
{
    public class ListItem
    {
        public ListItem(string name)
        {
            Name = name;
        }

        public string Name { get; protected set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
