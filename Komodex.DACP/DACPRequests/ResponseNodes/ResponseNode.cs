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

namespace Komodex.DACP.DACPRequests.ResponseNodes
{
    public class ResponseNode
    {
        protected ResponseNode() { }

        #region Static Methods

        public static void ProcessResponseBody(byte[] data)
        {
            ResponseNode node = new ResponseNode();
            //
            //
        }

        #endregion

        #region Properties

        public byte[] Body { get; protected set; }

        #endregion

    }
}
