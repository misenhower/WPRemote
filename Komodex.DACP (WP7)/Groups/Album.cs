using Komodex.DACP.Containers;
using Komodex.DACP.Items;
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

        public override string GroupType
        {
            get { return "albums"; }
        }

        public string ArtistName { get; private set; }

        protected override void ProcessNodes(DACPNodeDictionary nodes)
        {
            base.ProcessNodes(nodes);

            ArtistName = nodes.GetString("asaa");
        }

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
                    return string.Format("(('com.apple.itunes.mediakind:1','com.apple.itunes.mediakind:32')+'daap.songalbumid:{0}')", PersistentID);
                return string.Format("(('daap.songartist:{0}','daap.songalbumartist:{0}')+('com.apple.itunes.mediakind:1','com.apple.itunes.mediakind:32')+'daap.songalbumid:" + PersistentID + "')", DACPUtility.EscapeSingleQuotes(ArtistName));
            }
        }

        public async Task<bool> RequestSongsAsync()
        {
            DACPRequest request = Container.GetItemsRequest(SongsQuery);

            try
            {
                var response = await Server.SubmitRequestAsync(request);
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
                request = Database.GetPlayQueueEditRequest("add", string.Format("'daap.songalbumid:{0}'", PersistentID), mode);
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
                request = Database.GetPlayQueueEditRequest("add", string.Format("'dmap.itemid:{0}'&queuefilter=album:{1}", song.ID, PersistentID), mode);
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
                request = Database.GetPlayQueueEditRequest("add", string.Format("'daap.songalbumid:{0}'", PersistentID), PlayQueueMode.Shuffle);
            else
                request = Database.GetCueShuffleRequest(SongsQuery, "album");

            try { await Server.SubmitRequestAsync(request).ConfigureAwait(false); }
            catch { return false; }
            return true;
        }

        #endregion
    }
}
