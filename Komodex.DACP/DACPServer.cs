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
using Komodex.DACP.DACPRequests;

namespace Komodex.DACP
{
    public class DACPServer
    {
        private DACPServer() { }

        public DACPServer(string hostName, string pairingKey)
        {
            dacpRequestManager = new DACPRequestManager(this);
        }

        #region Fields

        DACPRequestManager dacpRequestManager = null;

        #endregion

        #region Properties



        #endregion

        public void Connect()
        {


        }
    }
}
