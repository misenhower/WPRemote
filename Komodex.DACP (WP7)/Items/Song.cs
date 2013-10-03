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

        public string ArtistName { get; private set; }
        public string AlbumName { get; private set; }
        public string AlbumArtistName { get; private set; }

        protected override void ProcessNodes(DACPNodeDictionary nodes)
        {
            base.ProcessNodes(nodes);

            ArtistName = nodes.GetString("asar");
            AlbumName = nodes.GetString("asal");
            AlbumArtistName = nodes.GetString("asaa");
        }
    }
}
