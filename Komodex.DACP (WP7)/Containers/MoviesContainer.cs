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
        private int _movieCacheRevision;

        public async Task<IDACPList> GetMoviesAsync()
        {
            if (_movies != null && _movieCacheRevision == Server.CurrentLibraryUpdateNumber)
                return _movies;

            DACPRequest request = GetItemsRequest(MediaKindQuery, "name", true);
            _movies = await Server.GetAlphaGroupedListAsync(request, n => new Movie(this, n));

            _movieCacheRevision = Server.CurrentLibraryUpdateNumber;

            return _movies;
        }

        #endregion

        #region Unwatched Movies

        public Task<IDACPList> GetUnwatchedMoviesAsync()
        {
            var query = DACPQueryCollection.And(DACPQueryPredicate.Is("daap.songuserplaycount", 0), MediaKindQuery);
            DACPRequest request = GetItemsRequest(query, "name", true);
            return Server.GetAlphaGroupedListAsync(request, n => new Movie(this, n));
        }

        #endregion

        #region Movies by Genre

        public Task<IDACPList> GetGenreMoviesAsync(string genreName)
        {
            var query = DACPQueryCollection.And(DACPQueryPredicate.Is("daap.songgenre", genreName), MediaKindQuery);
            DACPRequest request = GetItemsRequest(query, "name", true);
            return Server.GetAlphaGroupedListAsync(request, n => new Movie(this, n));
        }

        #endregion

        #region Movie Search

        public async Task<List<Movie>> SearchMoviesAsync(string searchString, CancellationToken cancellationToken)
        {
            DACPQueryElement query = DACPQueryCollection.And(DACPQueryPredicate.Is("dmap.itemname", searchString), MediaKindQuery);
            DACPRequest request = GetItemsRequest(query, "name", false);
            request.CancellationToken = cancellationToken;
            try
            {
                var response = await Server.SubmitRequestAsync(request).ConfigureAwait(false);
                return DACPUtility.GetItemsFromNodes(response.Nodes, n => new Movie(this, n)).ToList();
            }
            catch { return null; }
        }

        #endregion
    }
}
