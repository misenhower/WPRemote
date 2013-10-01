using Komodex.DACP.Containers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Komodex.DACP.Items
{
    public class Song : DACPItem
    {
        public Song(DACPContainer container, DACPNodeDictionary nodes)
            : base(container, nodes)
        { }
    }
}
