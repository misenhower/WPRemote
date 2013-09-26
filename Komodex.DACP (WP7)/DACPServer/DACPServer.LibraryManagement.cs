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
using Komodex.Common;

namespace Komodex.DACP
{
    public partial class DACPServer
    {
        #region Properties

        public int DatabaseID { get; protected set; }
        public UInt64 DatabasePersistentID { get; protected set; }
        public UInt64 ServiceID { get; protected set; }
        public Playlist BasePlaylist { get; protected set; }
        public Playlist MusicPlaylist { get; protected set; }
        public Playlist MoviesPlaylist { get; protected set; }
        public Playlist TVShowsPlaylist { get; protected set; }
        public Playlist PodcastsPlaylist { get; protected set; }

        private List<Playlist> _LibraryPlaylists = null;
        public List<Playlist> LibraryPlaylists
        {
            get { return _LibraryPlaylists; }
            set
            {
                if (_LibraryPlaylists == value)
                    return;
                _LibraryPlaylists = value;
                PropertyChanged.RaiseOnUIThread(this, "LibraryPlaylists");
            }
        }

        private List<Playlist> _LibraryGeniusMixes = null;
        public List<Playlist> LibraryGeniusMixes
        {
            get { return _LibraryGeniusMixes; }
            set
            {
                if (_LibraryGeniusMixes == value)
                    return;
                _LibraryGeniusMixes = value;
                PropertyChanged.RaiseOnUIThread(this, "LibraryGeniusMixes");
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
                PropertyChanged.RaiseOnUIThread(this, "LibraryArtists");
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
                PropertyChanged.RaiseOnUIThread(this, "LibraryAlbums");
            }
        }

        private GroupedItems<Genre> _LibraryGenres = null;
        public GroupedItems<Genre> LibraryGenres
        {
            get { return _LibraryGenres; }
            protected set
            {
                if (_LibraryGenres == value)
                    return;
                _LibraryGenres = value;
                PropertyChanged.RaiseOnUIThread(this, "LibraryGenres");
            }
        }

        private GroupedItems<VideoMediaItem> _LibraryMovies = null;
        public GroupedItems<VideoMediaItem> LibraryMovies
        {
            get { return _LibraryMovies; }
            protected set
            {
                if (_LibraryMovies == value)
                    return;
                _LibraryMovies = value;
                PropertyChanged.RaiseOnUIThread(this, "LibraryMovies");
            }
        }

        private List<GroupItems<VideoMediaItem>> _LibraryTVShows = null;
        public List<GroupItems<VideoMediaItem>> LibraryTVShows
        {
            get { return _LibraryTVShows; }
            protected set
            {
                if (_LibraryTVShows == value)
                    return;
                _LibraryTVShows = value;
                PropertyChanged.RaiseOnUIThread(this, "LibraryTVShows");
            }
        }

        private List<Podcast> _LibraryPodcasts = null;
        public List<Podcast> LibraryPodcasts
        {
            get { return _LibraryPodcasts; }
            protected set
            {
                if (_LibraryPodcasts == value)
                    return;
                _LibraryPodcasts = value;
                PropertyChanged.RaiseOnUIThread(this, "LibraryPodcasts");
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
            byte[] libraryBytes = requestInfo.ResponseNodes.First(rn => rn.Key == "mlcl").Value;
            var libraryNodes = DACPUtility.GetResponseNodes(libraryBytes).First();
            byte[] firstLibraryBytes = libraryNodes.Value;
            var firstLibraryNodes = DACPUtility.GetResponseNodes(firstLibraryBytes);

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
                    case "aeIM":
                        ServiceID = (UInt64)kvp.Value.GetInt64Value();
                        break;
                    default:
                        break;
                }
            }

            SubmitPlaylistsRequest();

            if (UseDelayedResponseRequests)
            {
                SubmitLibraryUpdateRequest();
                SubmitPlayStatusRequest();
            }

            // If we are using play queue requests, preload the list of artists.
            // There are a few places where the user can navigate to an artist page with just the artist name (e.g., Now Playing page, Album page, etc.).
            // Without this, the Artist IDs would not be available for requests from these pages.
            if (SupportsPlayQueue && (LibraryArtists == null || LibraryArtists.Count == 0))
                GetArtists();
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
                        var geniusMixes = new List<Playlist>();

                        var playlistNodes = DACPUtility.GetResponseNodes(kvp.Value);
                        foreach (var playlistData in playlistNodes)
                        {
                            Playlist pl = new Playlist(this, playlistData.Value);
                            if (pl.BasePlaylist)
                            {
                                BasePlaylist = pl;
                            }
                            else
                            {
                                switch (pl.SpecialPlaylistType)
                                {
                                    case 0: // Standard playlist
                                        playlists.Add(pl);
                                        break;

                                    case 1: // Podcasts
                                        PodcastsPlaylist = pl;
                                        break;

                                    case 4: // Movies
                                        MoviesPlaylist = pl;
                                        break;

                                    case 5: // TV Shows
                                        TVShowsPlaylist = pl;
                                        break;

                                    case 6: // Music Playlist
                                        MusicPlaylist = pl;
                                        break;

                                    case 16: // Genius mix
                                        geniusMixes.Add(pl);
                                        break;
                                }
                            }
                        }

                        LibraryPlaylists = playlists;
                        LibraryGeniusMixes = geniusMixes;
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
                + "?meta=dmap.itemname,dmap.itemid,dmap.persistentid,daap.songartist,daap.groupalbumcount,daap.songartistid"
                + "&type=music"
                + "&group-type=artists"
                + "&sort=album"
                + "&include-sort-headers=1"
                + "&query=(('com.apple.itunes.mediakind:1','com.apple.itunes.mediakind:32')+'daap.songartist!:')"
                + "&session-id=" + SessionID;
            SubmitHTTPRequest(url, new HTTPResponseHandler(ProcessArtistsResponse), true);
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
            SubmitHTTPRequest(url, new HTTPResponseHandler(ProcessAlbumsResponse), true);
        }

        protected void ProcessAlbumsResponse(HTTPRequestInfo requestInfo)
        {
            LibraryAlbums = GroupedItems<Album>.HandleResponseNodes(requestInfo.ResponseNodes, bytes => new Album(this, bytes));

            retrievingAlbums = false;
        }

        #endregion

        #region Genres

        private bool retrievingGenres = false;

        public void GetGenres()
        {
            if (!retrievingGenres)
                SubmitGenresRequest();
        }

        protected void SubmitGenresRequest()
        {
            retrievingGenres = true;
            string url = "/databases/" + DatabaseID + "/browse/genres"
                + "?include-sort-headers=1"
                + "&filter=('com.apple.itunes.mediakind:1','com.apple.itunes.mediakind:32')+'daap.songgenre!:'"
                + "&session-id=" + SessionID;
            SubmitHTTPRequest(url, ProcessGenresResponse, true);
        }

        protected void ProcessGenresResponse(HTTPRequestInfo requestInfo)
        {
            LibraryGenres = GroupedItems<Genre>.HandleResponseNodes(requestInfo.ResponseNodes, bytes => new Genre(this, bytes));

            retrievingGenres = false;
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
            string url = "/databases/" + DatabaseID + "/containers/" + BasePlaylist.ID + "/items"
                + "?meta=dmap.itemname,dmap.itemid,daap.songartist,daap.songalbum,dmap.containeritemid,com.apple.itunes.has-video,daap.songtime,com.apple.itunes.content-rating"
                + "&type=music"
                + "&sort=name"
                + "&include-sort-headers=1"
                + "&query='com.apple.itunes.mediakind:2'"
                + "&session-id=" + SessionID;
            SubmitHTTPRequest(url, new HTTPResponseHandler(ProcessMoviesResponse), true);
        }

        protected void ProcessMoviesResponse(HTTPRequestInfo requestInfo)
        {
            LibraryMovies = GroupedItems<VideoMediaItem>.HandleResponseNodes(requestInfo.ResponseNodes, bytes => new VideoMediaItem(this, bytes));

            retrievingMovies = false;
        }


        #endregion

        #region TV Shows

        private bool retrievingTVShows = false;

        public void GetTVShows()
        {
            if (!retrievingTVShows)
                SubmitTVShowsRequest();
        }

        protected void SubmitTVShowsRequest()
        {
            retrievingTVShows = true;
            string url = "/databases/" + DatabaseID + "/containers/" + BasePlaylist.ID + "/items"
                + "?meta=dmap.itemname,dmap.itemid,daap.songartist,daap.songalbum,dmap.containeritemid,com.apple.itunes.has-video,daap.songdisabled,com.apple.itunes.mediakind,daap.songtime,daap.songhasbeenplayed,daap.songdatereleased,daap.songdateadded,com.apple.itunes.series-name,daap.sortartist,daap.songalbum,com.apple.itunes.season-num,com.apple.itunes.episode-sort,com.apple.itunes.is-hd-video"
                + "&type=music"
                + "&sort=album"
                + "&query='com.apple.itunes.mediakind:64'"
                + "&session-id=" + SessionID;
            SubmitHTTPRequest(url, new HTTPResponseHandler(ProcessTVShowsResponse), true);
        }

        protected void ProcessTVShowsResponse(HTTPRequestInfo requestInfo)
        {
            List<GroupItems<VideoMediaItem>> result = new List<GroupItems<VideoMediaItem>>();

            Dictionary<string, GroupItems<VideoMediaItem>> groupsByKey = new Dictionary<string, GroupItems<VideoMediaItem>>();

            foreach (var kvp in requestInfo.ResponseNodes)
            {
                switch(kvp.Key)
                {
                    case "mlcl":
                        var itemNodes = DACPUtility.GetResponseNodes(kvp.Value);
                        foreach (var itemData in itemNodes)
                        {
                            // Parse the media item
                            var mediaItem = new VideoMediaItem(this, itemData.Value);

                            // Get the group for this show or create a new group
                            var group = groupsByKey.GetValueOrDefault(mediaItem.ShowName);
                            if (group == null)
                            {
                                group = new GroupItems<VideoMediaItem>(mediaItem.ShowName);
                                result.Add(group);
                                groupsByKey[mediaItem.ShowName] = group;
                            }

                            // Add the media item to the group
                            group.Add(mediaItem);
                        }
                        break;
                }
            }

            LibraryTVShows = result;

            retrievingTVShows = false;
        }

        #endregion

        #region Podcasts

        private bool retrievingPodcasts = false;

        public void GetPodcasts()
        {
            if (!retrievingPodcasts)
                SubmitPodcastsRequest();
        }

        protected void SubmitPodcastsRequest()
        {
            retrievingPodcasts = true;
            string url = "/databases/" + DatabaseID + "/groups"
                + "?meta=dmap.itemname,dmap.itemid,dmap.persistentid,daap.songartist,daap.songtime,daap.songhasbeenplayed,daap.songdatereleased,daap.sortartist,daap.songcontentdescription"
                + "&type=music"
                + "&group-type=albums"
                + "&sort=album"
                + "&include-sort-headers=0"
                + "&query=(('com.apple.itunes.mediakind:4','com.apple.itunes.mediakind:36','com.apple.itunes.mediakind:6','com.apple.itunes.mediakind:7')+'daap.songalbum!:')"
                + "&session-id=" + SessionID;
            SubmitHTTPRequest(url, new HTTPResponseHandler(ProcessPodcastsResponse), true);
        }

        protected void ProcessPodcastsResponse(HTTPRequestInfo requestInfo)
        {
            foreach (var kvp in requestInfo.ResponseNodes)
            {
                switch (kvp.Key)
                {
                    case "mlcl":
                        List<Podcast> podcasts = new List<Podcast>();

                        var podcastNodes = DACPUtility.GetResponseNodes(kvp.Value);
                        foreach (var podcastData in podcastNodes)
                            podcasts.Add(new Podcast(this, podcastData.Value));

                        LibraryPodcasts = podcasts;

                        break;
                    default:
                        break;
                }
            }

            retrievingPodcasts = false;
        }

        #endregion

        #endregion

        #region Album ID Persistence

        private Dictionary<UInt64, int> _AlbumIDs = new Dictionary<ulong, int>();
        internal Dictionary<UInt64, int> AlbumIDs
        {
            get { return _AlbumIDs; }
        }

        #endregion

        #region Artist ID Persistence

        private Dictionary<string, ulong> _artistIDs = new Dictionary<string, ulong>();
        internal Dictionary<string, ulong> ArtistIDs
        {
            get { return _artistIDs; }
        }

        #endregion
    }
}
