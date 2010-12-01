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
using Komodex.DACP.Library;

namespace Komodex.DACP
{
    public class SearchResultSet : ObservableCollection<GroupItems<ILibraryItem>>
    {
        public SearchResultSet(DACPServer server, string searchString)
        {
            Server = server;
            SearchString = searchString;
        }

        public DACPServer Server { get; protected set; }
        public string SearchString { get; protected set; }
    }
}
