using Komodex.DACP.Containers;
using Komodex.DACP.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Komodex.DACP.Groups
{
    public class Audiobook : DacpGroup
    {
        public Audiobook(DacpContainer container, DacpNodeDictionary nodes)
            : base(container, nodes)
        { }

        public string ArtistName { get; private set; }

        protected override void ProcessNodes(DacpNodeDictionary nodes)
        {
            base.ProcessNodes(nodes);

            ArtistName = nodes.GetString("asaa");
        }

        #region Episodes

        public Task<List<AudiobookEpisode>> GetEpisodesAsync()
        {
            DacpRequest request = Container.GetItemsRequest(ItemQuery);
            request.QueryParameters.Remove("sort");
            return Client.GetListAsync(request, n => new AudiobookEpisode(Container, n));
        }

        #endregion
    }
}
