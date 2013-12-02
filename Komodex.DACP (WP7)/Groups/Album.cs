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
    public class Album : DACPGroup
    {
        public Album(DACPContainer container, DACPNodeDictionary nodes)
            : base(container, nodes)
        { }

        public string ArtistName { get; private set; }

        protected override void ProcessNodes(DACPNodeDictionary nodes)
        {
            base.ProcessNodes(nodes);

            ArtistName = nodes.GetString("asaa");
        }

        internal override DACPQueryElement GroupQuery
        {
            get
            {
                var query = base.GroupQuery;
                if (Server.SupportsPlayQueue)
                    return query;
                return DACPQueryCollection.And(query, DACPQueryCollection.Or(DACPQueryPredicate.Is("daap.songartist", ArtistName), DACPQueryPredicate.Is("daap.songalbumartist", ArtistName)));
            }
        }

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
                request = Database.GetPlayQueueEditRequest("add", DACPQueryPredicate.Is("dmap.itemid", song.ID), mode, "album");
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
