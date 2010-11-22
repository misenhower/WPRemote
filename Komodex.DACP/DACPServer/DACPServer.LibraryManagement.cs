using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Linq;
using System.Collections.ObjectModel;
using Komodex.DACP.Library;
using System.Collections.Generic;

namespace Komodex.DACP
{
    public partial class DACPServer
    {
        #region Properties

        public int DatabaseID { get; protected set; }
        public int BasePlaylistID { get; protected set; }

        private GroupedItems<Artist> _LibraryArtists = null;
        public GroupedItems<Artist> LibraryArtists
        {
            get { return _LibraryArtists; }
            protected set
            {
                if (_LibraryArtists == value)
                    return;
                _LibraryArtists = value;
                SendPropertyChanged("LibraryArtists");
            }
        }

        private ObservableCollection<Album> _LibraryAlbums = null;
        public ObservableCollection<Album> LibraryAlbums
        {
            get { return _LibraryAlbums; }
            protected set
            {
                if (_LibraryAlbums == value)
                    return;
                _LibraryAlbums = value;
                SendPropertyChanged("LibraryAlbums");
            }
        }

        #endregion

        #region Requests and Responses

        #region Databases

        protected void SubmitDatabasesRequest()
        {
            string url = "/databases?session-id=" + SessionID;
            SubmitHTTPRequest(url);
        }

        protected void ProcessDatabasesResponse(HTTPRequestInfo requestInfo)
        {
            try
            {
                byte[] libraryBytes = requestInfo.ResponseNodes.First(rn => rn.Key == "mlcl").Value;
                var libraryNodes = Utility.GetResponseNodes(libraryBytes, true);
                byte[] firstLibraryBytes = libraryNodes[0].Value;
                var firstLibraryNodes = Utility.GetResponseNodes(firstLibraryBytes);

                foreach (var kvp in firstLibraryNodes)
                {
                    switch (kvp.Key)
                    {
                        case "miid":
                            DatabaseID = kvp.Value.GetInt32Value();
                            break;
                        default:
                            break;
                    }
                }

                SubmitPlaylistsRequest();
            }
            catch { }
        }

        #endregion

        #region Playlists

        protected void SubmitPlaylistsRequest()
        {
            string url = "/databases/" + DatabaseID + "/containers"
                + "?meta=dmap.itemname,dmap.itemcount,dmap.itemid,dmap.persistentid,daap.baseplaylist,com.apple.itunes.special-playlist,com.apple.itunes.smart-playlist,com.apple.itunes.saved-genius,dmap.parentcontainerid,dmap.editcommandssupported,com.apple.itunes.jukebox-current,daap.songcontentdescription"
                + "&session-id=" + SessionID;
            SubmitHTTPRequest(url);
        }

        protected void ProcessPlaylistsResponse(HTTPRequestInfo requestInfo)
        {
            foreach (var kvp in requestInfo.ResponseNodes)
            {
                switch (kvp.Key)
                {
                    case "mlcl":
                        // TODO: Retain playlist info
                        var playlistNodes = Utility.GetResponseNodes(kvp.Value);
                        foreach (var playlistData in playlistNodes)
                        {
                            Playlist pl = new Playlist(this, playlistData.Value);
                            if (pl.BasePlaylist)
                                BasePlaylistID = pl.ID;
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        #endregion

        #region Artists

        private bool retrievingArtists = false;

        public void GetArtists()
        {
            if (!retrievingArtists)
                SubmitArtistsRequest();
        }

        protected void SubmitArtistsRequest()
        {
            retrievingArtists = true;
            string url = "/databases/" + DatabaseID + "/groups"
                + "?meta=dmap.itemname,dmap.itemid,dmap.persistentid,daap.songartist,daap.groupalbumcount"
                + "&type=music"
                + "&group-type=artists"
                + "&sort=album"
                + "&include-sort-headers=1"
                + "&query=(('com.apple.itunes.mediakind:1','com.apple.itunes.mediakind:32')+'daap.songartist!:')"
                + "&session-id=" + SessionID;
            SubmitHTTPRequest(url);
        }

        protected void ProcessArtistsResponse(HTTPRequestInfo requestInfo)
        {
            List<Artist> artists = null;
            List<KeyValuePair<string, byte[]>> headers = null;

            foreach (var kvp in requestInfo.ResponseNodes)
            {
                switch (kvp.Key)
                {
                    case "mlcl": // Items list
                        artists = new List<Artist>();
                        var artistNodes = Utility.GetResponseNodes(kvp.Value);
                        foreach (var artistData in artistNodes)
                            artists.Add(new Artist(this, artistData.Value));
                        break;
                    case "mshl": // Headers
                        headers = Utility.GetResponseNodes(kvp.Value);
                        break;
                    default:
                        break;
                }
            }

            LibraryArtists = GroupedItems<Artist>.Parse(artists, headers);

            retrievingArtists = false;
        }

        #endregion

        #region Albums

        private bool retrievingAlbums = false;

        public void GetAlbums()
        {
            if (!retrievingAlbums)
                SubmitAlbumsRequest();
        }

        protected void SubmitAlbumsRequest()
        {
            retrievingAlbums = true;
            string url = "/databases/" + DatabaseID + "/groups"
                + "?meta=dmap.itemname,dmap.itemid,dmap.persistentid,daap.songartist,daap.songdatereleased,dmap.itemcount,daap.songtime,dmap.persistentid"
                + "&type=music"
                + "&group-type=albums"
                + "&sort=album"
                + "&include-sort-headers=1"
                + "&query=(('com.apple.itunes.mediakind:1','com.apple.itunes.mediakind:32')+'daap.songalbum!:')"
                + "&session-id=" + SessionID;
            SubmitHTTPRequest(url);
        }

        protected void ProcessAlbumsResponse(HTTPRequestInfo requestInfo)
        {
            foreach (var kvp in requestInfo.ResponseNodes)
            {
                switch (kvp.Key)
                {
                    case "mlcl":
                        ObservableCollection<Album> libraryAlbums = new ObservableCollection<Album>();

                        var albumNodes = Utility.GetResponseNodes(kvp.Value);
                        foreach (var albumData in albumNodes)
                        {
                            libraryAlbums.Add(new Album(this, albumData.Value));
                        }

                        LibraryAlbums = libraryAlbums;
                        break;
                    default:
                        break;
                }
            }
            retrievingAlbums = false;
        }

        #endregion

        #endregion
    }
}
