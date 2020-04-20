using Komodex.DACP.Containers;
using Komodex.DACP.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Komodex.DACP.Groups
{
    public class iTunesUCourse : DacpGroup
    {
        public iTunesUCourse(DacpContainer container, DacpNodeDictionary nodes)
            : base(container, nodes)
        { }

        public string ArtistName { get; private set; }

        protected override void ProcessNodes(DacpNodeDictionary nodes)
        {
            base.ProcessNodes(nodes);

            ArtistName = nodes.GetString("asaa");
        }

        #region Episodes

        public Task<List<iTunesUEpisode>> GetEpisodesAsync()
        {
            DacpRequest request = Container.GetItemsRequest(ItemQuery);
            return Client.GetListAsync(request, n => new iTunesUEpisode(Container, n));
        }

        #endregion

        #region Unplayed Episodes

        public Task<List<iTunesUEpisode>> GetUnplayedEpisodesAsync()
        {
            DacpRequest request = Container.GetItemsRequest(UnplayedItemQuery);
            return Client.GetListAsync(request, n => new iTunesUEpisode(Container, n));
        }

        #endregion
    }
}
