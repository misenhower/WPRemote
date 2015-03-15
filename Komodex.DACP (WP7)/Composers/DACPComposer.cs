using Komodex.Common;
using Komodex.DACP.Containers;
using Komodex.DACP.Databases;
using Komodex.DACP.Items;
using Komodex.DACP.Queries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Komodex.DACP.Composers
{
    public class DACPComposer : DACPElement
    {
        public DACPComposer(DACPContainer container, byte[] data)
            : this(container, DACPUtility.GetStringValue(data))
        { }

        public DACPComposer(DACPContainer container, string name)
            : base(container.Server, null)
        {
            Name = name;
            Database = container.Database;
            Container = container;
        }

        public DACPComposer(DACPContainer container, DACPNodeDictionary nodes)
            : base(container.Server, nodes)
        {
            Database = container.Database;
            Container = container;
        }

        public DACPDatabase Database { get; private set; }
        public DACPContainer Container { get; private set; }

        protected override void ProcessNodes(DACPNodeDictionary nodes)
        {
            if (nodes == null)
                return;

            base.ProcessNodes(nodes);
        }

        internal virtual DACPQueryElement ComposerQuery
        {
            get { return DACPQueryPredicate.Is("daap.songcomposer", Name); }
        }

        #region Items

        private List<DACPItem> _items;
        private IDACPList _groupedItems;

        public async Task<List<DACPItem>> GetItemsAsync()
        {
            if (_items != null)
                return _items;

            await GetGroupedItemsAsync().ConfigureAwait(false);
            return _items;
        }

        public async Task<IDACPList> GetGroupedItemsAsync()
        {
            if (_groupedItems != null)
                return _groupedItems;

            var query = DACPQueryCollection.And(DACPQueryPredicate.Is("daap.songcomposer", Name), Container.MediaKindQuery);
            DACPRequest request = Container.GetItemsRequest(query, "album", false);

            try
            {
                var response = await Server.SubmitRequestAsync(request).ConfigureAwait(false);
                var items = DACPUtility.GetItemsFromNodes(response.Nodes, n => new DACPItem(Container, n)).ToList();

                ItemGroup<DACPItem> currentGroup = null;
                DACPList<ItemGroup<DACPItem>> result = new DACPList<ItemGroup<DACPItem>>(true);

                foreach (var item in items)
                {
                    if (currentGroup == null || currentGroup.Key != item.AlbumName)
                    {
                        currentGroup = new ItemGroup<DACPItem>(item.AlbumName);
                        result.Add(currentGroup);
                    }
                    currentGroup.Add(item);
                }

                _items = items;
                _groupedItems = result;
            }
            catch
            {
                _items = new List<DACPItem>();
                _groupedItems = new DACPList<DACPItem>(false);
            }

            return _groupedItems;
        }

        #endregion

        #region Commands

        public async Task<bool> Play(PlayQueueMode mode = PlayQueueMode.Replace)
        {
            DACPRequest request;
            if (Server.SupportsPlayQueue)
                request = Database.GetPlayQueueEditRequest("add", ComposerQuery, mode, "album");
            else
                request = Database.GetCueSongRequest(DACPQueryCollection.And(ComposerQuery, Container.MediaKindQuery), "album", 0);

            try { await Server.SubmitRequestAsync(request).ConfigureAwait(false); }
            catch { return false; }
            return true;
        }

        public async Task<bool> PlayItem(DACPItem item, PlayQueueMode mode = PlayQueueMode.Replace)
        {
            DACPRequest request;
            if (Server.SupportsPlayQueue)
            {
                request = Database.GetPlayQueueEditRequest("add", DACPQueryPredicate.Is("dmap.itemid", item.ID), mode, "album");
                request.QueryParameters["queuefilter"] = string.Format("composer:{0}", Uri.EscapeDataString(DACPUtility.EscapeSingleQuotes(Name)));
            }
            else
            {
                var items = await GetItemsAsync();
                int index = items.FindIndex(i => i.ID == item.ID);
                if (index < 0)
                    return false;
                request = Database.GetCueSongRequest(DACPQueryCollection.And(ComposerQuery, Container.MediaKindQuery), "album", index);
            }

            try { await Server.SubmitRequestAsync(request).ConfigureAwait(false); }
            catch { return false; }
            return true;
        }

        public async Task<bool> Shuffle()
        {
            DACPRequest request;
            if (Server.SupportsPlayQueue)
                request = Database.GetPlayQueueEditRequest("add", ComposerQuery, PlayQueueMode.Shuffle, "album");
            else
                request = Database.GetCueShuffleRequest(DACPQueryCollection.And(ComposerQuery, Container.MediaKindQuery), "album");

            try { await Server.SubmitRequestAsync(request).ConfigureAwait(false); }
            catch { return false; }
            return true;
        }

        #endregion
    }
}
