using Komodex.DACP.Databases;
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
    public class MoviesContainer : DacpContainer
    {
        public MoviesContainer(DacpDatabase database, DacpNodeDictionary nodes)
            : base(database, nodes)
        { }

        protected override int[] MediaKinds
        {
            get { return new[] { 2 }; }
        }

        #region Movies

        private IDacpList _movies;
        private int _movieCacheRevision;

        public async Task<IDacpList> GetMoviesAsync()
        {
            if (_movies != null && _movieCacheRevision == Client.CurrentLibraryUpdateNumber)
                return _movies;

            DacpRequest request = GetItemsRequest(MediaKindQuery, "name", true);
            _movies = await Client.GetAlphaGroupedListAsync(request, n => new Movie(this, n));

            _movieCacheRevision = Client.CurrentLibraryUpdateNumber;

            return _movies;
        }

        #endregion

        #region Unwatched Movies

        public Task<IDacpList> GetUnwatchedMoviesAsync()
        {
            var query = DacpQueryCollection.And(DacpQueryPredicate.Is("daap.songuserplaycount", 0), MediaKindQuery);
            DacpRequest request = GetItemsRequest(query, "name", true);
            return Client.GetAlphaGroupedListAsync(request, n => new Movie(this, n));
        }

        #endregion

        #region Movies by Genre

        public Task<IDacpList> GetGenreMoviesAsync(string genreName)
        {
            var query = DacpQueryCollection.And(DacpQueryPredicate.Is("daap.songgenre", genreName), MediaKindQuery);
            DacpRequest request = GetItemsRequest(query, "name", true);
            return Client.GetAlphaGroupedListAsync(request, n => new Movie(this, n));
        }

        #endregion

        #region Movie Search

        public async Task<List<Movie>> SearchMoviesAsync(string searchString, CancellationToken cancellationToken)
        {
            DacpQueryElement query = DacpQueryCollection.And(DacpQueryPredicate.Is("dmap.itemname", searchString), MediaKindQuery);
            DacpRequest request = GetItemsRequest(query, "name", false);
            request.CancellationToken = cancellationToken;
            try
            {
                var response = await Client.SendRequestAsync(request).ConfigureAwait(false);
                return DacpUtility.GetItemsFromNodes(response.Nodes, n => new Movie(this, n)).ToList();
            }
            catch { return null; }
        }

        #endregion
    }
}
