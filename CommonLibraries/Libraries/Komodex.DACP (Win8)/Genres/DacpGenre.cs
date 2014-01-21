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

namespace Komodex.DACP.Genres
{
    public class DacpGenre : DacpElement
    {
        public DacpGenre(DacpContainer container, byte[] data)
            : this(container, DacpUtility.GetStringValue(data))
        { }

        public DacpGenre(DacpContainer container, string name)
            : base(container.Client, null)
        {
            Name = name;
            Database = container.Database;
            Container = container;
        }

        public DacpDatabase Database { get; private set; }
        public DacpContainer Container { get; private set; }

        protected override void ProcessNodes(DacpNodeDictionary nodes)
        {
        }

        internal virtual DacpQueryElement GenreQuery
        {
            get { return DacpQueryPredicate.Is("daap.songgenre", Name); }
        }

        #region Items

        private List<DacpItem> _items;
        private IDacpList _groupedItems;

        public async Task<List<DacpItem>> GetItemsAsync()
        {
            if (_items != null)
                return _items;

            await GetGroupedItemsAsync().ConfigureAwait(false);
            return _items;
        }

        public async Task<IDacpList> GetGroupedItemsAsync()
        {
            if (_groupedItems != null)
                return _groupedItems;

            var query = DacpQueryCollection.And(DacpQueryPredicate.Is("daap.songgenre", Name), Container.MediaKindQuery);
            DacpRequest request = Container.GetItemsRequest(query, "name", true);

            try
            {
                var response = await Client.SendRequestAsync(request).ConfigureAwait(false);
                _groupedItems = DacpUtility.GetAlphaGroupedDacpList(response.Nodes, n => new DacpItem(Container, n), out _items);
            }
            catch
            {
                _items = new List<DacpItem>();
                _groupedItems = new DacpList<DacpItem>(false);
            }

            return _groupedItems;
        }

        #endregion

        #region Commands

        public async Task<bool> Play(PlayQueueMode mode = PlayQueueMode.Replace)
        {
            DacpRequest request;
            if (Client.ServerSupportsPlayQueue)
                request = Database.GetPlayQueueEditRequest("add", GenreQuery, mode, "name");
            else
                request = Database.GetCueSongRequest(DacpQueryCollection.And(GenreQuery, Container.MediaKindQuery), "name", 0);

            try { await Client.SendRequestAsync(request).ConfigureAwait(false); }
            catch { return false; }
            return true;
        }

        public async Task<bool> PlayItem(DacpItem item, PlayQueueMode mode = PlayQueueMode.Replace)
        {
            DacpRequest request;
            if (Client.ServerSupportsPlayQueue)
            {
                request = Database.GetPlayQueueEditRequest("add", DacpQueryPredicate.Is("dmap.itemid", item.ID), mode, "name");
                request.QueryParameters["queuefilter"] = string.Format("genre:{0}", Uri.EscapeDataString(DacpUtility.EscapeSingleQuotes(Name)));
            }
            else
            {
                var items = await GetItemsAsync();
                int index = items.FindIndex(i => i.ID == item.ID);
                if (index < 0)
                    return false;
                request = Database.GetCueSongRequest(DacpQueryCollection.And(GenreQuery, Container.MediaKindQuery), "name", index);
            }

            try { await Client.SendRequestAsync(request).ConfigureAwait(false); }
            catch { return false; }
            return true;
        }

        public async Task<bool> Shuffle()
        {
            DacpRequest request;
            if (Client.ServerSupportsPlayQueue)
                request = Database.GetPlayQueueEditRequest("add", GenreQuery, PlayQueueMode.Shuffle, "name");
            else
                request = Database.GetCueShuffleRequest(DacpQueryCollection.And(GenreQuery, Container.MediaKindQuery), "name");

            try { await Client.SendRequestAsync(request).ConfigureAwait(false); }
            catch { return false; }
            return true;
        }

        #endregion
    }
}
