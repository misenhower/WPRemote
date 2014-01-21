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
    public class Playlist : DacpContainer
    {
        public Playlist(DacpDatabase database, DacpNodeDictionary nodes)
            : base(database, nodes)
        { }

        public bool IsSmartPlaylist { get; private set; }
        public bool IsSavedGenius { get; private set; }

        protected override void ProcessNodes(DacpNodeDictionary nodes)
        {
            base.ProcessNodes(nodes);

            IsSmartPlaylist = nodes.GetBool("aeSP");
            IsSavedGenius = nodes.GetBool("aeSG");
        }

        #region Items

        private List<DacpItem> _items;
        private int _itemCacheRevision;

        public async Task<List<DacpItem>> GetItemsAsync()
        {
            if (_items != null && _itemCacheRevision == Client.CurrentLibraryUpdateNumber)
                return _items;

            DacpRequest request = GetContainerItemsRequest();
            _items = await Client.GetListAsync(request, n => (DacpItem)new Song(this, n)).ConfigureAwait(false);

            _itemCacheRevision = Client.CurrentLibraryUpdateNumber;

            return _items;
        }

        public async Task<IList> GetItemsOrSubListsAsync()
        {
            // Check whether there are any playlists under this one
            if (HasChildContainers)
                return Database.Playlists.Where(pl => pl.ParentContainerID == this.ID).ToDacpList();

            // Get the playlist's items
            return await GetItemsAsync().ConfigureAwait(false);
        }

        #endregion

        #region Commands

        public async Task<bool> Play(PlayQueueMode mode = PlayQueueMode.Replace)
        {
            if (!Client.ServerSupportsPlayQueue)
            {
                // Send a play request for the first item in this playlist
                var items = await GetItemsAsync();
                if (items == null || items.Count == 0)
                    return false;
                return await PlayItem(items[0], mode).ConfigureAwait(false);
            }

            var query = DacpQueryPredicate.Is("dmap.itemid", ID);
            DacpRequest request = Database.GetPlayQueueEditRequest("add", query, mode);
            request.QueryParameters["query-modifier"] = "containers";

            try { await Client.SendRequestAsync(request).ConfigureAwait(false); }
            catch { return false; }
            return true;
        }

        public async Task<bool> PlayItem(DacpItem item, PlayQueueMode mode = PlayQueueMode.Replace)
        {
            DacpRequest request;
            if (Client.ServerSupportsPlayQueue)
            {
                var query = DacpQueryPredicate.Is("dmap.containeritemid", item.ContainerItemID);
                request = Database.GetPlayQueueEditRequest("add", query, mode, "physical");
                request.QueryParameters["queuefilter"] = string.Format("playlist:{0}", ID);
            }
            else
            {
                request = GetPlaySpecRequest();
                request.QueryParameters["container-item-spec"] = DacpQueryPredicate.Is("dmap.containeritemid", "0x" + item.ContainerItemID.ToString("x8")).ToString();
            }

            try { await Client.SendRequestAsync(request).ConfigureAwait(false); }
            catch { return false; }
            return true;
        }

        public async Task<bool> Shuffle()
        {
            if (Client.ServerSupportsPlayQueue)
                return await Play(PlayQueueMode.Shuffle).ConfigureAwait(false);

            DacpRequest request = GetPlaySpecRequest();
            request.QueryParameters["dacp.shufflestate"] = "1";

            try { await Client.SendRequestAsync(request).ConfigureAwait(false); }
            catch { return false; }
            return true;
        }

        private DacpRequest GetPlaySpecRequest()
        {
            DacpRequest request = new DacpRequest("/ctrl-int/1/playspec");
            request.QueryParameters["database-spec"] = DacpQueryPredicate.Is("dmap.persistentid", "0x" + Database.PersistentID.ToString("x16")).ToString();
            request.QueryParameters["container-spec"] = DacpQueryPredicate.Is("dmap.persistentid", "0x" + PersistentID.ToString("x16")).ToString();

            return request;
        }

        #endregion
    }
}
