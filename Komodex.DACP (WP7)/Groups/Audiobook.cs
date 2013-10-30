using Komodex.DACP.Containers;
using Komodex.DACP.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Komodex.DACP.Groups
{
    public class Audiobook : DACPGroup
    {
        public Audiobook(DACPContainer container, DACPNodeDictionary nodes)
            : base(container, nodes)
        { }

        public string ArtistName { get; private set; }

        protected override void ProcessNodes(DACPNodeDictionary nodes)
        {
            base.ProcessNodes(nodes);

            ArtistName = nodes.GetString("asaa");
        }

        #region Episodes

        public Task<List<AudiobookEpisode>> GetEpisodesAsync()
        {
            DACPRequest request = Container.GetItemsRequest(ItemQuery);
            request.QueryParameters.Remove("sort");
            return Server.GetListAsync(request, n => new AudiobookEpisode(Container, n));
        }

        #endregion
    }
}
