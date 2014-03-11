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
    public class Album : DacpGroup
    {
        public Album(DacpContainer container, DacpNodeDictionary nodes)
            : base(container, nodes)
        { }

        public string ArtistName { get; private set; }

        protected override void ProcessNodes(DacpNodeDictionary nodes)
        {
            base.ProcessNodes(nodes);

            ArtistName = nodes.GetString("asaa");
        }

        internal override DacpQueryElement GroupQuery
        {
            get
            {
                var query = base.GroupQuery;
                if (Client.ServerSupportsPlayQueue)
                    return query;
                if (string.IsNullOrEmpty(ArtistName))
                    return query;
                return DacpQueryCollection.And(query, DacpQueryCollection.Or(DacpQueryPredicate.Is("daap.songartist", ArtistName), DacpQueryPredicate.Is("daap.songalbumartist", ArtistName)));
            }
        }

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
                request.QueryParameters["queuefilter"] = string.Format("album:{0}", PersistentID);
            }
            else
            {
                var songs = await GetSongsAsync();
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
