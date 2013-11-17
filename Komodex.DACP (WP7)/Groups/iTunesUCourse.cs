using Komodex.DACP.Containers;
using Komodex.DACP.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Komodex.DACP.Groups
{
    public class iTunesUCourse : DACPGroup
    {
        public iTunesUCourse(DACPContainer container, DACPNodeDictionary nodes)
            : base(container, nodes)
        { }

        public string ArtistName { get; private set; }

        protected override void ProcessNodes(DACPNodeDictionary nodes)
        {
            base.ProcessNodes(nodes);

            ArtistName = nodes.GetString("asaa");
        }

        #region Episodes

        public Task<List<iTunesUEpisode>> GetEpisodesAsync()
        {
            DACPRequest request = Container.GetItemsRequest(ItemQuery);
            return Server.GetListAsync(request, n => new iTunesUEpisode(Container, n));
        }

        #endregion

        #region Unplayed Episodes

        public Task<List<iTunesUEpisode>> GetUnplayedEpisodesAsync()
        {
            DACPRequest request = Container.GetItemsRequest(UnplayedItemQuery);
            return Server.GetListAsync(request, n => new iTunesUEpisode(Container, n));
        }

        #endregion
    }
}
