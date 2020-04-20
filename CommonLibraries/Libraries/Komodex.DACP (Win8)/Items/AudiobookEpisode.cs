using Komodex.DACP.Containers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Komodex.DACP.Items
{
    public class AudiobookEpisode : DacpItem
    {
        public AudiobookEpisode(DacpContainer container, DacpNodeDictionary nodes)
            : base(container, nodes)
        { }
    }
}
