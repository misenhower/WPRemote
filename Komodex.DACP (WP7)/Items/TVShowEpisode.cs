using Komodex.DACP.Containers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Komodex.DACP.Items
{
    public class TVShowEpisode : DACPItem
    {
        public TVShowEpisode(DACPContainer container, DACPNodeDictionary nodes)
            : base(container, nodes)
        { }

        public DateTime? DateReleased { get; private set; }

        protected override void ProcessNodes(DACPNodeDictionary nodes)
        {
            base.ProcessNodes(nodes);

            if (nodes.ContainsKey("asdr"))
                DateReleased = nodes.GetDateTime("asdr");
        }
    }
}
