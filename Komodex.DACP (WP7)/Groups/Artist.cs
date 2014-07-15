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
    public class Artist : DACPGroup
    {
        public Artist(DACPContainer container, DACPNodeDictionary nodes)
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

        protected override void ProcessNodes(DACPNodeDictionary nodes)
        {
            base.ProcessNodes(nodes);

            ArtistID = (UInt64)nodes.GetLong("asri");
        }

        internal override DACPQueryElement GroupQuery
        {
            get
            {
                if (Server.SupportsPlayQueue)
                    return DACPQueryPredicate.Is("daap.songartistid", ArtistID);
                else
                    return DACPQueryCollection.Or(DACPQueryPredicate.Is("daap.songartist", Name), DACPQueryPredicate.Is("daap.songalbumartist", Name));
            }
        }

        #region Albums

        public Task<List<Album>> GetAlbumsAsync()
        {
            var query = DACPQueryCollection.And(GroupQuery, Container.GroupsQuery);
            DACPRequest request = Container.GetGroupsRequest(query);
            return Server.GetListAsync(request, n => new Album(Container, n));
        }

        #endregion

        #region Songs

        private List<Song> _songs;

        public async Task<List<Song>> GetSongsAsync()
        {
            if (_songs != null)
                return _songs;

            DACPRequest request = Container.GetItemsRequest(ItemQuery);
            _songs = await Server.GetListAsync(request, n => new Song(Container, n)).ConfigureAwait(false);
            return _songs;
        }

        public async Task<IDACPList> GetGroupedSongsAsync()
        {
            var songs = await GetSongsAsync().ConfigureAwait(false);
            if (songs == null || songs.Count == 0)
                return new DACPList<Song>(false);

            ItemGroup<Song> currentGroup = null;
            DACPList<ItemGroup<Song>> result = new DACPList<ItemGroup<Song>>(true);

            foreach (var song in songs)
            {
                if (currentGroup == null || currentGroup.Key != song.AlbumName)
                {
                    currentGroup = new ItemGroup<Song>(song.AlbumName);
                    result.Add(currentGroup);
                }
                currentGroup.Add(song);
            }

            if (result.Count == 0)
                return new DACPList<Song>(false);

            if (result.Count == 1 && string.IsNullOrEmpty(result[0].Key))
                return result[0].ToDACPList();

            return result;
        }

        #endregion

        #region Commands

        public async Task<bool> Play(PlayQueueMode mode = PlayQueueMode.Replace)
        {
            DACPRequest request;
            if (Server.SupportsPlayQueue)
                request = Database.GetPlayQueueEditRequest("add", GroupQuery, mode, "album");
            else
                request = Database.GetCueSongRequest(ItemQuery, "album", 0);

            try { await Server.SubmitRequestAsync(request).ConfigureAwait(false); }
            catch { return false; }
            return true;
        }

        public async Task<bool> PlaySong(Song song, PlayQueueMode mode = PlayQueueMode.Replace)
        {
            DACPRequest request;
            if (Server.SupportsPlayQueue)
            {
                object songID;
                if (Server.IsAppleTV)
                    songID = song.PersistentID;
                else
                    songID = song.ID;

                request = Database.GetPlayQueueEditRequest("add", DACPQueryPredicate.Is("dmap.itemid", songID), mode, "album");
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

            try { await Server.SubmitRequestAsync(request).ConfigureAwait(false); }
            catch { return false; }
            return true;
        }

        public async Task<bool> Shuffle()
        {
            DACPRequest request;
            if (Server.SupportsPlayQueue)
                request = Database.GetPlayQueueEditRequest("add", GroupQuery, PlayQueueMode.Shuffle, "album");
            else
                request = Database.GetCueShuffleRequest(ItemQuery, "album");

            try { await Server.SubmitRequestAsync(request).ConfigureAwait(false); }
            catch { return false; }
            return true;
        }

        #endregion
    }
}
