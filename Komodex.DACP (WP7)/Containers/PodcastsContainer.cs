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
    public class PodcastsContainer : DACPContainer
    {
        public PodcastsContainer(DACPDatabase database, DACPNodeDictionary nodes)
            : base(database, nodes)
        { }

        protected override int[] MediaKinds
        {
            get { return new[] { 4, 36, 6, 7 }; }
        }

        protected override string ItemsMeta
        {
            get { return base.ItemsMeta + ",daap.songdatereleased"; }
        }

        #region Shows

        private List<Podcast> _shows;
        private Dictionary<int, Podcast> _showsByID;

        public async Task<List<Podcast>> GetShowsAsync()
        {
            if (_shows != null)
                return _shows;

            _shows = null;
            _showsByID = null;

            _shows = await GetGroupsAsync(GroupsQuery, n => new Podcast(this, n)).ConfigureAwait(false);
            if (_shows != null)
                _showsByID = _shows.ToDictionary(s => s.ID);

            return _shows;
        }

        public async Task<Podcast> GetShowByIDAsync(int showID)
        {
            if (_showsByID == null)
                await GetShowsAsync();
            if (_showsByID == null || !_showsByID.ContainsKey(showID))
                return null;

            return _showsByID[showID];
        }

        #endregion

        #region Unplayed Shows

        public Task<List<Podcast>> GetUnplayedShowsAsync()
        {
            var query = DACPQueryCollection.And(DACPQueryPredicate.Is("daap.songuserplaycount", 0), GroupsQuery);
            return GetGroupsAsync(query, n => new Podcast(this, n));
        }

        #endregion
    }
}
