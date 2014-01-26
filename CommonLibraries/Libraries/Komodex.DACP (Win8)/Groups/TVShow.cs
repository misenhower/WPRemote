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
    public class TVShow : DacpGroup
    {
        public TVShow(DacpContainer container, int index, string name, int seasonNumber, IEnumerable<TVShowEpisode> episodes)
            : base(container, null)
        {
            Index = index;
            Name = name;
            SeasonNumber = seasonNumber;
            ProcessEpisodes(episodes);
        }

        protected override void ProcessNodes(DacpNodeDictionary nodes)
        {
        }

        public int Index { get; private set; }
        public new string Name { get; private set; }
        public int SeasonNumber { get; private set; }

        public override string ArtworkUriFormat
        {
            get
            {
                if (Episodes == null || Episodes.Count == 0)
                    return null;
                return Episodes[0].ArtworkUriFormat;
            }
        }

        #region Episodes

        public List<TVShowEpisode> Episodes { get; private set; }
        public IDacpList GroupedEpisodes { get; private set; }
        public IDacpList GroupedUnwatchedEpisodes { get; private set; }

        protected void ProcessEpisodes(IEnumerable<TVShowEpisode> episodes)
        {
            Episodes = episodes.OrderBy(e => e.EpisodeNumber).ToList();

            GroupedEpisodes = GetGroupedEpisodes(Episodes);
            GroupedUnwatchedEpisodes = GetGroupedEpisodes(Episodes.Where(e => e.PlayedState != ItemPlayedState.HasBeenPlayed));
        }

        private IDacpList GetGroupedEpisodes(IEnumerable<TVShowEpisode> episodes)
        {
            ItemGroup<TVShowEpisode> currentGroup = null;
            DacpList<ItemGroup<TVShowEpisode>> result = new DacpList<ItemGroup<TVShowEpisode>>(true);

            foreach (var episode in episodes)
            {
                if (currentGroup == null || currentGroup.Key != episode.AlbumName)
                {
                    currentGroup = new ItemGroup<TVShowEpisode>(episode.AlbumName);
                    result.Add(currentGroup);
                }
                currentGroup.Add(episode);
            }

            if (result.Count == 0)
                return new DacpList<TVShowEpisode>(false);

            if (result.Count == 1 && string.IsNullOrEmpty(result[0].Key))
                return result[0].ToDacpList();

            return result;
        }

        #endregion
    }
}
