using Komodex.Common;
using Komodex.DACP.Containers;
using Komodex.DACP.Items;
using Komodex.DACP.Queries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Komodex.DACP.Groups
{
    public class Artist : DacpGroup
    {
        public Artist(DacpContainer container, DacpNodeDictionary nodes)
            : base(container, nodes)
        { }

        public override string GroupType
        {
            get { return "artists"; }
        }

        /// <summary>
        /// iTunes 11 Artist ID (asri, daap.songartistid)
        /// </summary>
        public UInt64 ArtistID { get; private set; }

        protected override void ProcessNodes(DacpNodeDictionary nodes)
        {
            base.ProcessNodes(nodes);

            ArtistID = (UInt64)nodes.GetLong("asri");
        }

        internal override DacpQueryElement GroupQuery
        {
            get
            {
                if (Client.ServerSupportsPlayQueue)
                    return DacpQueryPredicate.Is("daap.songartistid", ArtistID);
                else
                    return DacpQueryCollection.Or(DacpQueryPredicate.Is("daap.songartist", Name), DacpQueryPredicate.Is("daap.songalbumartist", Name));
            }
        }

        #region Albums

        public Task<List<Album>> GetAlbumsAsync()
        {
            var query = DacpQueryCollection.And(GroupQuery, Container.GroupsQuery);
            DacpRequest request = Container.GetGroupsRequest(query);
            return Client.GetListAsync(request, n => new Album(Container, n));
        }

        #endregion

        #region Songs

        private List<Song> _songs;

        public async Task<List<Song>> GetSongsAsync()
        {
            if (_songs != null)
                return _songs;

            DacpRequest request = Container.GetItemsRequest(ItemQuery);
            _songs = await Client.GetListAsync(request, n => new Song(Container, n)).ConfigureAwait(false);
            return _songs;
        }

        public async Task<IDacpList> GetGroupedSongsAsync()
        {
            var songs = await GetSongsAsync().ConfigureAwait(false);
            if (songs == null || songs.Count == 0)
                return new DacpList<Song>(false);

            ItemGroup<Song> currentGroup = null;
            DacpList<ItemGroup<Song>> result = new DacpList<ItemGroup<Song>>(true);

            MusicContainer musicContainer = Container as MusicContainer;

            foreach (var song in songs)
            {
                if (currentGroup == null || currentGroup.Key != song.AlbumName)
                {
                    currentGroup = new ItemGroup<Song>(song.AlbumName);
                    if (musicContainer != null && song.AlbumPersistentID != 0)
                        currentGroup.KeyObject = await musicContainer.GetAlbumByPersistentIDAsync(song.AlbumPersistentID).ConfigureAwait(false);
                    result.Add(currentGroup);
                }
                currentGroup.Add(song);
            }

            if (result.Count == 0)
                return new DacpList<Song>(false);

            if (result.Count == 1 && string.IsNullOrEmpty(result[0].Key))
                return result[0].ToDacpList();

            return result;
        }

        #endregion

        #region Commands

        public async Task<bool> SendPlayCommandAsync(PlayQueueMode mode = PlayQueueMode.Replace)
        {
            DacpRequest request;
            if (Client.ServerSupportsPlayQueue)
                request = Database.GetPlayQueueEditRequest("add", GroupQuery, mode, "album");
            else
                request = Database.GetCueSongRequest(ItemQuery, "album", 0);

            try { await Client.SendRequestAsync(request).ConfigureAwait(false); }
            catch { return false; }
            return true;
        }

        public async Task<bool> SendPlaySongCommandAsync(Song song, PlayQueueMode mode = PlayQueueMode.Replace)
        {
            DacpRequest request;
            if (Client.ServerSupportsPlayQueue)
            {
                request = Database.GetPlayQueueEditRequest("add", DacpQueryPredicate.Is("dmap.itemid", song.ID), mode, "album");
                request.QueryParameters["queuefilter"] = string.Format("artist:{0}", ArtistID);
            }
            else
            {
                var songs = await GetSongsAsync().ConfigureAwait(false);
                int index = songs.FindIndex(s => s.ID == song.ID);
                if (index < 0)
                    return false;
                request = Database.GetCueSongRequest(ItemQuery, "album", index);
            }

            try { await Client.SendRequestAsync(request).ConfigureAwait(false); }
            catch { return false; }
            return true;
        }

        public async Task<bool> SendShuffleCommandAsync()
        {
            DacpRequest request;
            if (Client.ServerSupportsPlayQueue)
                request = Database.GetPlayQueueEditRequest("add", GroupQuery, PlayQueueMode.Shuffle, "album");
            else
                request = Database.GetCueShuffleRequest(ItemQuery, "album");

            try { await Client.SendRequestAsync(request).ConfigureAwait(false); }
            catch { return false; }
            return true;
        }

        #endregion
    }
}
