using Komodex.DACP.Containers;
using Komodex.DACP.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Komodex.DACP.Groups
{
    public class Album : DACPGroup
    {
        public Album(DACPContainer container, DACPNodeDictionary nodes)
            : base(container, nodes)
        { }

        public override string GroupType
        {
            get { return "albums"; }
        }

        public string ArtistName { get; private set; }

        protected override void ProcessNodes(DACPNodeDictionary nodes)
        {
            base.ProcessNodes(nodes);

            ArtistName = nodes.GetString("asaa");
        }

        #region Songs

        private List<Song> _songs;
        public List<Song> Songs
        {
            get { return _songs; }
            private set
            {
                if (_songs == value)
                    return;
                _songs = value;
                SendPropertyChanged();
            }
        }

        public async Task<bool> RequestSongsAsync()
        {
            string query = "(('com.apple.itunes.mediakind:1','com.apple.itunes.mediakind:32')+'daap.songalbumid:" + PersistentID + "')";
            DACPRequest request = Container.GetItemsRequest(query);

            try
            {
                var response = await Server.SubmitRequestAsync(request);
                Songs = DACPUtility.GetItemsFromNodes(response.Nodes, n => new Song(Container, n)).ToList();
            }
            catch (Exception e)
            {
                Songs = null;
                Server.HandleHTTPException(request, e);
                return false;
            }

            return true;
        }

        #endregion
    }
}
