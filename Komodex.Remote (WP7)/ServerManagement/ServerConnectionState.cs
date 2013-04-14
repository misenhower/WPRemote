using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Komodex.Remote.ServerManagement
{
    public enum ServerConnectionState
    {
        NoLibrarySelected,
        WaitingForWiFiConnection,
        LookingForLibrary,
        ConnectingToLibrary,
        Connected,
    }

    public class ConnectionStateChangedEventArgs : EventArgs
    {
        public ConnectionStateChangedEventArgs(ServerConnectionState state)
        {
            State = state;
        }

        public ServerConnectionState State { get; protected set; }
    }
}
