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
using System.Collections.Generic;
using Komodex.Common;
using Komodex.DACP.Databases;

namespace Komodex.DACP
{
    public partial class DACPServer
    {
        #region Databases

        public DACPDatabase MainDatabase { get; protected set; }
        public DACPDatabase InternetRadioDatabase { get; protected set; }
        public Dictionary<int, DACPDatabase> SharedDatabases { get; protected set; }

        #endregion

        #region Properties

        public int DatabaseID { get; protected set; }
        public UInt64 DatabasePersistentID { get; protected set; }
        public UInt64 ServiceID { get; protected set; }

        #endregion

        #region Requests and Responses

        #region Databases

        protected void SubmitDatabasesRequest()
        {
            string url = "/databases?session-id=" + SessionID;
            SubmitHTTPRequest(url, new HTTPResponseHandler(ProcessDatabasesResponse));
        }

        protected void ProcessDatabasesResponse(HTTPRequestInfo requestInfo)
        {
            var databases = DACPUtility.GetItemsFromNodes(requestInfo.ResponseNodes, data => new DACPDatabase(this, data)).ToList();
            if (databases.Count == 0)
            {
                ConnectionError();
                return;
            }

            for (int i = 0; i < databases.Count; i++)
            {
                var database = databases[i];

                // Main Database
                if (i == 0)
                {
                    if (MainDatabase == null)
                    {
                        MainDatabase = database;
                        MainDatabase.RequestContainersAsync();
                    }
                    continue;
                }

                // Internet Radio Database
                if (database.DBKind == 100)
                {
                    if (InternetRadioDatabase == null)
                        InternetRadioDatabase = database;
                    continue;
                }

                // TODO: Shared databases
            }

            var mlcl = requestInfo.ResponseNodes.FirstOrDefault(n => n.Key == "mlcl").Value;
            var mlit = DACPUtility.GetResponseNodes(mlcl).First(n => n.Key == "mlit").Value;

            var nodes = DACPNodeDictionary.Parse(mlit);

            DatabaseID = nodes.GetInt("miid");
            DatabasePersistentID = (UInt64)nodes.GetLong("mper");
            ServiceID = (UInt64)nodes.GetLong("aeIM");


            if (UseDelayedResponseRequests)
            {
                SubmitLibraryUpdateRequest();
                SubmitPlayStatusRequest();
            }

            ConnectionEstablished();
        }

        #endregion

        #endregion
    }
}
