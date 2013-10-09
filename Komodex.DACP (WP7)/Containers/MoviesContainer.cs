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

        private List<Movie> _movies;

        public async Task<List<Movie>> GetMoviesAsync()
        {
            if (_movies != null)
                return _movies;

            _movies = await GetItemsAsync(MediaKindQuery, n => new Movie(this, n)).ConfigureAwait(false);
            return _movies;
        }

        #endregion

        #region Unplayed Movies

        public Task<List<Movie>> GetUnplayedMoviesAsync()
        {
            var query = DACPQueryCollection.And(DACPQueryPredicate.Is("daap.songuserplaycount", 0), MediaKindQuery);
            return GetItemsAsync(query, n => new Movie(this, n));
        }

        #endregion
    }
}
