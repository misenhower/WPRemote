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
using Komodex.DACP;
using Komodex.WP7DACPRemote.DACPServerInfoManagement;
using System.Windows.Controls.Primitives;
using Komodex.WP7DACPRemote.Controls;
using Microsoft.Phone.Controls;

namespace Komodex.WP7DACPRemote.DACPServerManagement
{
    public static class DACPServerManager
    {
        #region Properties

        private static DACPServer _Server = null;
        public static DACPServer Server
        {
            get { return _Server; }
            private set
            {
                if (_Server != null)
                {
                    _Server.ServerUpdate -= new EventHandler<ServerUpdateEventArgs>(Server_ServerUpdate);
                    _Server.PropertyChanged -= new System.ComponentModel.PropertyChangedEventHandler(Server_PropertyChanged);
                    _Server.Stop();
                }

                _Server = value;

                if (_Server != null)
                {
                    _Server.ServerUpdate += new EventHandler<ServerUpdateEventArgs>(Server_ServerUpdate);
                    _Server.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(Server_PropertyChanged);
                }

                SendServerChanged();
            }
        }

        #endregion

        #region Popup

        private static Popup connectingPopup = null;
        private static ConnectingStatusControl connectingStatusControl = null;

        private static void ShowPopup()
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (connectingPopup == null)
                {
                    connectingPopup = new Popup();


                    if (connectingStatusControl == null)
                    {
                        connectingStatusControl = new ConnectingStatusControl();
                    }

                    connectingPopup.Child = connectingStatusControl;
                }

                if (RootVisual != null)
                {
                    connectingStatusControl.Width = RootVisual.ActualWidth;
                    connectingStatusControl.Height = RootVisual.ActualHeight;
                }
                connectingPopup.IsOpen = true;
            });
        }

        private static void HidePopup()
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (connectingPopup != null)
                    connectingPopup.IsOpen = false;
            });
        }

        private static PhoneApplicationFrame _RootVisual = null;
        private static PhoneApplicationFrame RootVisual
        {
            get
            {
                var x = Application.Current.RootVisual;
                if (_RootVisual == null)
                    _RootVisual = Application.Current.RootVisual as PhoneApplicationFrame;

                return _RootVisual;
            }
        }

        #endregion

        #region Public Methods

        private static bool firstLoadDone = false;
        public static void DoFirstLoad()
        {
            if (firstLoadDone)
                return;
            firstLoadDone = true;
            ConnectToServer();
        }

        public static void ConnectToServer(Guid serverID)
        {
            DACPServerViewModel.Instance.SelectedServerGuid = serverID;
            ConnectToServer();
        }

        public static void ConnectToServer()
        {
            ShowPopup();
            Server = null;

            DACPServerInfo serverInfo = DACPServerViewModel.Instance.CurrentDACPServer;

            if (serverInfo != null)
            {
                DACPServer server = new DACPServer(serverInfo.ID, serverInfo.HostName, serverInfo.PairingCode);
                server.LibraryName = serverInfo.LibraryName;
                Server = server;
                Server.Start();
            }
        }

        #endregion

        #region Event Handlers

        static void Server_ServerUpdate(object sender, ServerUpdateEventArgs e)
        {
            switch (e.Type)
            {
                case ServerUpdateType.ServerInfoResponse:
                    break;
                case ServerUpdateType.ServerConnected:
                    HidePopup();
                    break;
                case ServerUpdateType.Error:
                    // Need to have an auto-reconnect feature but it needs to know when auto-reconnect has already been attempted
                    //ConnectToServer();
                    Server = null;
                    break;
                default:
                    break;
            }
        }

        static void Server_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != "LibraryName")
                return;

            DACPServerInfo serverInfo = DACPServerViewModel.Instance.CurrentDACPServer;
            DACPServer server = sender as DACPServer;
            if (serverInfo == null || server == null || serverInfo.ID != server.ID)
                return;

            serverInfo.LibraryName = server.LibraryName;
        }

        #endregion

        #region Events

        public static event EventHandler ServerChanged;

        private static void SendServerChanged()
        {
            if (ServerChanged != null)
                ServerChanged(null, new EventArgs());
        }

        #endregion
    }
}
