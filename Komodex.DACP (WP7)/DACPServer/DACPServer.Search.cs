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
using System.Collections.ObjectModel;
using System.Collections;
using System.Collections.Generic;
using Komodex.DACP.Library;
using Komodex.DACP.Localization;
using Komodex.Common;

namespace Komodex.DACP
{
    public partial class DACPServer
    {
        List<HTTPRequestInfo> currentSearchRequests = new List<HTTPRequestInfo>();

        #region Public Methods

        public ObservableCollection<SearchResultSet> GetSearchResults(string searchString)
        {
            string escapedSearchString = Uri.EscapeDataString(DACPUtility.EscapeSingleQuotes(searchString.Trim()));

            var searchResults = new ObservableCollection<SearchResultSet>();

            StopSearch();

            if (!string.IsNullOrEmpty(escapedSearchString))
            {
                searchResults.Add(SubmitAlbumSearchRequest(escapedSearchString));
                searchResults.Add(SubmitArtistSearchRequest(escapedSearchString));
                searchResults.Add(SubmitSongSearchRequest(escapedSearchString));
                searchResults.Add(SubmitMovieSearchRequest(escapedSearchString));
                searchResults.Add(SubmitPodcastSearchRequest(escapedSearchString));
            }

            return searchResults;
        }

        public void StopSearch()
        {
            foreach (HTTPRequestInfo requestInfo in currentSearchRequests)
            {
                lock (PendingHttpRequests)
                    PendingHttpRequests.Remove(requestInfo);

                try
                {
                    requestInfo.WebRequest.Abort();
                }
                catch { }
            }

            currentSearchRequests.Clear();

            UpdateGettingData();
        }

        #endregion

        #region Responses and Requests

        #region Albums

        protected SearchResultSet SubmitAlbumSearchRequest(string escapedSearchString)
        {
            SearchResultSet albumResults = new SearchResultSet(this, SearchResultsType.Albums);

            string url = "/databases/" + DatabaseID + "/groups"
                + "?meta=dmap.itemname,dmap.itemid,dmap.persistentid,daap.songartist,daap.songdatereleased,dmap.itemcount,daap.songtime,dmap.persistentid"
                + "&type=music"
                + "&group-type=albums"
                + "&sort=album"
                + "&include-sort-headers=1"
                + "&query=('daap.songalbum:*" + escapedSearchString + "*'+('com.apple.itunes.mediakind:1','com.apple.itunes.mediakind:32')+'daap.songalbum!:')"
                + "&session-id=" + SessionID;
            currentSearchRequests.Add(SubmitHTTPRequest(url, new HTTPResponseHandler(ProcessAlbumSearchResponse), true, r => r.ActionObject = albumResults));

            return albumResults;
        }

        protected void ProcessAlbumSearchResponse(HTTPRequestInfo requestInfo)
        {
            if (!currentSearchRequests.Contains(requestInfo))
                return;

            ProcessSearchResponse(requestInfo, bytes => new Album(this, bytes));
        }

        #endregion

        #region Artists

        protected SearchResultSet SubmitArtistSearchRequest(string escapedSearchString)
        {
            SearchResultSet artistResults = new SearchResultSet(this, SearchResultsType.Artists);

            string url = "/databases/" + DatabaseID + "/groups"
                + "?meta=dmap.itemname,dmap.itemid,dmap.persistentid,daap.songartist,daap.groupalbumcount,daap.songartistid"
                + "&type=music"
                + "&group-type=artists"
                + "&sort=album"
                + "&include-sort-headers=1"
                + "&query=('daap.songartist:*" + escapedSearchString + "*'+('com.apple.itunes.mediakind:1','com.apple.itunes.mediakind:32')+'daap.songartist!:')"
                + "&session-id=" + SessionID;
            currentSearchRequests.Add(SubmitHTTPRequest(url, new HTTPResponseHandler(ProcessArtistSearchResponse), true, r => r.ActionObject = artistResults));

            return artistResults;
        }

        protected void ProcessArtistSearchResponse(HTTPRequestInfo requestInfo)
        {
            if (!currentSearchRequests.Contains(requestInfo))
                return;

            ProcessSearchResponse(requestInfo, bytes => new Artist(this, bytes));
        }

        #endregion

        #region Songs

        protected SearchResultSet SubmitSongSearchRequest(string escapedSearchString)
        {
            string queryString = "('dmap.itemname:*" + escapedSearchString + "*'+('com.apple.itunes.mediakind:1','com.apple.itunes.mediakind:32'))";

            SearchResultSet songResults = new SearchResultSet(this, SearchResultsType.Songs, queryString);

            string url = "/databases/" + DatabaseID + "/containers/" + BasePlaylist.ID + "/items"
                + "?meta=dmap.itemname,dmap.itemid,daap.songartist,daap.songalbum,dmap.containeritemid,com.apple.itunes.has-video,daap.songdatereleased,dmap.itemcount,daap.songtime,dmap.persistentid,daap.songalbum"
                + "&type=music"
                + "&sort=name"
                + "&include-sort-headers=1"
                + "&query=" + queryString
                + "&session-id=" + SessionID;
            currentSearchRequests.Add(SubmitHTTPRequest(url, new HTTPResponseHandler(ProcessSongSearchResponse), true, r => r.ActionObject = songResults));

            return songResults;
        }

        protected void ProcessSongSearchResponse(HTTPRequestInfo requestInfo)
        {
            if (!currentSearchRequests.Contains(requestInfo))
                return;

            ProcessSearchResponse(requestInfo, bytes => new MediaItem(this, bytes));
        }

        #endregion

        #region Movies

        protected SearchResultSet SubmitMovieSearchRequest(string escapedSearchString)
        {
            string queryString = "('dmap.itemname:*" + escapedSearchString + "*'+'com.apple.itunes.mediakind:2')";

            SearchResultSet movieResults = new SearchResultSet(this, SearchResultsType.Movies, queryString);

            string url = "/databases/" + DatabaseID + "/containers/" + BasePlaylist.ID + "/items"
                + "?meta=dmap.itemname,dmap.itemid,daap.songartist,daap.songalbum,dmap.containeritemid,com.apple.itunes.has-video,daap.songtime,com.apple.itunes.content-rating"
                + "&type=music"
                + "&sort=name"
                + "&include-sort-headers=1"
                + "&query="+queryString
                + "&session-id=" + SessionID;
            currentSearchRequests.Add(SubmitHTTPRequest(url, new HTTPResponseHandler(ProcessMovieSearchResponse), true, r => r.ActionObject = movieResults));

            return movieResults;
        }

        protected void ProcessMovieSearchResponse(HTTPRequestInfo requestInfo)
        {
            if (!currentSearchRequests.Contains(requestInfo))
                return;

            ProcessSearchResponse(requestInfo, bytes => new VideoMediaItem(this, bytes));
        }

        #endregion

        #region Podcasts

        protected SearchResultSet SubmitPodcastSearchRequest(string escapedSearchString)
        {
            SearchResultSet podcastResults = new SearchResultSet(this, SearchResultsType.Podcasts);

            string url = "/databases/" + DatabaseID + "/groups"
                + "?meta=dmap.itemname,dmap.itemid,dmap.persistentid,daap.songartist,daap.songtime,daap.songhasbeenplayed,daap.songdatereleased,daap.sortartist,daap.songcontentdescription"
                + "&type=music"
                + "&group-type=albums"
                + "&sort=album"
                + "&include-sort-headers=1"
                + "&query=('daap.songalbum:*" + escapedSearchString + "*'+('com.apple.itunes.mediakind:4','com.apple.itunes.mediakind:36','com.apple.itunes.mediakind:6','com.apple.itunes.mediakind:7')+'daap.songalbum!:')"
                + "&session-id=" + SessionID;
            currentSearchRequests.Add(SubmitHTTPRequest(url, new HTTPResponseHandler(ProcessPodcastSearchResponse), true, r => r.ActionObject = podcastResults));

            return podcastResults;
        }

        protected void ProcessPodcastSearchResponse(HTTPRequestInfo requestInfo)
        {
            if (!currentSearchRequests.Contains(requestInfo))
                return;

            ProcessSearchResponse(requestInfo, bytes => new Podcast(this, bytes));
        }

        #endregion

        #endregion

        #region Methods

        protected void ProcessSearchResponse(HTTPRequestInfo requestInfo, Func<byte[], ILibraryElement> itemGenerator)
        {
            List<ILibraryElement> libraryElements = new List<ILibraryElement>();

            foreach (var kvp in requestInfo.ResponseNodes)
            {
                if (kvp.Key == "mlcl")
                {
                    libraryElements.Clear();
                    var itemNodes = DACPUtility.GetResponseNodes(kvp.Value);
                    foreach (var itemData in itemNodes)
                        libraryElements.Add(itemGenerator(itemData.Value));
                }
            }

            SearchResultSet searchResults = (SearchResultSet)requestInfo.ActionObject;

            Utility.BeginInvokeOnUIThread(() =>
            {
                foreach (ILibraryElement item in libraryElements)
                    searchResults.Add(item);
            });
        }

        #endregion

    }
}
