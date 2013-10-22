using Komodex.DACP.Databases;
using Komodex.DACP.Items;
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

        public async Task<IList> GetItemsOrSubListsAsync()
        {
            // Check whether there are any playlists under this one
            if (HasChildContainers)
                return Database.Playlists.Where(pl => pl.ParentContainerID == this.ID).ToDACPList();

            // Get the playlist's items
            if (_items != null)
                return _items;

            DACPRequest request = GetContainerItemsRequest();
            _items = await Server.GetListAsync(request, n => (DACPItem)new Song(this, n)).ConfigureAwait(false);
            return _items;
        }

        #endregion
    }
}
