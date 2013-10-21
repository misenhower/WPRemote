using Komodex.DACP.Databases;
using Komodex.DACP.Items;
using Komodex.DACP.Queries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Komodex.DACP.Containers
{
    public class MoviesContainer : DACPContainer
    {
        public MoviesContainer(DACPDatabase database, DACPNodeDictionary nodes)
            : base(database, nodes)
        { }

        protected override int[] MediaKinds
        {
            get { return new[] { 2 }; }
        }

        #region Movies

        private IDACPList _movies;

        public async Task<IDACPList> GetMoviesAsync()
        {
            if (_movies != null)
                return _movies;

            DACPRequest request = GetItemsRequest(MediaKindQuery, "name", true);
            _movies = await Server.GetAlphaGroupedListAsync(request, n => new Movie(this, n));
            return _movies;
        }

        #endregion

        #region Unplayed Movies

        public Task<IDACPList> GetUnplayedMoviesAsync()
        {
            var query = DACPQueryCollection.And(DACPQueryPredicate.Is("daap.songuserplaycount", 0), MediaKindQuery);
            DACPRequest request = GetItemsRequest(query, "name", true);
            return Server.GetAlphaGroupedListAsync(request, n => new Movie(this, n));
        }

        #endregion
    }
}
