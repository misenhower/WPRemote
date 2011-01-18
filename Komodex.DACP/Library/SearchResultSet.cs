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

namespace Komodex.DACP.Library
{
    public class SearchResultSet : GroupItems<ILibraryElement>
    {
        public SearchResultSet(DACPServer server, string headerText, string queryString = null)
            : base(headerText)
        {
            Server = server;
            QueryString = queryString;
        }

        #region Properties

        public DACPServer Server { get; protected set; }
        public string QueryString { get; protected set; }

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

                Server.SubmitHTTPRequest(url);
            }
            catch { }
        }

        #endregion

        #endregion
    }
}
