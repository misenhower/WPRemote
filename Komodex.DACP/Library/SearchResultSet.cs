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
    public class SearchResultSet : ObservableCollection<GroupItems<ILibraryItem>>
    {
        public SearchResultSet(DACPServer server, string searchString)
        {
            Server = server;
            SearchString = searchString;
        }

        #region Properties

        public DACPServer Server { get; protected set; }
        public string SearchString { get; protected set; }

        private GroupItems<ILibraryItem> _SongGroup = null;
        public GroupItems<ILibraryItem> SongGroup
        {
            get { return _SongGroup; }
            set
            {
                if (!Contains(value))
                    throw new ArgumentException("Argument must be contained in the list");

                _SongGroup = value;
            }
        }

        #endregion

        #region HTTP Requests and Responses

        #region Play Song Command

        public void SendPlaySongCommand(Song song)
        {
            try
            {
                int songIndex = SongGroup.IndexOf(song);

                string url = "/ctrl-int/1/cue"
                    + "?command=play"
                    + "&query=('dmap.itemname:*" + SearchString + "*'+('com.apple.itunes.mediakind:1','com.apple.itunes.mediakind:32'))"
                    + "&index=" + songIndex
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
