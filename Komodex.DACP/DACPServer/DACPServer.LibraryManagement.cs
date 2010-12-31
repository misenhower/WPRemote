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
        public UInt64 DatabasePersistentID { get; protected set; }
        public int BasePlaylistID { get; protected set; }

        private List<Playlist> _LibraryPlaylists = null;
        public List<Playlist> LibraryPlaylists
        {
            get { return _LibraryPlaylists; }
            set
            {
                if (_LibraryPlaylists == value)
                    return;
                _LibraryPlaylists = value;
                SendPropertyChanged("LibraryPlaylists");
            }
        }

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

        private GroupedItems<Album> _LibraryAlbums = null;
        public GroupedItems<Album> LibraryAlbums
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

        private GroupedItems<MediaItem> _LibraryMovies = null;
        public GroupedItems<MediaItem> LibraryMovies
        {
            get { return _LibraryMovies; }
            protected set
            {
                if (_LibraryMovies == value)
                    return;
                _LibraryMovies = value;
                SendPropertyChanged("LibraryMovies");
            }
        }

        #endregion

        #region Requests and Responses

        #region Databases

        protected void SubmitDatabasesRequest()
        {
            string url = "/databases?session-id=" + SessionID;
            SubmitHTTPRequest(url, new HTTPResponseHandler(ProcessDatabasesResponse));
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
                        case "mper":
                            DatabasePersistentID = (UInt64)kvp.Value.GetInt64Value();
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
            SubmitHTTPRequest(url, new HTTPResponseHandler(ProcessPlaylistsResponse));
        }

        protected void ProcessPlaylistsResponse(HTTPRequestInfo requestInfo)
        {
            foreach (var kvp in requestInfo.ResponseNodes)
            {
                switch (kvp.Key)
                {
                    case "mlcl":
                        var playlists = new List<Playlist>();
                        var playlistNodes = Utility.GetResponseNodes(kvp.Value);
                        foreach (var playlistData in playlistNodes)
                        {
                            Playlist pl = new Playlist(this, playlistData.Value);
                            if (pl.BasePlaylist)
                            {
                                BasePlaylistID = pl.ID;
                            }
                            else if (pl.SpecialPlaylistType != 0)
                            {
                                // Handle special playlist
                            }
                            else
                            {
                                playlists.Add(pl);
                            }
                        }
                        LibraryPlaylists = playlists;
                        break;
                    default:
                        break;
                }
            }

            ConnectionEstablished();
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
            SubmitHTTPRequest(url, new HTTPResponseHandler(ProcessArtistsResponse), null, true);
        }

        protected void ProcessArtistsResponse(HTTPRequestInfo requestInfo)
        {
            LibraryArtists = GroupedItems<Artist>.HandleResponseNodes(requestInfo.ResponseNodes, bytes => new Artist(this, bytes));

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
            SubmitHTTPRequest(url, new HTTPResponseHandler(ProcessAlbumsResponse), null, true);
        }

        protected void ProcessAlbumsResponse(HTTPRequestInfo requestInfo)
        {
            LibraryAlbums = GroupedItems<Album>.HandleResponseNodes(requestInfo.ResponseNodes, bytes => new Album(this, bytes));

            retrievingAlbums = false;
        }

        #endregion

        #region Movies

        private bool retrievingMovies = false;

        public void GetMovies()
        {
            if (!retrievingMovies)
                SubmitMoviesRequest();
        }

        protected void SubmitMoviesRequest()
        {
            retrievingMovies = true;
            string url = "/databases/" + DatabaseID + "/containers/" + BasePlaylistID + "/items"
                + "?meta=dmap.itemname,dmap.itemid,daap.songartist,daap.songalbum,dmap.containeritemid,com.apple.itunes.has-video,daap.songtime,com.apple.itunes.content-rating"
                + "&type=music"
                + "&sort=name"
                + "&include-sort-headers=1"
                + "&query='com.apple.itunes.mediakind:2'"
                + "&session-id=" + SessionID;
            SubmitHTTPRequest(url, new HTTPResponseHandler(ProcessMoviesResponse), null, true);
        }

        protected void ProcessMoviesResponse(HTTPRequestInfo requestInfo)
        {
            LibraryMovies = GroupedItems<MediaItem>.HandleResponseNodes(requestInfo.ResponseNodes, bytes => new MediaItem(this, bytes));

            retrievingAlbums = false;
        }


        #endregion

        #endregion
    }
}
