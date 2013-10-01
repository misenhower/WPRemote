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

        public async Task<bool> RequestAlbumsAsync()
        {
            string query = "('daap.songartistid:" + PersistentID + "'+'daap.songalbum!:'+('com.apple.itunes.extended-media-kind:1','com.apple.itunes.extended-media-kind:32'))";
            DACPRequest request = Container.GetGroupsRequest("albums", query, false);

            try
            {
                var response = await Server.SubmitRequestAsync(request).ConfigureAwait(false);
                Albums = DACPUtility.GetItemsFromNodes(response.Nodes, n => new Album(Container, n)).ToDictionary(a => a.ID);
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

        private Dictionary<int, Song> _songs;
        public Dictionary<int, Song> Songs
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
            string query = "('daap.songartistid:" + PersistentID + "'+('com.apple.itunes.extended-media-kind:1','com.apple.itunes.extended-media-kind:32'))";
            DACPRequest request = Container.GetItemsRequest(query);

            try
            {
                var response = await Server.SubmitRequestAsync(request).ConfigureAwait(false);
                Songs = DACPUtility.GetItemsFromNodes(response.Nodes, n => new Song(Container, n)).ToDictionary(s => s.ID);
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
    }
}
