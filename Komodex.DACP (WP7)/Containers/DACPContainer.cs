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

            // Special playlist IDs
            switch (nodes.GetByte("aePS"))
            {
                case 6: // Music
                    return new MusicContainer(database, nodes);
            }

            return new DACPContainer(database, nodes);
        }

        #region Requests

        internal DACPRequest GetGroupsRequest(string groupType, string query, bool includeSortHeaders)
        {
            DACPRequest request = new DACPRequest("/databases/{0}/groups", Database.ID);
            request.QueryParameters["meta"] = "dmap.itemname,dmap.itemid,dmap.persistentid,daap.songartist,daap.songdatereleased,dmap.itemcount,daap.songtime,dmap.persistentid";
            request.QueryParameters["type"] = "music";
            request.QueryParameters["group-type"] = groupType;
            if (includeSortHeaders)
                request.QueryParameters["include-sort-headers"] = "1";
            request.QueryParameters["sort"] = "album";
            request.QueryParameters["query"] = query;

            return request;
        }

        #endregion
    }
}
