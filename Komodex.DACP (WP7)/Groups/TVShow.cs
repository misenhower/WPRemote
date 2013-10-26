using Komodex.DACP.Containers;
using Komodex.DACP.Databases;
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
        public TVShow(DACPContainer container, int index, string name, int seasonNumber, IEnumerable<TVShowEpisode> episodes)
            : base(container, null)
        {
            Index = index;
            Name = name;
            SeasonNumber = seasonNumber;
            ProcessEpisodes(episodes);
        }

        protected override void ProcessNodes(DACPNodeDictionary nodes)
        {
        }

        public int Index { get; private set; }
        public new string Name { get; private set; }
        public int SeasonNumber { get; private set; }

        protected internal override string GetAlbumArtURI(int width, int height)
        {
            if (Episodes == null || Episodes.Count == 0)
                return null;
            return Episodes[0].GetAlbumArtURI(width, height);
        }

        #region Episodes

        public List<TVShowEpisode> Episodes { get; private set; }

        protected void ProcessEpisodes(IEnumerable<TVShowEpisode> episodes)
        {
            Episodes = episodes.ToList();
        }

        public Task<List<TVShowEpisode>> GetEpisodesAsync()
        {
            return Task.FromResult(Episodes);
        }

        #endregion
    }
}
