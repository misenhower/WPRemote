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

namespace Komodex.Bonjour
{
    public class NetServiceEventArgs : EventArgs
    {
        public NetServiceEventArgs(NetService service)
        {
            Service = service;
        }

        public NetService Service { get; protected set; }
    }
}
