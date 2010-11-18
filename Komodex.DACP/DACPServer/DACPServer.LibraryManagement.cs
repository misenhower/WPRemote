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

namespace Komodex.DACP
{
    public partial class DACPServer
    {
        #region Properties

        public int DatabaseID { get; protected set; }

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

        protected void SubmitArtistsRequest()
        {
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

        }

        #endregion

        #region Album
        /*
        protected void SubmitAlbumRequest(int infoSomeType)
        {
            string url = "/databases/" + DatabaseID + "/groups"
                + "?meta=dmap.itemname,dmap.itemid,dmap.persistentid,daap.songartist,daap.songdatereleased,dmap.itemcount,daap.songtime,dmap.persistentid"
                + "&type=music"
                + "&group-type=albums"
                + "&sort=album"
                + "&include-sort-headers=1"
                + "&query=(('daap.songartist:Puscifer','daap.songalbumartist:Puscifer')+('com.apple.itunes.mediakind:1','com.apple.itunes.mediakind:32')+'daap.songalbum!:')"
                + "&session-id=" + SessionID;
            SubmitHTTPRequest(url);
        }
        */
        #endregion

        #endregion
    }
}
