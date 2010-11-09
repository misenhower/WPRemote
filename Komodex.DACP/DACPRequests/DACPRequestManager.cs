﻿using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Komodex.DACP.DACPRequests
{
    public class DACPRequestManager
    {
        public DACPRequestManager(DACPServer dacpServer)
        {
            DACPServer = dacpServer;
        }

        public DACPServer DACPServer { get; protected set; }

         

    }
}
