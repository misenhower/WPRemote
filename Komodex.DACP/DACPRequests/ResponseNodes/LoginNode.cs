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
    public class LoginNode : ResponseNode
    {
        protected override void ProcessNode(string code, byte[] body)
        {
            if (code == "mlid") // Session ID
                return;
        }
    }
}
