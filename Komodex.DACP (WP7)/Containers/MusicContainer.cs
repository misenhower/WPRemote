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
    public class MusicContainer : DACPContainer
    {
        public MusicContainer(DACPDatabase database, DACPNodeDictionary nodes)
            : base(database, nodes)
        { }

        protected override int[] MediaKinds
        {
            get { return new[] { 1, 32 }; }
        }

        #region Artists

        private List<Artist> _artists;
        private Dictionary<int, Artist> _artistsByID;
        private IDACPList _groupedArtists;

        public async Task<List<Artist>> GetArtistsAsync()
        {
            if (_artists != null)
                return _artists;

            var query = DACPQueryCollection.And(DACPQueryPredicate.IsNotEmpty("daap.songartist"), MediaKindQuery);
            DACPRequest request = GetGroupsRequest(query, true, "artists");

            try
            {
                var response = await Server.SubmitRequestAsync(request).ConfigureAwait(false);
                _groupedArtists = DACPUtility.GetAlphaGroupedDACPList(response.Nodes, n => new Artist(this, n), out _artists);
                _artistsByID = _artists.ToDictionary(a => a.ID);
            }
            catch
            {
                _artists = new List<Artist>();
                _artistsByID = new Dictionary<int, Artist>();
                _groupedArtists = new DACPList<Artist>(false);
            }

            return _artists;
        }

        public async Task<IDACPList> GetGroupedArtistsAsync()
        {
            if (_groupedArtists == null)
                await GetArtistsAsync().ConfigureAwait(false);
            return _groupedArtists;
        }

        public async Task<Artist> GetArtistByIDAsync(int artistID)
        {
            if (_artistsByID == null)
                await GetArtistsAsync().ConfigureAwait(false);
            if (_artistsByID == null || !_artistsByID.ContainsKey(artistID))
                return null;

            return _artistsByID[artistID];
        }

        public Task<IDACPList> GetGenreArtistsAsync(string genreName)
        {
            var query = DACPQueryCollection.And(DACPQueryPredicate.Is("daap.songgenre", genreName), DACPQueryPredicate.IsNotEmpty("daap.songartist"), MediaKindQuery);
            DACPRequest request = GetGroupsRequest(query, true, "artists");
            return Server.GetAlphaGroupedListAsync(request, n => new Artist(this, n));
        }

        public async Task<List<Artist>> SearchArtistsAsync(string searchString, CancellationToken cancellationToken)
        {
            DACPQueryElement query = DACPQueryCollection.And(DACPQueryPredicate.Is("daap.songartist", searchString), DACPQueryPredicate.IsNotEmpty("daap.songartist"), MediaKindQuery);
            DACPRequest request = GetGroupsRequest(query, false, "artists");
            request.CancellationToken = cancellationToken;
            try
            {
                var response = await Server.SubmitRequestAsync(request).ConfigureAwait(false);
                return DACPUtility.GetItemsFromNodes(response.Nodes, n => new Artist(this, n)).ToList();
            }
            catch { return null; }
        }

        #endregion

        #region Albums

        private List<Album> _albums;
        private Dictionary<int, Album> _albumsByID;
        private IDACPList _groupedAlbums;

        public async Task<List<Album>> GetAlbumsAsync()
        {
            if (_albums != null)
                return _albums;

            DACPRequest request = GetGroupsRequest(GroupsQuery, true);

            try
            {
                var response = await Server.SubmitRequestAsync(request).ConfigureAwait(false);
                _groupedAlbums = DACPUtility.GetAlphaGroupedDACPList(response.Nodes, n => new Album(this, n), out _albums);
                _albumsByID = _albums.ToDictionary(a => a.ID);
            }
            catch
            {
                _albums = new List<Album>();
                _albumsByID = new Dictionary<int, Album>();
                _groupedAlbums = new DACPList<Album>(false);
            }

            return _albums;
        }

        public async Task<IDACPList> GetGroupedAlbumsAsync()
        {
            if (_groupedAlbums == null)
                await GetAlbumsAsync().ConfigureAwait(false);
            return _groupedAlbums;
        }

        public async Task<Album> GetAlbumByIDAsync(int albumID)
        {
            if (_albumsByID == null)
                await GetAlbumsAsync().ConfigureAwait(false);
            if (_albumsByID == null || !_albumsByID.ContainsKey(albumID))
                return null;

            return _albumsByID[albumID];
        }

        public Task<IDACPList> GetGenreAlbumsAsync(string genreName)
        {
            var query = DACPQueryCollection.And(DACPQueryPredicate.Is("daap.songgenre", genreName), DACPQueryPredicate.IsNotEmpty("daap.songalbum"), MediaKindQuery);
            DACPRequest request = GetGroupsRequest(query, true, "albums");
            return Server.GetAlphaGroupedListAsync(request, n => new Album(this, n));
        }

        public async Task<List<Album>> SearchAlbumsAsync(string searchString, CancellationToken cancellationToken)
        {
            DACPQueryElement query = DACPQueryCollection.And(DACPQueryPredicate.Is("daap.songalbum", searchString), DACPQueryPredicate.IsNotEmpty("daap.songalbum"), MediaKindQuery);
            DACPRequest request = GetGroupsRequest(query, false, "albums");
            request.CancellationToken = cancellationToken;
            try
            {
                var response = await Server.SubmitRequestAsync(request).ConfigureAwait(false);
                return DACPUtility.GetItemsFromNodes(response.Nodes, n => new Album(this, n)).ToList();
            }
            catch { return null; }
        }

        #endregion

        #region Songs

        public Task<IDACPList> GetGenreSongsAsync(string genreName)
        {
            var query = DACPQueryCollection.And(DACPQueryPredicate.Is("daap.songgenre", genreName), MediaKindQuery);
            DACPRequest request = GetItemsRequest(query, "name", true);
            return Server.GetAlphaGroupedListAsync(request, n => new Song(this, n));
        }

        public async Task<List<Song>> SearchSongsAsync(string searchString, CancellationToken cancellationToken)
        {
            DACPQueryElement query = DACPQueryCollection.And(DACPQueryPredicate.Is("dmap.itemname", searchString), MediaKindQuery);
            DACPRequest request = GetItemsRequest(query, "name", false);
            request.CancellationToken = cancellationToken;
            try
            {
                var response = await Server.SubmitRequestAsync(request).ConfigureAwait(false);
                return DACPUtility.GetItemsFromNodes(response.Nodes, n => new Song(this, n)).ToList();
            }
            catch { return null; }
        }

        #endregion
    }
}
