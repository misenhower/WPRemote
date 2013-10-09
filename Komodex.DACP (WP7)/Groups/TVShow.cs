using Komodex.DACP.Containers;
using Komodex.DACP.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Komodex.DACP.Groups
{
    public class TVShow : DACPGroup
    {
        public TVShow(DACPContainer container, DACPNodeDictionary nodes)
            : base(container, nodes)
        { }

        #region Episodes

        public Task<List<TVShowEpisode>> GetEpisodesAsync()
        {
            return GetItemsAsync(n => new TVShowEpisode(Container, n));
        }

        #endregion
    }
}
