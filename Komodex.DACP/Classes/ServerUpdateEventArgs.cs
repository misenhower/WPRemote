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

namespace Komodex.DACP
{
    public class ServerUpdateEventArgs : EventArgs
    {
        public ServerUpdateEventArgs(ServerUpdateType type)
        {
            Type = type;
        }

        public ServerUpdateType Type { get; protected set; }
    }

    public enum ServerUpdateType
    {
        ServerConnected,
        ServerReconnecting,
        Error,
    }
}
