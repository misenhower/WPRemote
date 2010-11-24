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

                UpdatePopupDisplay();
                SendServerChanged();
            }
        }

        #endregion

        #region Popup

        private static bool applicationBarWasOpen = false;

        private static Popup _ConnectingPopup = null;
        private static Popup ConnectingPopup
        {
            get
            {
                if (_ConnectingPopup == null)
                {
                    _ConnectingPopup = new Popup();
                    _ConnectingPopup.Child = ConnectingStatusControl;
                }

                return _ConnectingPopup;
            }
        }

        private static ConnectingStatusControl _ConnectingStatusControl = null;
        private static ConnectingStatusControl ConnectingStatusControl
        {
            get
            {
                if (_ConnectingStatusControl == null)
                {
                    _ConnectingStatusControl = new ConnectingStatusControl(true);
                    UpdatePopupSize();

                    _ConnectingStatusControl.ButtonClick += new RoutedEventHandler(ConnectingStatusControl_ButtonClick);
                }

                return _ConnectingStatusControl;
            }
        }

        private static void UpdatePopupDisplay()
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (ConnectingPopup == null)
                    return;

                PhoneApplicationPage currentPage = GetCurrentPhoneApplicationPage();
                if (currentPage == null)
                    return;

                bool connecting = (Server == null || !Server.IsConnected);
                bool canShow = !(currentPage is LibraryChooserPage || currentPage is AddLibraryPage);

                if (canShow)
                {
                    if (connecting)
                    {
                        if (!ConnectingPopup.IsOpen)
                        {
                            if (currentPage.ApplicationBar != null)
                            {
                                applicationBarWasOpen = currentPage.ApplicationBar.IsVisible;
                                currentPage.ApplicationBar.IsVisible = false;
                            }

                            ConnectingPopup.IsOpen = true;
                        }
                    }
                    else
                    {
                        if (ConnectingPopup.IsOpen)
                        {
                            ConnectingPopup.IsOpen = false;

                            if (applicationBarWasOpen && currentPage.ApplicationBar != null)
                                currentPage.ApplicationBar.IsVisible = true;

                        }
                    }
                }
                else
                {
                    ConnectingPopup.IsOpen = false;
                }
            });
        }

        static void ConnectingStatusControl_ButtonClick(object sender, RoutedEventArgs e)
        {
            if (Server != null)
            {
                Server.Stop();
                Server = null;
            }

            if (RootVisual != null)
                NavigationManager.OpenLibraryChooserPage();
        }

        private static PhoneApplicationFrame RootVisual { get; set; }

        private static PhoneApplicationPage GetCurrentPhoneApplicationPage()
        {
            if (RootVisual == null)
                return null;

            return RootVisual.Content as PhoneApplicationPage;
        }

        static void RootVisual_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdatePopupSize(e.NewSize.Width, e.NewSize.Height);
        }

        static void RootVisual_OrientationChanged(object sender, OrientationChangedEventArgs e)
        {
            UpdatePopupSize();
        }

        static void RootVisual_Navigating(object sender, System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            if (Server != null && Server.IsConnected)
                return;

            if (applicationBarWasOpen)
            {
                PhoneApplicationPage currentPage = GetCurrentPhoneApplicationPage();
                if (currentPage != null && currentPage.ApplicationBar != null)
                    currentPage.ApplicationBar.IsVisible = true;
            }
        }

        static void RootVisual_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            if (Server != null && Server.IsConnected)
                return;

            UpdatePopupDisplay();
        }

        private static void UpdatePopupSize()
        {
            if (RootVisual != null)
                UpdatePopupSize(RootVisual.ActualWidth, RootVisual.ActualHeight);
        }

        private static void UpdatePopupSize(double width, double height)
        {
            if (ConnectingStatusControl == null)
                return;

            ConnectingStatusControl.Width = width;
            ConnectingStatusControl.Height = height;
        }

        #endregion

        #region Public Methods

        public static void DoFirstLoad(PhoneApplicationFrame frame)
        {
            RootVisual = frame;

            RootVisual.OrientationChanged += new EventHandler<OrientationChangedEventArgs>(RootVisual_OrientationChanged);
            RootVisual.SizeChanged += new SizeChangedEventHandler(RootVisual_SizeChanged);
            RootVisual.Navigating += new System.Windows.Navigation.NavigatingCancelEventHandler(RootVisual_Navigating);
            RootVisual.Navigated += new System.Windows.Navigation.NavigatedEventHandler(RootVisual_Navigated);

            if (DACPServerViewModel.Instance.CurrentDACPServer == null)
                return;

            ConnectToServer();
        }

        public static void ConnectToServer(Guid serverID)
        {
            DACPServerViewModel.Instance.SelectedServerGuid = serverID;
            ConnectToServer();
        }

        public static void ConnectToServer()
        {
            Server = null;

            UpdatePopupDisplay();

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
                case ServerUpdateType.ServerReconnecting:
                case ServerUpdateType.ServerConnected:
                    UpdatePopupDisplay();
                    break;
                case ServerUpdateType.Error:
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
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (ServerChanged != null)
                    ServerChanged(null, new EventArgs());
            });
        }

        #endregion
    }
}
