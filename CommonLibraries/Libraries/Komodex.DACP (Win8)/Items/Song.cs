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

        public int? TrackNumber { get; private set; }
        public string AlbumArtistName { get; private set; }
        public UInt64 AlbumPersistentID { get; private set; }
        public int? Year { get; private set; }

        protected override void ProcessNodes(DacpNodeDictionary nodes)
        {
            base.ProcessNodes(nodes);

            int? trackNumber = nodes.GetNullableShort("astn");
            if (trackNumber != 0)
                TrackNumber = trackNumber;

            AlbumArtistName = nodes.GetString("asaa");
            AlbumPersistentID = (UInt64)nodes.GetLong("asai");

            int? year = nodes.GetNullableShort("asyr");
            if (year != 0)
                Year = year;
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
