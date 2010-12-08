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

namespace Komodex.DACP
{
    public partial class DACPServer
    {
        private DACPServer()
        {
            timerTrackTimeUpdate.Interval = TimeSpan.FromSeconds(1);
            timerTrackTimeUpdate.Tick += new EventHandler(timerTrackTimeUpdate_Tick);
        }

        public DACPServer(Guid id, string hostName, string pairingKey)
            : this()
        {
            string assemblyName = System.Reflection.Assembly.GetCallingAssembly().FullName;
            if (!assemblyName.StartsWith("Komodex.WP7DACPRemote,"))
                throw new Exception();

            ID = id;
            HostName = hostName;
            PairingKey = pairingKey;
        }

        public DACPServer(string hostName, string pairingKey)
            //: this(Guid.Empty, hostName, pairingKey)
            : this()
        {
            string assemblyName = System.Reflection.Assembly.GetCallingAssembly().FullName;
            if (!assemblyName.StartsWith("Komodex.WP7DACPRemote,"))
                throw new Exception();

            ID = Guid.Empty;
            HostName = hostName;
            PairingKey = pairingKey;
        }

        #region Fields

        private bool UseDelayedResponseRequests = true;
        private bool Stopped = false;

        #endregion

        #region Properties

        public Guid ID { get; protected set; }

        private string _HostName = null;
        public string HostName
        {
            get { return _HostName; }
            protected set
            {
                _HostName = value;
                _HTTPPrefix = null;
            }
        }

        public string PairingKey { get; protected set; }

        private string _HTTPPrefix = null;
        public string HTTPPrefix
        {
            get
            {
                if (string.IsNullOrEmpty(_HTTPPrefix))
                    _HTTPPrefix = "http://" + HostName + ":3689";
                return _HTTPPrefix;
            }
        }

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

            Stopped = false;

            SubmitServerInfoRequest();
        }

        public void Stop()
        {
            Stopped = true;
            IsConnected = false;

            try
            {
                foreach (HTTPRequestInfo request in PendingHttpRequests)
                {
                    request.WebRequest.Abort();
                }
            }
            catch { }

            PendingHttpRequests.Clear();
        }

        #endregion

        #region Connection State

        protected bool TryToReconnect = false;

        protected void ConnectionEstablished()
        {
            if (IsConnected)
                return;

            TryToReconnect = true;
            IsConnected = true;
            SendServerUpdate(ServerUpdateType.ServerConnected);
        }

        protected void ConnectionError()
        {
            ConnectionError(ServerErrorType.General);
        }

        protected void ConnectionError(ServerErrorType errorType)
        {
            IsConnected = false;

            if (!Stopped)
            {
                if (TryToReconnect)
                {
                    Stop();
                    TryToReconnect = false;
                    SendServerUpdate(ServerUpdateType.ServerReconnecting);
                    Start();
                }
                else
                {
                    SendServerUpdate(ServerUpdateType.Error, errorType);
                }
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

        #endregion

    }
}
