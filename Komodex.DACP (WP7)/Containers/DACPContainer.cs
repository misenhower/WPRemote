using Komodex.DACP.Databases;
using Komodex.DACP.Queries;
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
        public int ParentContainerID { get; private set; }

        protected override void ProcessNodes(DACPNodeDictionary nodes)
        {
            base.ProcessNodes(nodes);

            BasePlaylist = nodes.GetBool("abpl");
            SpecialPlaylistID = nodes.GetByte("aePS");
            ItemCount = nodes.GetInt("mimc");
            ParentContainerID = nodes.GetInt("mpco");
        }

        public static DACPContainer GetContainer(DACPDatabase database, DACPNodeDictionary nodes)
        {
            // Base Playlist
            if (nodes.GetBool("abpl"))
                return new DACPContainer(database, nodes);

            // Special playlist IDs
            switch (nodes.GetByte("aePS"))
            {
                case 0: // Playlist
                    return new Playlist(database, nodes);
                case 6: // Music
                    return new MusicContainer(database, nodes);
            }

            return new DACPContainer(database, nodes);
        }

        protected virtual int[] MediaKinds
        {
            get { return null; }
        }

        internal virtual DACPQueryElement MediaKindQuery
        {
            get
            {
                if (Server.SupportsPlayQueue)
                    return DACPQueryCollection.Or(MediaKinds.Select(i => DACPQueryPredicate.Is("com.apple.itunes.extended-media-kind", i)).ToArray());
                return DACPQueryCollection.Or(MediaKinds.Select(i => DACPQueryPredicate.Is("com.apple.itunes.mediakind", i)).ToArray());
            }
        }

        #region Requests

        internal DACPRequest GetGroupsRequest(string groupType, DACPQueryElement query, bool includeSortHeaders)
        {
            DACPRequest request = new DACPRequest("/databases/{0}/groups", Database.ID);
            request.QueryParameters["meta"] = "dmap.itemname,dmap.itemid,dmap.persistentid,daap.songartist,daap.songdatereleased,dmap.itemcount,daap.songtime,dmap.persistentid,daap.songartistid";
            request.QueryParameters["type"] = "music";
            request.QueryParameters["group-type"] = groupType;
            if (includeSortHeaders)
                request.QueryParameters["include-sort-headers"] = "1";
            request.QueryParameters["sort"] = "album";
            if (query != null)
                request.QueryParameters["query"] = query.ToString();

            return request;
        }

        internal DACPRequest GetContainerItemsRequest()
        {
            DACPRequest request = new DACPRequest("/databases/{0}/containers/{1}/items", Database.ID, ID);
            request.QueryParameters["meta"] = "dmap.itemname,dmap.itemid,daap.songartist,daap.songalbumartist,daap.songalbum,com.apple.itunes.cloud-id,dmap.containeritemid,com.apple.itunes.has-video,com.apple.itunes.itms-songid,com.apple.itunes.extended-media-kind,dmap.downloadstatus,daap.songdisabled,com.apple.itunes.cloud-id,daap.songartistid,daap.songalbumid,dmap.persistentid,dmap.downloadstatus,daap.songalbum";
            request.QueryParameters["type"] = "music";

            return request;
        }

        internal DACPRequest GetItemsRequest(DACPQueryElement query)
        {
            // Apple's Remote uses the Base Playlist ID here rather than the actual container ID.
            DACPRequest request = new DACPRequest("/databases/{0}/containers/{1}/items", Database.ID, Database.BasePlaylist.ID);
            request.QueryParameters["meta"] = "dmap.itemname,dmap.itemid,daap.songartist,daap.songalbumartist,daap.songalbum,com.apple.itunes.cloud-id,dmap.containeritemid,com.apple.itunes.has-video,com.apple.itunes.itms-songid,com.apple.itunes.extended-media-kind,dmap.downloadstatus,daap.songdisabled,com.apple.itunes.cloud-id,daap.songartistid,daap.songalbumid,dmap.persistentid,dmap.downloadstatus,daap.songalbum";
            request.QueryParameters["type"] = "music";
            request.QueryParameters["sort"] = "album";
            if (query != null)
                request.QueryParameters["query"] = query.ToString();

            return request;
        }

        #endregion
    }
}
