using Komodex.Common;
using Komodex.DACP.Databases;
using Komodex.DACP.Groups;
using Komodex.DACP.Items;
using Komodex.DACP.Queries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Komodex.DACP.Containers
{
    public class TVShowsContainer : DacpContainer
    {
        public TVShowsContainer(DacpDatabase database, DacpNodeDictionary nodes)
            : base(database, nodes)
        { }

        protected override int[] MediaKinds
        {
            get { return new[] { 64 }; }
        }

        protected override string ItemsMeta
        {
            get { return base.ItemsMeta + ",daap.songdatereleased,com.apple.itunes.series-name,daap.sortartist,daap.sortalbum,daap.songalbum,com.apple.itunes.season-num,com.apple.itunes.episode-sort,com.apple.itunes.is-hd-video,com.apple.itunes.itms-songid,com.apple.itunes.has-chapter-data,com.apple.itunes.content-rating,com.apple.itunes.extended-media-kind"; }
        }

        #region Shows and Episodes

        private List<TVShow> _shows;
        private Dictionary<int, TVShow> _showsByID;
        private int _showCacheRevision;

        public async Task<List<TVShow>> GetShowsAsync()
        {
            if (_shows != null && _showCacheRevision == Client.CurrentLibraryUpdateNumber)
                return _shows;

            var episodes = await GetItemsAsync(MediaKindQuery, n => new TVShowEpisode(this, n));

            // Process episodes and sort them into TV shows manually
            int showIndex = 0;
            _shows = episodes.GroupBy(e => new { e.SeriesName, e.SeasonNumber }, (key, group) => new TVShow(this, showIndex++, key.SeriesName, key.SeasonNumber, group))
                .OrderBy(s => s.Name)
                .ThenBy(s => s.SeasonNumber)
                .ToList();
            _showsByID = _shows.ToDictionary(s => s.Index);

            _showCacheRevision = Client.CurrentLibraryUpdateNumber;

            return _shows;
        }

        public async Task<TVShow> GetShowByIDAsync(int showID)
        {
            await GetShowsAsync().ConfigureAwait(false);
            if (_showsByID == null || !_showsByID.ContainsKey(showID))
                return null;

            return _showsByID[showID];
        }

        #endregion

        #region Unwatched Shows

        public async Task<List<TVShow>> GetUnwatchedShowsAsync()
        {
            var shows = await GetShowsAsync();
            if (shows == null)
                return null;
            return shows.Where(s => s.Episodes.Any(e => e.PlayedState != ItemPlayedState.HasBeenPlayed)).ToList();
        }

        #endregion

        #region Show/Episode Search

        public async Task<IEnumerable<TVShow>> SearchShowsAsync(string searchString, CancellationToken cancellationToken)
        {
            var shows = await GetShowsAsync();
            if (shows == null)
                return null;

            searchString = searchString.Trim('*');
            return shows.Where(s => s.Name.Contains(searchString, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<IEnumerable<TVShowEpisode>> SearchEpisodesAsync(string searchString, CancellationToken cancellationToken)
        {
            var shows = await GetShowsAsync();
            if (shows == null)
                return null;

            searchString = searchString.Trim('*');
            var episodes = shows.SelectMany(s => s.Episodes);
            return episodes.Where(e => e.Name.Contains(searchString, StringComparison.OrdinalIgnoreCase));
        }

        #endregion
    }
}
