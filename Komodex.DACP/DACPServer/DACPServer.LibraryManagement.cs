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

        #endregion
    }
}
