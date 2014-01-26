using Komodex.DACP.Databases;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Komodex.DACP.Containers
{
    public class GeniusMix : Playlist
    {
        public GeniusMix(DacpDatabase database, DacpNodeDictionary nodes)
            : base(database, nodes)
        { }

        public string Description { get; private set; }

        protected override void ProcessNodes(DacpNodeDictionary nodes)
        {
            base.ProcessNodes(nodes);

            Description = nodes.GetString("ascn");
        }
    }
}
