using Komodex.DACP.Databases;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Komodex.DACP.Containers
{
    public class GeniusMix : Playlist
    {
        public GeniusMix(DACPDatabase database, DACPNodeDictionary nodes)
            : base(database, nodes)
        { }

        public string Description { get; private set; }

        protected override void ProcessNodes(DACPNodeDictionary nodes)
        {
            base.ProcessNodes(nodes);

            Description = nodes.GetString("ascn");
        }

        #region Artwork

        public string Artwork350pxURI { get { return GetAlbumArtURI(350, 350); } }

        #endregion
    }
}
