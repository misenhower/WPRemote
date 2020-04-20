using Komodex.DACP.Containers;
using Komodex.DACP.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Komodex.DACP.Groups
{
    public class Podcast : DacpGroup
    {
        public Podcast(DacpContainer container, DacpNodeDictionary nodes)
            : base(container, nodes)
        { }

        public string ArtistName { get; private set; }

        protected override void ProcessNodes(DacpNodeDictionary nodes)
        {
            base.ProcessNodes(nodes);

            ArtistName = nodes.GetString("asaa");
        }

        #region Episodes

        public Task<List<PodcastEpisode>> GetEpisodesAsync()
        {
            DacpRequest request = Container.GetItemsRequest(ItemQuery);
            request.QueryParameters["sort"] = "releasedate";
            request.QueryParameters["invert-sort-order"] = "1";
            return Client.GetListAsync(request, n => new PodcastEpisode(Container, n));
        }

        #endregion

        #region Unplayed Episodes

        public Task<List<PodcastEpisode>> GetUnplayedEpisodesAsync()
        {
            DacpRequest request = Container.GetItemsRequest(UnplayedItemQuery);
            request.QueryParameters["sort"] = "releasedate";
            request.QueryParameters["invert-sort-order"] = "1";
            return Client.GetListAsync(request, n => new PodcastEpisode(Container, n));
        }

        #endregion
    }
}
