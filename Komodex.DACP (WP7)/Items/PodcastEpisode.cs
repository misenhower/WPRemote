using Komodex.DACP.Containers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Komodex.DACP.Items
{
    public class PodcastEpisode : DACPItem
    {
        public PodcastEpisode(DACPContainer container, DACPNodeDictionary nodes)
            : base(container, nodes)
        { }
    }
}
