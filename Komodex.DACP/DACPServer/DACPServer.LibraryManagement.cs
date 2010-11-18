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

namespace Komodex.DACP
{
    public partial class DACPServer
    {
        #region Properties

        public int DatabaseID { get; protected set; }

        private ObservableCollection<Artist> _LibraryArtists = null;
        public ObservableCollection<Artist> LibraryArtists
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

        #endregion

        #region Requests and Responses

        #region Databases

        protected void SubmitDatabasesRequest()
        {
            string url = "/databases?session-id=" + SessionID;
            SubmitHTTPRequest(url);
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
                        default:
                            break;
                    }
                }
            }
            catch { }
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
            SubmitHTTPRequest(url);
        }

        protected void ProcessArtistsResponse(HTTPRequestInfo requestInfo)
        {
            foreach (var kvp in requestInfo.ResponseNodes)
            {
                switch (kvp.Key)
                {
                    case "mlcl":
                        ObservableCollection<Artist> libraryArtists = new ObservableCollection<Artist>();

                        var artistNodes = Utility.GetResponseNodes(kvp.Value);
                        foreach (var artistData in artistNodes)
                        {
                            libraryArtists.Add(new Artist(this, artistData.Value));
                        }

                        LibraryArtists = libraryArtists;
                        break;
                    default:
                        break;
                }
            }

            retrievingArtists = false;
        }

        #endregion

        #region Albums

        #endregion

        #endregion
    }
}
