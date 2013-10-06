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

        private List<Album> _albums;
        public List<Album> Albums
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

        public async Task<bool> RequestAlbumsAsync()
        {
            var query = DACPQueryCollection.And(GroupQuery, DACPQueryPredicate.IsNotEmpty("daap.songalbum"), Container.MediaKindQuery);

            DACPRequest request = Container.GetGroupsRequest("albums", query, false);

            try
            {
                var response = await Server.SubmitRequestAsync(request).ConfigureAwait(false);
                Albums = DACPUtility.GetItemsFromNodes(response.Nodes, n => new Album(Container, n)).ToList();
            }
            catch (Exception e)
            {
                Albums = null;
                Server.HandleHTTPException(request, e);
                return false;
            }

            return true;
        }

        #endregion

        #region Songs

        private List<Song> _songs;
        public List<Song> Songs
        {
            get { return _songs; }
            private set
            {
                if (_songs == value)
                    return;
                _songs = value;
                SendPropertyChanged();
            }
        }

        public async Task<bool> RequestSongsAsync()
        {
            DACPRequest request = Container.GetItemsRequest(ItemQuery);

            try
            {
                var response = await Server.SubmitRequestAsync(request).ConfigureAwait(false);
                Songs = DACPUtility.GetItemsFromNodes(response.Nodes, n => new Song(Container, n)).ToList();
            }
            catch (Exception e)
            {
                Songs = null;
                Server.HandleHTTPException(request, e);
                return false;
            }

            return true;
        }

        #endregion

        #region Commands

        public async Task<bool> Play(PlayQueueMode mode = PlayQueueMode.Replace)
        {
            DACPRequest request;
            if (Server.SupportsPlayQueue)
                request = Database.GetPlayQueueEditRequest("add", GroupQuery, mode);
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
                request = Database.GetPlayQueueEditRequest("add", DACPQueryPredicate.Is("dmap.itemid", song.ID), mode);
                request.QueryParameters["queuefilter"] = string.Format("artist:{0}", ArtistID);
                request.QueryParameters["sort"] = "album";
            }
            else
            {
                request = Database.GetCueSongRequest(ItemQuery, "album", Songs.IndexOf(song));
            }

            try { await Server.SubmitRequestAsync(request).ConfigureAwait(false); }
            catch { return false; }
            return true;
        }

        public async Task<bool> Shuffle()
        {
            DACPRequest request;
            if (Server.SupportsPlayQueue)
                request = Database.GetPlayQueueEditRequest("add", GroupQuery, PlayQueueMode.Shuffle);
            else
                request = Database.GetCueShuffleRequest(ItemQuery, "album");

            try { await Server.SubmitRequestAsync(request).ConfigureAwait(false); }
            catch { return false; }
            return true;
        }

        #endregion
    }
}
