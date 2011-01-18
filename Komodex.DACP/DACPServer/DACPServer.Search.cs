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

namespace Komodex.DACP
{
    public partial class DACPServer
    {
        private readonly string searchResultAlbumHeaderText = "albums";
        private readonly string searchResultArtistHeaderText = "artists";
        private readonly string searchResultSongHeaderText = "songs";

        #region Public Methods

        public ObservableCollection<SearchResultSet> GetSearchResults(string searchString)
        {
            string escapedSearchString = Uri.EscapeDataString(Utility.EscapeSingleQuotes(searchString.Trim()));

            var searchResults = new ObservableCollection<SearchResultSet>();

            StopSearch();

            if (!string.IsNullOrEmpty(escapedSearchString))
            {
                searchResults.Add(SubmitAlbumSearchRequest(escapedSearchString));
                searchResults.Add(SubmitArtistSearchRequest(escapedSearchString));
                searchResults.Add(SubmitSongSearchRequest(escapedSearchString));
            }

            return searchResults;
        }

        public void StopSearch()
        {
            if (albumSearchRequestInfo != null)
            {
                albumSearchRequestInfo.WebRequest.Abort();
                albumSearchRequestInfo = null;
            }

            if (artistSearchRequestInfo != null)
            {
                artistSearchRequestInfo.WebRequest.Abort();
                artistSearchRequestInfo = null;
            }

            if (songSearchRequestInfo != null)
            {
                songSearchRequestInfo.WebRequest.Abort();
                songSearchRequestInfo = null;
            }

            UpdateGettingData();
        }

        #endregion

        #region Responses and Requests

        #region Albums

        private HTTPRequestInfo albumSearchRequestInfo = null;

        protected SearchResultSet SubmitAlbumSearchRequest(string escapedSearchString)
        {
            SearchResultSet albumResults = new SearchResultSet(this, searchResultAlbumHeaderText);

            string url = "/databases/" + DatabaseID + "/groups"
                + "?meta=dmap.itemname,dmap.itemid,dmap.persistentid,daap.songartist,daap.songdatereleased,dmap.itemcount,daap.songtime,dmap.persistentid"
                + "&type=music"
                + "&group-type=albums"
                + "&sort=album"
                + "&include-sort-headers=1"
                + "&query=('daap.songalbum:*" + escapedSearchString + "*'+('com.apple.itunes.mediakind:1','com.apple.itunes.mediakind:32')+'daap.songalbum!:')"
                + "&session-id=" + SessionID;
            albumSearchRequestInfo = SubmitHTTPRequest(url, new HTTPResponseHandler(ProcessAlbumSearchResponse), true, r => r.ActionObject = albumResults);

            return albumResults;
        }

        protected void ProcessAlbumSearchResponse(HTTPRequestInfo requestInfo)
        {
            if (requestInfo != albumSearchRequestInfo)
                return;

            ProcessSearchResponse(requestInfo, bytes => new Album(this, bytes));
        }

        #endregion

        #region Artists

        private HTTPRequestInfo artistSearchRequestInfo = null;

        protected SearchResultSet SubmitArtistSearchRequest(string escapedSearchString)
        {
            SearchResultSet artistResults = new SearchResultSet(this, searchResultArtistHeaderText);

            string url = "/databases/" + DatabaseID + "/groups"
                + "?meta=dmap.itemname,dmap.itemid,dmap.persistentid,daap.songartist,daap.groupalbumcount"
                + "&type=music"
                + "&group-type=artists"
                + "&sort=album"
                + "&include-sort-headers=1"
                + "&query=('daap.songartist:*" + escapedSearchString + "*'+('com.apple.itunes.mediakind:1','com.apple.itunes.mediakind:32')+'daap.songartist!:')"
                + "&session-id=" + SessionID;
            artistSearchRequestInfo = SubmitHTTPRequest(url, new HTTPResponseHandler(ProcessArtistSearchResponse), true, r => r.ActionObject = artistResults);

            return artistResults;
        }

        protected void ProcessArtistSearchResponse(HTTPRequestInfo requestInfo)
        {
            if (requestInfo != artistSearchRequestInfo)
                return;

            ProcessSearchResponse(requestInfo, bytes => new Artist(this, bytes));
        }

        #endregion

        #region Songs

        private HTTPRequestInfo songSearchRequestInfo = null;

        protected SearchResultSet SubmitSongSearchRequest(string escapedSearchString)
        {
            string queryString = "('dmap.itemname:*" + escapedSearchString + "*'+('com.apple.itunes.mediakind:1','com.apple.itunes.mediakind:32'))";

            SearchResultSet songResults = new SearchResultSet(this, searchResultSongHeaderText, queryString);

            string url = "/databases/" + DatabaseID + "/containers/" + BasePlaylist.ID + "/items"
                + "?meta=dmap.itemname,dmap.itemid,daap.songartist,daap.songalbum,dmap.containeritemid,com.apple.itunes.has-video,daap.songdatereleased,dmap.itemcount,daap.songtime,dmap.persistentid,daap.songalbum"
                + "&type=music"
                + "&sort=name"
                + "&include-sort-headers=1"
                + "&query=" + queryString
                + "&session-id=" + SessionID;
            songSearchRequestInfo = SubmitHTTPRequest(url, new HTTPResponseHandler(ProcessSongSearchResponse), true, r => r.ActionObject = songResults);

            return songResults;
        }

        protected void ProcessSongSearchResponse(HTTPRequestInfo requestInfo)
        {
            if (requestInfo != songSearchRequestInfo)
                return;

            ProcessSearchResponse(requestInfo, bytes => new MediaItem(this, bytes));
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
                    var itemNodes = Utility.GetResponseNodes(kvp.Value);
                    foreach (var itemData in itemNodes)
                        libraryElements.Add(itemGenerator(itemData.Value));
                }
            }

            SearchResultSet searchResults = (SearchResultSet)requestInfo.ActionObject;

            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                foreach (ILibraryElement item in libraryElements)
                    searchResults.Add(item);
            });
        }

        #endregion

    }
}
