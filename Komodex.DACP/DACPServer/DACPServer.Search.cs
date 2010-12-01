﻿using System;
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

        #region Properties

        private ObservableCollection<GroupItems<ILibraryItem>> _SearchResults = null;
        public ObservableCollection<GroupItems<ILibraryItem>> SearchResults
        {
            get { return _SearchResults; }
            set
            {
                if (_SearchResults == value)
                    return;
                _SearchResults = value;
                SendPropertyChanged("SearchResults");
            }
        }

        #endregion

        #region Public Methods

        public void GetSearchResults(string searchString)
        {
            var searchResults = new ObservableCollection<GroupItems<ILibraryItem>>();
            searchResults.Add(new GroupItems<ILibraryItem>(searchResultAlbumHeaderText));
            searchResults.Add(new GroupItems<ILibraryItem>(searchResultArtistHeaderText));
            searchResults.Add(new GroupItems<ILibraryItem>(searchResultSongHeaderText));
            SearchResults = searchResults;

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

            string escapedSearchString = Uri.EscapeDataString(Utility.EscapeSingleQuotes(searchString));

            SubmitAlbumSearchRequest(escapedSearchString);
            SubmitArtistSearchRequest(escapedSearchString);
        }

        #endregion

        #region Responses and Requests

        #region Albums

        private HTTPRequestInfo albumSearchRequestInfo = null;

        protected void SubmitAlbumSearchRequest(string escapedSearchString)
        {
            string url = "/databases/" + DatabaseID + "/groups"
                + "?meta=dmap.itemname,dmap.itemid,dmap.persistentid,daap.songartist,daap.songdatereleased,dmap.itemcount,daap.songtime,dmap.persistentid"
                + "&type=music"
                + "&group-type=albums"
                + "&sort=album"
                + "&include-sort-headers=1"
                + "&query=('daap.songalbum:*" + escapedSearchString + "*'+('com.apple.itunes.mediakind:1','com.apple.itunes.mediakind:32')+'daap.songalbum!:')"
                + "&session-id=" + SessionID;
            albumSearchRequestInfo = SubmitHTTPRequest(url, null, null, new HTTPResponseHandler(ProcessAlbumSearchResponse));
        }

        protected void ProcessAlbumSearchResponse(HTTPRequestInfo requestInfo)
        {
            if (requestInfo != albumSearchRequestInfo)
                return;

            var albums = new GroupItems<ILibraryItem>(searchResultAlbumHeaderText);
            //var albums = _SearchResults[0];

            foreach (var kvp in requestInfo.ResponseNodes)
            {
                if (kvp.Key == "mlcl")
                {
                    var albumNodes = Utility.GetResponseNodes(kvp.Value);
                    foreach (var albumData in albumNodes)
                        albums.Add(new Album(this, albumData.Value));
                }
            }

            if (albums.Count == 0)
                return;

            //SendPropertyChanged("SearchResults");

            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                // The entire SearchResults list is replaced because of an issue with the LongListSelector
                // Replacing a single element triggers a NotSupportedException
                // TODO: Find a better way
                var searchResults = new ObservableCollection<GroupItems<ILibraryItem>>();
                searchResults.Add(albums);
                searchResults.Add(_SearchResults[1]);
                searchResults.Add(_SearchResults[2]);
                SearchResults = searchResults;
            });
        }

        #endregion

        #region Artists

        private HTTPRequestInfo artistSearchRequestInfo = null;

        protected void SubmitArtistSearchRequest(string escapedSearchString)
        {
            string url = "/databases/" + DatabaseID + "/groups"
                + "?meta=dmap.itemname,dmap.itemid,dmap.persistentid,daap.songartist,daap.groupalbumcount"
                + "&type=music"
                + "&group-type=artists"
                + "&sort=album"
                + "&include-sort-headers=1"
                + "&query=('daap.songartist:*" + escapedSearchString + "*'+('com.apple.itunes.mediakind:1','com.apple.itunes.mediakind:32')+'daap.songartist!:')"
                + "&session-id=" + SessionID;
            artistSearchRequestInfo = SubmitHTTPRequest(url, null, null, new HTTPResponseHandler(ProcessArtistSearchResponse));
        }

        protected void ProcessArtistSearchResponse(HTTPRequestInfo requestInfo)
        {
            if (requestInfo != artistSearchRequestInfo)
                return;

            var artists = new GroupItems<ILibraryItem>(searchResultArtistHeaderText);

            foreach (var kvp in requestInfo.ResponseNodes)
            {
                if (kvp.Key == "mlcl")
                {
                    var artistNodes = Utility.GetResponseNodes(kvp.Value);
                    foreach (var artistData in artistNodes)
                        artists.Add(new Artist(this, artistData.Value));
                }
            }

            if (artists.Count == 0)
                return;

            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                var searchResults = new ObservableCollection<GroupItems<ILibraryItem>>();
                searchResults.Add(_SearchResults[0]);
                searchResults.Add(artists);
                searchResults.Add(_SearchResults[2]);
                SearchResults = searchResults;
            });
        }

        #endregion

        #endregion
    }
}