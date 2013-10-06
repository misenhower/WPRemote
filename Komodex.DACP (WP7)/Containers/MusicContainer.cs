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

        private Dictionary<int, Artist> _artists;
        public Dictionary<int, Artist> Artists
        {
            get { return _artists; }
            private set
            {
                if (_artists == value)
                    return;
                _artists = value;
                SendPropertyChanged();
            }
        }

        private GroupedItems<Artist> _groupedArtists;
        public GroupedItems<Artist> GroupedArtists
        {
            get { return _groupedArtists; }
            private set
            {
                if (_groupedArtists == value)
                    return;
                _groupedArtists = value;
                SendPropertyChanged();
            }
        }

        public async Task<bool> RequestArtistsAsync()
        {
            var query = DACPQueryCollection.And(DACPQueryPredicate.IsNotEmpty("daap.songartist"), MediaKindQuery);
            DACPRequest request = GetGroupsRequest("artists", query, true);

            try
            {
                var response = await Server.SubmitRequestAsync(request).ConfigureAwait(false);
                GroupedArtists = GroupedItems<Artist>.GetAlphaGroupedItems(response.Nodes, n => new Artist(this, n));
                Artists = GroupedArtists.SelectMany(l => l).ToDictionary(a => a.ID);
            }
            catch (Exception e)
            {
                Artists = null;
                GroupedArtists = null;
                Server.HandleHTTPException(request, e);
                return false;
            }

            return true;
        }

        public async Task<Artist> GetArtistByID(int artistID)
        {
            if (Artists == null)
            {
                bool success = await RequestArtistsAsync();
                if (!success)
                    return null;
            }

            if (!Artists.ContainsKey(artistID))
                return null;
            return Artists[artistID];
        }

        #endregion

        #region Albums

        private Dictionary<int, Album> _albums;
        public Dictionary<int, Album> Albums
        {
            get { return _albums; }
            private set
            {
                if (_albums == value)
                    return;
                _albums = value;
                SendPropertyChanged();
            }
        }

        private GroupedItems<Album> _groupedAlbums;
        public GroupedItems<Album> GroupedAlbums
        {
            get { return _groupedAlbums; }
            private set
            {
                if (_groupedAlbums == value)
                    return;
                _groupedAlbums = value;
                SendPropertyChanged();
            }
        }

        public async Task<bool> RequestAlbumsAsync()
        {
            var query = DACPQueryCollection.And(DACPQueryPredicate.IsNotEmpty("daap.songalbum"), MediaKindQuery);
            DACPRequest request = GetGroupsRequest("albums", query, true);

            try
            {
                var response = await Server.SubmitRequestAsync(request).ConfigureAwait(false);
                GroupedAlbums = GroupedItems<Album>.GetAlphaGroupedItems(response.Nodes, n => new Album(this, n));
                Albums = GroupedAlbums.SelectMany(l => l).ToDictionary(a => a.ID);
            }
            catch (Exception e)
            {
                Albums = null;
                GroupedAlbums = null;
                Server.HandleHTTPException(request, e);
                return false;
            }
            return true;
        }

        public async Task<Album> GetAlbumByID(int albumID)
        {
            if (Albums == null)
            {
                bool success = await RequestAlbumsAsync();
                if (!success)
                    return null;
            }

            if (!Albums.ContainsKey(albumID))
                return null;
            return Albums[albumID];
        }

        #endregion
    }
}
