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

        #region Public Methods

        public SearchResultSet GetSearchResults(string searchString)
        {
            string escapedSearchString = Uri.EscapeDataString(Utility.EscapeSingleQuotes(searchString.Trim()));

            var searchResults = new SearchResultSet(this, escapedSearchString);
            searchResults.Add(new GroupItems<ILibraryItem>(searchResultAlbumHeaderText));
            searchResults.Add(new GroupItems<ILibraryItem>(searchResultArtistHeaderText));
            searchResults.Add(new GroupItems<ILibraryItem>(searchResultSongHeaderText));
            searchResults.SongGroup = searchResults[2];

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

            SubmitAlbumSearchRequest(escapedSearchString, searchResults);
            SubmitArtistSearchRequest(escapedSearchString, searchResults);
            SubmitSongSearchRequest(escapedSearchString, searchResults);

            return searchResults;
        }

        #endregion

        #region Responses and Requests

        #region Albums

        private HTTPRequestInfo albumSearchRequestInfo = null;

        protected void SubmitAlbumSearchRequest(string escapedSearchString, SearchResultSet resultSet)
        {
            string url = "/databases/" + DatabaseID + "/groups"
                + "?meta=dmap.itemname,dmap.itemid,dmap.persistentid,daap.songartist,daap.songdatereleased,dmap.itemcount,daap.songtime,dmap.persistentid"
                + "&type=music"
                + "&group-type=albums"
                + "&sort=album"
                + "&include-sort-headers=1"
                + "&query=('daap.songalbum:*" + escapedSearchString + "*'+('com.apple.itunes.mediakind:1','com.apple.itunes.mediakind:32')+'daap.songalbum!:')"
                + "&session-id=" + SessionID;
            albumSearchRequestInfo = SubmitHTTPRequest(url, null, null, new HTTPResponseHandler(ProcessAlbumSearchResponse), resultSet);
        }

        protected void ProcessAlbumSearchResponse(HTTPRequestInfo requestInfo)
        {
            if (requestInfo != albumSearchRequestInfo)
                return;

            var resultSet = (SearchResultSet)requestInfo.ActionObject;

            var albums = new GroupItems<ILibraryItem>(searchResultAlbumHeaderText);

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

            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                resultSet[0] = albums;
            });
        }

        #endregion

        #region Artists

        private HTTPRequestInfo artistSearchRequestInfo = null;

        protected void SubmitArtistSearchRequest(string escapedSearchString, SearchResultSet resultSet)
        {
            string url = "/databases/" + DatabaseID + "/groups"
                + "?meta=dmap.itemname,dmap.itemid,dmap.persistentid,daap.songartist,daap.groupalbumcount"
                + "&type=music"
                + "&group-type=artists"
                + "&sort=album"
                + "&include-sort-headers=1"
                + "&query=('daap.songartist:*" + escapedSearchString + "*'+('com.apple.itunes.mediakind:1','com.apple.itunes.mediakind:32')+'daap.songartist!:')"
                + "&session-id=" + SessionID;
            artistSearchRequestInfo = SubmitHTTPRequest(url, null, null, new HTTPResponseHandler(ProcessArtistSearchResponse), resultSet);
        }

        protected void ProcessArtistSearchResponse(HTTPRequestInfo requestInfo)
        {
            if (requestInfo != artistSearchRequestInfo)
                return;

            var resultSet = (SearchResultSet)requestInfo.ActionObject;

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
                resultSet[1] = artists;
            });
        }

        #endregion

        #region Songs

        private HTTPRequestInfo songSearchRequestInfo = null;

        protected void SubmitSongSearchRequest(string escapedSearchString, SearchResultSet resultSet)
        {
            string url = "/databases/" + DatabaseID + "/containers/" + BasePlaylistID + "/items"
                + "?meta=dmap.itemname,dmap.itemid,daap.songartist,daap.songalbum,dmap.containeritemid,com.apple.itunes.has-video,daap.songdatereleased,dmap.itemcount,daap.songtime,dmap.persistentid,daap.songalbum"
                + "&type=music"
                + "&sort=name"
                + "&include-sort-headers=1"
                + "&query=('dmap.itemname:*" + escapedSearchString + "*'+('com.apple.itunes.mediakind:1','com.apple.itunes.mediakind:32'))"
                + "&session-id=" + SessionID;
            songSearchRequestInfo = SubmitHTTPRequest(url, null, null, new HTTPResponseHandler(ProcessSongSearchResponse), resultSet);
        }

        protected void ProcessSongSearchResponse(HTTPRequestInfo requestInfo)
        {
            if (requestInfo != songSearchRequestInfo)
                return;

            var resultSet = (SearchResultSet)requestInfo.ActionObject;

            var songs = new GroupItems<ILibraryItem>(searchResultSongHeaderText);

            foreach (var kvp in requestInfo.ResponseNodes)
            {
                if (kvp.Key == "mlcl")
                {
                    var songNodes = Utility.GetResponseNodes(kvp.Value);
                    foreach (var songData in songNodes)
                        songs.Add(new Song(this, songData.Value));
                }
            }

            if (songs.Count == 0)
                return;

            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                resultSet[2] = songs;
                resultSet.SongGroup = songs;
            });
        }

        #endregion

        #endregion
    }
}
