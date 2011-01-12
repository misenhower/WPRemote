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
using System.Windows.Data;

namespace Komodex.WP7DACPRemote.DACPServerManagement
{
    public static class DACPServerManager
    {
        private static bool _suppressNavigateToHome = false;

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
                UpdateServerBusyIndicator();
                SendServerChanged();
                if (_suppressNavigateToHome)
                    _suppressNavigateToHome = false;
                else
                    NavigationManager.GoToFirstPage();
            }
        }

        #endregion

        #region Popups

        private static Popup ConnectingPopup = null;
        private static ConnectingStatusControl ConnectingStatusControl = null;

        private static Popup ServerBusyPopup = null;
        private static ProgressBar ServerBusyProgressBar = null;

        private static void UpdatePopupDisplay()
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (ConnectingPopup == null)
                {
                    ConnectingStatusControl = new ConnectingStatusControl(true);
                    UpdatePopupSize();
                    ConnectingStatusControl.ButtonClick += new RoutedEventHandler(ConnectingStatusControl_ButtonClick);
                    ConnectingPopup = new Popup();
                    ConnectingPopup.Child = ConnectingStatusControl;
                }

                PhoneApplicationPage currentPage = GetCurrentPhoneApplicationPage();
                if (currentPage == null)
                    return;

                bool connecting = (Server == null || !Server.IsConnected);
                bool canShow = !(currentPage is LibraryChooserPage || currentPage is AddLibraryPage || currentPage is AboutPage);
                bool hasServers = (DACPServerViewModel.Instance.Items.Count > 0);

                if (currentPage is MainPage)
                    ConnectingPopup.IsOpen = (connecting && canShow && hasServers);
                else
                    ConnectingPopup.IsOpen = (connecting && canShow);
            });
        }


        private static void UpdateServerBusyIndicator()
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (ServerBusyPopup == null)
                {
                    ServerBusyProgressBar = new ProgressBar();
                    ServerBusyProgressBar.Padding = new Thickness(0);
                    ServerBusyProgressBar.Margin = new Thickness(0, 32, 0, 0);
                    ServerBusyProgressBar.Style = (Style)Application.Current.Resources["PerformanceProgressBar"];
                    UpdatePopupSize();
                    ServerBusyProgressBar.Visibility = Visibility.Collapsed;
                    ServerBusyPopup = new Popup();
                    ServerBusyPopup.Child = ServerBusyProgressBar;
                }

                if (Server != null)
                {
                    // Set up progressbar bindings
                    Binding indeterminateBinding = new Binding("GettingData");
                    indeterminateBinding.Source = Server;
                    ServerBusyProgressBar.SetBinding(ProgressBar.IsIndeterminateProperty, indeterminateBinding);
                    Binding visibilityBinding = new Binding("GettingData");
                    visibilityBinding.Source = Server;
                    visibilityBinding.Converter = Application.Current.Resources["booleanToVisibilityConverter"] as IValueConverter;
                    ServerBusyProgressBar.SetBinding(ProgressBar.VisibilityProperty, visibilityBinding);
                }
                else
                {
                    ServerBusyProgressBar.IsIndeterminate = false;
                    ServerBusyProgressBar.Visibility = Visibility.Collapsed;
                }

                ServerBusyPopup.IsOpen = true;
            });
        }

        static void ConnectingStatusControl_ButtonClick(object sender, RoutedEventArgs e)
        {
            if (Server != null)
                Server = null;

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
            if (ConnectingStatusControl != null)
            {
                ConnectingStatusControl.Width = width;
                ConnectingStatusControl.Height = height;
            }

            if (ServerBusyProgressBar != null)
            {
                ServerBusyProgressBar.Width = width;
            }
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

            if (DACPServerViewModel.Instance.CurrentDACPServer != null)
                ConnectToServer();
        }

        public static void ConnectToServer(Guid serverID)
        {
            DACPServerViewModel.Instance.SelectedServerGuid = serverID;
            ConnectToServer();
        }

        public static void ConnectToServer(bool suppressNavigateToHome = false)
        {
            _suppressNavigateToHome = suppressNavigateToHome;

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
            if (sender != Server)
                return;

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
            if (sender != Server)
                return;

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
