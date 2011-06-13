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
        public ServerUpdateEventArgs(ServerUpdateType type, ServerErrorType errorType = ServerErrorType.None, string errorDetails = null)
        {
            Type = type;
            ErrorType = errorType;
            ErrorDetails = errorDetails;
        }

        public ServerUpdateType Type { get; protected set; }
        public ServerErrorType ErrorType { get; protected set; }
        public string ErrorDetails { get; protected set; }
    }

    public enum ServerUpdateType
    {
        ServerConnected,
        Error,
        AirPlaySpeakerPassword,
        LibraryError, // e.g., trying to play a song that has been deleted
    }

    public enum ServerErrorType
    {
        None,
        General,
        InvalidPIN,
        UnsupportedVersion,
    }
}
