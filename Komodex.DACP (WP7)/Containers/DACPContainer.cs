using Komodex.DACP.Databases;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Komodex.DACP.Containers
{
    public class DACPContainer : DACPElement
    {
        public DACPContainer(DACPDatabase database, DACPNodeDictionary nodes)
            : base(database.Server, nodes)
        {
            Database = database;
        }

        public DACPDatabase Database { get; private set; }

        public bool BasePlaylist { get; private set; }
        public int SpecialPlaylistID { get; private set; }
        public int ItemCount { get; private set; }

        protected override void ProcessNodes(DACPNodeDictionary nodes)
        {
            base.ProcessNodes(nodes);

            BasePlaylist = nodes.GetBool("abpl");
            SpecialPlaylistID = nodes.GetByte("aePS");
            ItemCount = nodes.GetInt("mimc");
        }

        public static DACPContainer GetContainer(DACPDatabase database, DACPNodeDictionary nodes)
        {
            // Base Playlist
            if (nodes.GetBool("abpl"))
                return new DACPContainer(database, nodes);

            return new DACPContainer(database, nodes);
        }
    }
}
