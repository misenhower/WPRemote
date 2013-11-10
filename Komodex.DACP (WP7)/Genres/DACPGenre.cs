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
    public class DACPGenre : DACPElement
    {
        public DACPGenre(DACPContainer container, byte[] data)
            : this(container, DACPUtility.GetStringValue(data))
        { }

        public DACPGenre(DACPContainer container, string name)
            : base(container.Server, null)
        {
            Name = name;
            Database = container.Database;
            Container = container;
        }

        public DACPDatabase Database { get; private set; }
        public DACPContainer Container { get; private set; }

        protected override void ProcessNodes(DACPNodeDictionary nodes)
        {
        }

        internal virtual DACPQueryElement GenreQuery
        {
            get { return DACPQueryPredicate.Is("daap.songgenre", Name); }
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

            var query = DACPQueryCollection.And(DACPQueryPredicate.Is("daap.songgenre", Name), Container.MediaKindQuery);
            DACPRequest request = Container.GetItemsRequest(query, "name", true);

            try
            {
                var response = await Server.SubmitRequestAsync(request).ConfigureAwait(false);
                _groupedItems = DACPUtility.GetAlphaGroupedDACPList(response.Nodes, n => new DACPItem(Container, n), out _items);
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
            {
                request = Database.GetPlayQueueEditRequest("add", GenreQuery, mode);
                request.QueryParameters["sort"] = "name";
            }
            else
            {
                request = Database.GetCueSongRequest(DACPQueryCollection.And(GenreQuery, Container.MediaKindQuery), "name", 0);
            }

            try { await Server.SubmitRequestAsync(request).ConfigureAwait(false); }
            catch { return false; }
            return true;
        }

        public async Task<bool> PlayItem(DACPItem item, PlayQueueMode mode = PlayQueueMode.Replace)
        {
            DACPRequest request;
            if (Server.SupportsPlayQueue)
            {
                request = Database.GetPlayQueueEditRequest("add", DACPQueryPredicate.Is("dmap.itemid", item.ID), mode);
                request.QueryParameters["queuefilter"] = string.Format("genre:{0}", DACPUtility.EscapeSingleQuotes(Name));
                request.QueryParameters["sort"] = "name";
            }
            else
            {
                var items = await GetItemsAsync();
                request = Database.GetCueSongRequest(DACPQueryCollection.And(GenreQuery, Container.MediaKindQuery), "name", items.IndexOf(item));
            }

            try { await Server.SubmitRequestAsync(request).ConfigureAwait(false); }
            catch { return false; }
            return true;
        }

        public async Task<bool> Shuffle()
        {
            DACPRequest request;
            if (Server.SupportsPlayQueue)
            {
                request = Database.GetPlayQueueEditRequest("add", GenreQuery, PlayQueueMode.Shuffle);
                request.QueryParameters["sort"] = "name";
            }
            else
            {
                request = Database.GetCueShuffleRequest(DACPQueryCollection.And(GenreQuery, Container.MediaKindQuery), "name");
            }

            try { await Server.SubmitRequestAsync(request).ConfigureAwait(false); }
            catch { return false; }
            return true;
        }

        #endregion
    }
}
