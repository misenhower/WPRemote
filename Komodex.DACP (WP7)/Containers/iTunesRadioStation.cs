using Komodex.DACP.Databases;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Komodex.DACP.Containers
{
    public class iTunesRadioStation : DACPContainer
    {
        public iTunesRadioStation(DACPDatabase database, DACPNodeDictionary nodes)
            : base(database, nodes)
        { }

        public bool IsFeaturedStation { get; private set; }

        protected override void ProcessNodes(DACPNodeDictionary nodes)
        {
            base.ProcessNodes(nodes);

            IsFeaturedStation = nodes.GetBool("aeRf");
        }
    }
}
