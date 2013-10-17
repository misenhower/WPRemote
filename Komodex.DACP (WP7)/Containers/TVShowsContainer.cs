using Komodex.DACP.Databases;
using Komodex.DACP.Groups;
using Komodex.DACP.Queries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Komodex.DACP.Containers
{
    public class TVShowsContainer : DACPContainer
    {
        public TVShowsContainer(DACPDatabase database, DACPNodeDictionary nodes)
            : base(database, nodes)
        { }

        protected override int[] MediaKinds
        {
            get { return new[] { 64 }; }
        }

        #region Shows

        private List<TVShow> _shows;
        private Dictionary<int, TVShow> _showsByID;

        public async Task<List<TVShow>> GetShowsAsync()
        {
            if (_shows != null)
                return _shows;

            _shows = null;
            _showsByID = null;

            var query = DACPQueryCollection.And(DACPQueryPredicate.IsNotEmpty("daap.songalbum"), MediaKindQuery);
            _shows = await GetGroupsAsync(query, n => new TVShow(this, n)).ConfigureAwait(false);
            if (_shows != null)
                _showsByID = _shows.ToDictionary(s => s.ID);

            return _shows;
        }

        public async Task<TVShow> GetShowByIDAsync(int showID)
        {
            if (_showsByID == null)
                await GetShowsAsync();
            if (_showsByID == null || !_showsByID.ContainsKey(showID))
                return null;

            return _showsByID[showID];
        }

        #endregion

        #region Unplayed Shows

        public Task<List<TVShow>> GetUnwatchedShowsAsync()
        {
            var query = DACPQueryCollection.And(DACPQueryPredicate.IsNotEmpty("daap.songalbum"), DACPQueryPredicate.Is("daap.songuserplaycount", 0), MediaKindQuery);
            return GetGroupsAsync(query, n => new TVShow(this, n));
        }

        #endregion
    }
}
