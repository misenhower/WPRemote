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
    public class MusicContainer : DacpContainer
    {
        public MusicContainer(DacpDatabase database, DacpNodeDictionary nodes)
            : base(database, nodes)
        { }

        protected override int[] MediaKinds
        {
            get { return new[] { 1, 32 }; }
        }

        protected override string ItemsMeta
        {
            get { return base.ItemsMeta + ",daap.songcodectype,daap.songbitrate,daap.songtracknumber"; }
        }

        #region Artists

        private List<Artist> _artists;
        private Dictionary<int, Artist> _artistsByID;
        private IDacpList _groupedArtists;
        private int _artistCacheRevision;

        public async Task<List<Artist>> GetArtistsAsync()
        {
            if (_artists != null && _artistCacheRevision == Client.CurrentLibraryUpdateNumber)
                return _artists;

            var query = DacpQueryCollection.And(DacpQueryPredicate.IsNotEmpty("daap.songartist"), MediaKindQuery);
            DacpRequest request = GetGroupsRequest(query, true, "artists");

            try
            {
                var response = await Client.SendRequestAsync(request).ConfigureAwait(false);
                _groupedArtists = DacpUtility.GetAlphaGroupedDacpList(response.Nodes, n => new Artist(this, n), out _artists);
                _artistsByID = _artists.ToDictionary(a => a.ID);
            }
            catch
            {
                _artists = new List<Artist>();
                _artistsByID = new Dictionary<int, Artist>();
                _groupedArtists = new DacpList<Artist>(false);
            }

            _artistCacheRevision = Client.CurrentLibraryUpdateNumber;

            return _artists;
        }

        public async Task<IDacpList> GetGroupedArtistsAsync()
        {
            await GetArtistsAsync().ConfigureAwait(false);
            return _groupedArtists;
        }

        public async Task<Artist> GetArtistByIDAsync(int artistID)
        {
            await GetArtistsAsync().ConfigureAwait(false);
            if (_artistsByID == null || !_artistsByID.ContainsKey(artistID))
                return null;

            return _artistsByID[artistID];
        }

        public Task<IDacpList> GetGenreArtistsAsync(string genreName)
        {
            var query = DacpQueryCollection.And(DacpQueryPredicate.Is("daap.songgenre", genreName), DacpQueryPredicate.IsNotEmpty("daap.songartist"), MediaKindQuery);
            DacpRequest request = GetGroupsRequest(query, true, "artists");
            return Client.GetAlphaGroupedListAsync(request, n => new Artist(this, n));
        }

        public async Task<List<Artist>> SearchArtistsAsync(string searchString, CancellationToken cancellationToken)
        {
            DacpQueryElement query = DacpQueryCollection.And(DacpQueryPredicate.Is("daap.songartist", searchString), DacpQueryPredicate.IsNotEmpty("daap.songartist"), MediaKindQuery);
            DacpRequest request = GetGroupsRequest(query, false, "artists");
            request.CancellationToken = cancellationToken;
            try
            {
                var response = await Client.SendRequestAsync(request).ConfigureAwait(false);
                return DacpUtility.GetItemsFromNodes(response.Nodes, n => new Artist(this, n)).ToList();
            }
            catch { return null; }
        }

        #endregion

        #region Albums

        private List<Album> _albums;
        private Dictionary<int, Album> _albumsByID;
        private IDacpList _groupedAlbums;
        private int _albumCacheRevision;

        public async Task<List<Album>> GetAlbumsAsync()
        {
            if (_albums != null && _albumCacheRevision == Client.CurrentLibraryUpdateNumber)
                return _albums;

            DacpRequest request = GetGroupsRequest(GroupsQuery, true);

            try
            {
                var response = await Client.SendRequestAsync(request).ConfigureAwait(false);
                _groupedAlbums = DacpUtility.GetAlphaGroupedDacpList(response.Nodes, n => new Album(this, n), out _albums);
                _albumsByID = _albums.ToDictionary(a => a.ID);
            }
            catch
            {
                _albums = new List<Album>();
                _albumsByID = new Dictionary<int, Album>();
                _groupedAlbums = new DacpList<Album>(false);
            }

            _albumCacheRevision = Client.CurrentLibraryUpdateNumber;

            return _albums;
        }

        public async Task<IDacpList> GetGroupedAlbumsAsync()
        {
            await GetAlbumsAsync().ConfigureAwait(false);
            return _groupedAlbums;
        }

        public async Task<Album> GetAlbumByIDAsync(int albumID)
        {
            await GetAlbumsAsync().ConfigureAwait(false);
            if (_albumsByID == null || !_albumsByID.ContainsKey(albumID))
                return null;

            return _albumsByID[albumID];
        }

        public Task<IDacpList> GetGenreAlbumsAsync(string genreName)
        {
            var query = DacpQueryCollection.And(DacpQueryPredicate.Is("daap.songgenre", genreName), DacpQueryPredicate.IsNotEmpty("daap.songalbum"), MediaKindQuery);
            DacpRequest request = GetGroupsRequest(query, true, "albums");
            return Client.GetAlphaGroupedListAsync(request, n => new Album(this, n));
        }

        public async Task<List<Album>> SearchAlbumsAsync(string searchString, CancellationToken cancellationToken)
        {
            DacpQueryElement query = DacpQueryCollection.And(DacpQueryPredicate.Is("daap.songalbum", searchString), DacpQueryPredicate.IsNotEmpty("daap.songalbum"), MediaKindQuery);
            DacpRequest request = GetGroupsRequest(query, false, "albums");
            request.CancellationToken = cancellationToken;
            try
            {
                var response = await Client.SendRequestAsync(request).ConfigureAwait(false);
                return DacpUtility.GetItemsFromNodes(response.Nodes, n => new Album(this, n)).ToList();
            }
            catch { return null; }
        }

        #endregion

        #region Songs

        public Task<IDacpList> GetGenreSongsAsync(string genreName)
        {
            var query = DacpQueryCollection.And(DacpQueryPredicate.Is("daap.songgenre", genreName), MediaKindQuery);
            DacpRequest request = GetItemsRequest(query, "name", true);
            return Client.GetAlphaGroupedListAsync(request, n => new Song(this, n));
        }

        public async Task<List<Song>> SearchSongsAsync(string searchString, CancellationToken cancellationToken)
        {
            DacpQueryElement query = DacpQueryCollection.And(DacpQueryPredicate.Is("dmap.itemname", searchString), MediaKindQuery);
            DacpRequest request = GetItemsRequest(query, "name", false);
            request.CancellationToken = cancellationToken;
            try
            {
                var response = await Client.SendRequestAsync(request).ConfigureAwait(false);
                return DacpUtility.GetItemsFromNodes(response.Nodes, n => new Song(this, n)).ToList();
            }
            catch { return null; }
        }

        #endregion

        #region Commands

        public async Task<bool> ShuffleAllSongsAsync()
        {
            DacpRequest request;

            if (Client.ServerSupportsPlayQueue)
            {
                request = Database.GetPlayQueueEditRequest("add", DacpQueryPredicate.Is("dmap.itemid", ID), PlayQueueMode.Shuffle, "name");
                request.QueryParameters["query-modifier"] = "containers";
            }
            else
            {
                request = Database.GetCueShuffleRequest(MediaKindQuery, "artist");
            }

            try { await Client.SendRequestAsync(request).ConfigureAwait(false); }
            catch { return false; }
            return true;
        }

        #endregion
    }
}
