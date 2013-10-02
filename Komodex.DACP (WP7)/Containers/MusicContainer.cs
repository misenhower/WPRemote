﻿using Komodex.DACP.Databases;
using Komodex.DACP.Groups;
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
            string query = "(('com.apple.itunes.mediakind:1','com.apple.itunes.mediakind:32')+'daap.songartist!:')";
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
            string query = "(('com.apple.itunes.mediakind:1','com.apple.itunes.mediakind:32')+'daap.songalbum!:')";
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

        #endregion
    }
}
