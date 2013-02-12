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
using Komodex.DACP.Localization;

namespace Komodex.DACP.Library
{
    public class SearchResultSet : GroupItems<ILibraryElement>
    {
        public SearchResultSet(DACPServer server, SearchResultsType type, string queryString = null)
            : base(GetHeaderText(type))
        {
            Server = server;
            Type = type;
            QueryString = queryString;
        }

        private static string GetHeaderText(SearchResultsType type)
        {
            switch (type)
            {
                case SearchResultsType.Albums:
                    return LocalizedDACPStrings.SearchResultsAlbums;
                case SearchResultsType.Artists:
                    return LocalizedDACPStrings.SearchResultsArtists;
                case SearchResultsType.Songs:
                    return LocalizedDACPStrings.SearchResultsSongs;
                case SearchResultsType.Movies:
                    return LocalizedDACPStrings.SearchResultsMovies;
                case SearchResultsType.Podcasts:
                    return LocalizedDACPStrings.SearchResultsPodcasts;
                default:
                    return null;
            }
        }

        #region Properties

        public DACPServer Server { get; protected set; }
        public string QueryString { get; protected set; }
        public SearchResultsType Type { get; protected set; }

        #endregion

        #region HTTP Requests and Responses

        #region Play Item Command

        public void SendPlayItemCommand(MediaItem item)
        {
            if (string.IsNullOrEmpty(QueryString))
                return;

            try
            {
                int itemIndex = IndexOf(item);

                string url = "/ctrl-int/1/cue"
                    + "?command=play"
                    + "&query=" + QueryString
                    + "&index=" + itemIndex
                    + "&sort=name"
                    + "&clear-first=1"
                    + "&session-id=" + Server.SessionID;

                Server.SubmitHTTPPlayRequest(url);
            }
            catch { }
        }

        #endregion

        #endregion
    }
}
