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
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Komodex.Common;
using System.Threading;
using System.Windows.Threading;

namespace Komodex.DACP
{
    public partial class DACPServer
    {
        private static readonly Log _log = new Log("DACP");

        private DACPServer()
        {
            Utility.BeginInvokeOnUIThread(() =>
            {
                timerTrackTimeUpdate = new DispatcherTimer();
                timerTrackTimeUpdate.Interval = TimeSpan.FromSeconds(1);
                timerTrackTimeUpdate.Tick += timerTrackTimeUpdate_Tick;
            });

            _playStatusCancelTimer = new Timer(playStatusCancelTimer_Tick);
        }

        public DACPServer(string hostname, int port, string pairingCode)
            : this()
        {
            string assemblyName = System.Reflection.Assembly.GetCallingAssembly().FullName;
            if (!assemblyName.StartsWith("Komodex.Remote,"))
                throw new Exception();

            _hostname = hostname;
            _port = port;
            UpdateHTTPPrefix();
            PairingCode = pairingCode;
        }

        public static string GetAssemblyName()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().FullName;
        }

        #region Fields

        private bool UseDelayedResponseRequests = true;
        private bool Stopped = false;
        internal string HTTPPrefix;

        #endregion

        #region Properties

        private string _hostname;
        public string Hostname
        {
            get { return _hostname; }
            set
            {
                if (_hostname == value)
                    return;

                _hostname = value;
                UpdateHTTPPrefix();
            }
        }

        private int _port = 3689;
        public int Port
        {
            get { return _port; }
            set
            {
                if (_port == value)
                    return;

                _port = value;
                UpdateHTTPPrefix();
            }
        }

        protected void UpdateHTTPPrefix()
        {
            HTTPPrefix = "http://" + Hostname + ":" + Port;
            UpdateHttpClient();
        }

        public string PairingCode { get; protected set; }

        public int SessionID { get; protected set; }

        #endregion

        #region Public Methods

        public void Start()
        {
            Start(UseDelayedResponseRequests);
        }

        public void Start(bool useDelayedResponseRequests)
        {
            UseDelayedResponseRequests = useDelayedResponseRequests;
            _playStatusRevisionNumber = 1;
            ignoringTrackTimeChanges = false;
            ignoringVolumeChanges = false;
            sendTrackTimeChangeWhenFinished = -1;

            // Note: Do not clear AlbumIDs or ArtistIDs here.
            // Since the LibraryArtists and LibraryAlbums variables are never cleared (and therefore never reloaded),
            // the Album ID and Artist ID caches may never actually get refreshed.

            Stopped = false;

            SubmitServerInfoRequest();
        }

        public void Stop()
        {
            Stopped = true;
            IsConnected = false;

            try
            {
                List<HTTPRequestInfo> oldRequests;
                lock (PendingHttpRequests)
                {
                    oldRequests = PendingHttpRequests.ToList();
                    PendingHttpRequests.Clear();
                }

                foreach (HTTPRequestInfo request in oldRequests)
                {
                    try
                    {
                        request.WebRequest.Abort();
                    }
                    catch { }
                }
            }
            catch { }
        }

        #endregion

        #region Connection State

        protected void ConnectionEstablished()
        {
            if (IsConnected)
                return;

            IsConnected = true;
            SendServerUpdate(ServerUpdateType.ServerConnected);
        }

        internal void HandleHTTPException(string uri, Exception e)
        {
            _log.Error("HTTP Exception for URI: " + uri);
            _log.Debug("Exception details: " + e.ToString());
            ConnectionError("HTTP Exception:\nURI: " + uri + "\n" + e.ToString());
        }

        protected void ConnectionError(string errorDetails)
        {
            ConnectionError(ServerErrorType.General, errorDetails);
        }

        protected void ConnectionError(ServerErrorType errorType = ServerErrorType.General, string errorDetails = null)
        {
            IsConnected = false;

            if (!Stopped)
            {
                Stop();
                SendServerUpdate(ServerUpdateType.Error, errorType, errorDetails);
            }
        }

        #endregion

        #region Events

        public event EventHandler<ServerUpdateEventArgs> ServerUpdate;

        protected void SendServerUpdate(ServerUpdateType type)
        {
            if (ServerUpdate != null)
                ServerUpdate(this, new ServerUpdateEventArgs(type));
        }

        protected void SendServerUpdate(ServerUpdateType type, ServerErrorType errorType)
        {
            if (ServerUpdate != null)
                ServerUpdate(this, new ServerUpdateEventArgs(type, errorType));
        }

        protected void SendServerUpdate(ServerUpdateType type, ServerErrorType errorType, string errorDetails)
        {
            if (ServerUpdate != null)
                ServerUpdate(this, new ServerUpdateEventArgs(type, errorType, errorDetails));
        }

        #endregion

    }
}
