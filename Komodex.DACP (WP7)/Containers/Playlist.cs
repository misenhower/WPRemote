using Komodex.DACP.Databases;
using Komodex.DACP.Items;
using Komodex.DACP.Queries;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Komodex.DACP.Containers
{
    public class Playlist : DACPContainer
    {
        public Playlist(DACPDatabase database, DACPNodeDictionary nodes)
            : base(database, nodes)
        { }

        public bool IsSmartPlaylist { get; private set; }
        public bool IsSavedGenius { get; private set; }

        protected override void ProcessNodes(DACPNodeDictionary nodes)
        {
            base.ProcessNodes(nodes);

            IsSmartPlaylist = nodes.GetBool("aeSP");
            IsSavedGenius = nodes.GetBool("aeSG");
        }

        #region Items

        private List<DACPItem> _items;
        private int _itemCacheRevision;

        public async Task<List<DACPItem>> GetItemsAsync()
        {
            if (_items != null && _itemCacheRevision == Server.CurrentLibraryUpdateNumber)
                return _items;

            DACPRequest request = GetContainerItemsRequest();
            _items = await Server.GetListAsync(request, n => (DACPItem)new Song(this, n)).ConfigureAwait(false);

            _itemCacheRevision = Server.CurrentLibraryUpdateNumber;

            return _items;
        }

        public async Task<IList> GetItemsOrSubListsAsync()
        {
            // Check whether there are any playlists under this one
            if (HasChildContainers)
                return Database.Playlists.Where(pl => pl.ParentContainerID == this.ID).ToDACPList();

            // Get the playlist's items
            return await GetItemsAsync().ConfigureAwait(false);
        }

        #endregion

        #region Commands

        public async Task<bool> Play(PlayQueueMode mode = PlayQueueMode.Replace)
        {
            if (!Server.SupportsPlayQueue)
            {
                // Send a play request for the first item in this playlist
                var items = await GetItemsAsync();
                if (items == null || items.Count == 0)
                    return false;
                return await PlayItem(items[0], mode).ConfigureAwait(false);
            }

            var query = DACPQueryPredicate.Is("dmap.itemid", ID);
            DACPRequest request = Database.GetPlayQueueEditRequest("add", query, mode);
            request.QueryParameters["query-modifier"] = "containers";

            try { await Server.SubmitRequestAsync(request).ConfigureAwait(false); }
            catch { return false; }
            return true;
        }

        public async Task<bool> PlayItem(DACPItem item, PlayQueueMode mode = PlayQueueMode.Replace)
        {
            DACPRequest request;
            if (Server.SupportsPlayQueue)
            {
                var query = DACPQueryPredicate.Is("dmap.containeritemid", item.ContainerItemID);
                request = Database.GetPlayQueueEditRequest("add", query, mode, "physical");
                request.QueryParameters["queuefilter"] = string.Format("playlist:{0}", ID);
            }
            else
            {
                request = GetPlaySpecRequest();
                request.QueryParameters["container-item-spec"] = DACPQueryPredicate.Is("dmap.containeritemid", "0x" + item.ContainerItemID.ToString("x8")).ToString();
            }

            try { await Server.SubmitRequestAsync(request).ConfigureAwait(false); }
            catch { return false; }
            return true;
        }

        public async Task<bool> Shuffle()
        {
            if (Server.SupportsPlayQueue)
                return await Play(PlayQueueMode.Shuffle).ConfigureAwait(false);

            DACPRequest request = GetPlaySpecRequest();
            request.QueryParameters["dacp.shufflestate"] = "1";

            try { await Server.SubmitRequestAsync(request).ConfigureAwait(false); }
            catch { return false; }
            return true;
        }

        private DACPRequest GetPlaySpecRequest()
        {
            DACPRequest request = new DACPRequest("/ctrl-int/1/playspec");
            request.QueryParameters["database-spec"] = DACPQueryPredicate.Is("dmap.persistentid", "0x" + Database.PersistentID.ToString("x16")).ToString();
            request.QueryParameters["container-spec"] = DACPQueryPredicate.Is("dmap.persistentid", "0x" + PersistentID.ToString("x16")).ToString();

            return request;
        }

        #endregion
    }
}
