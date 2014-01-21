using Komodex.Common;
using Komodex.DACP.Containers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Komodex.DACP.Items
{
    public class Song : DacpItem
    {
        public Song(DacpContainer container, DacpNodeDictionary nodes)
            : base(container, nodes)
        { }

        public string AlbumArtistName { get; private set; }

        protected override void ProcessNodes(DacpNodeDictionary nodes)
        {
            base.ProcessNodes(nodes);

            AlbumArtistName = nodes.GetString("asaa");
        }

        #region Display

        public string SecondLine
        {
            get
            {
                if (string.IsNullOrEmpty(AlbumArtistName) || ArtistName == AlbumArtistName)
                    return FormattedDuration;
                return Utility.JoinNonEmptyStrings(" – ", ArtistName, FormattedDuration);
            }
        }

        #endregion
    }
}
