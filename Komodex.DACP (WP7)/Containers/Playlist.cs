using Komodex.DACP.Databases;
using Komodex.DACP.Items;
using System;
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
        public List<DACPItem> Items
        {
            get { return _items; }
            private set
            {
                if (_items == value)
                    return;
                _items = value;
                SendPropertyChanged();
            }
        }

        public async Task<bool> RequestItemsAsync()
        {
            DACPRequest request = GetContainerItemsRequest();

            try
            {
                var response = await Server.SubmitRequestAsync(request);
                Items = DACPUtility.GetItemsFromNodes(response.Nodes, n => (DACPItem)new Song(this, n)).ToList();
            }
            catch (Exception e)
            {
                Items = null;
                Server.HandleHTTPException(request, e);
                return false;
            }

            return true;
        }

        #endregion
    }
}
