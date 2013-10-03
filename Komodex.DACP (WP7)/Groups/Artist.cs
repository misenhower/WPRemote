using Komodex.DACP.Containers;
using Komodex.DACP.Items;
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
            string query;
            if (Server.SupportsPlayQueue)
                query = string.Format("('daap.songartistid:{0}'+'daap.songalbum!:'+('com.apple.itunes.extended-media-kind:1','com.apple.itunes.extended-media-kind:32'))", ArtistID);
            else
                query = string.Format("(('daap.songartist:{0}','daap.songalbumartist:{0}')+('com.apple.itunes.mediakind:1','com.apple.itunes.mediakind:32')+'daap.songalbum!:')", DACPUtility.EscapeSingleQuotes(Name));

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

        private string SongsQuery
        {
            get
            {
                if (Server.SupportsPlayQueue)
                    return string.Format("('daap.songartistid:{0}'+('com.apple.itunes.extended-media-kind:1','com.apple.itunes.extended-media-kind:32'))", ArtistID);
                return string.Format("(('daap.songartist:{0}','daap.songalbumartist:{0}')+('com.apple.itunes.mediakind:1','com.apple.itunes.mediakind:32'))", DACPUtility.EscapeSingleQuotes(Name));
            }
        }

        public async Task<bool> RequestSongsAsync()
        {
            DACPRequest request = Container.GetItemsRequest(SongsQuery);

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
                request = Database.GetPlayQueueEditRequest("add", string.Format("'daap.songartistid:{0}'", ArtistID), mode);
            else
                request = Database.GetCueSongRequest(SongsQuery, "album", 0);

            try { await Server.SubmitRequestAsync(request).ConfigureAwait(false); }
            catch { return false; }
            return true;
        }

        public async Task<bool> PlaySong(Song song, PlayQueueMode mode = PlayQueueMode.Replace)
        {
            DACPRequest request;
            if (Server.SupportsPlayQueue)
                request = Database.GetPlayQueueEditRequest("add", string.Format("'dmap.itemid:{0}'&queuefilter=artist:{1}", song.ID, ArtistID), mode);
            else
                request = Database.GetCueSongRequest(SongsQuery, "album", Songs.IndexOf(song));

            try { await Server.SubmitRequestAsync(request).ConfigureAwait(false); }
            catch { return false; }
            return true;
        }

        public async Task<bool> Shuffle()
        {
            DACPRequest request;
            if (Server.SupportsPlayQueue)
                request = Database.GetPlayQueueEditRequest("add", string.Format("'daap.songartistid:{0}'", ArtistID), PlayQueueMode.Shuffle);
            else
                request = Database.GetCueShuffleRequest(SongsQuery, "album");

            try { await Server.SubmitRequestAsync(request).ConfigureAwait(false); }
            catch { return false; }
            return true;
        }

        #endregion
    }
}
