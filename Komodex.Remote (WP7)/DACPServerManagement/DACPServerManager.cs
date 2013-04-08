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
using Komodex.Remote.DACPServerInfoManagement;
using System.Windows.Controls.Primitives;
using Komodex.Remote.Controls;
using Microsoft.Phone.Controls;
using System.Windows.Data;
using Komodex.Remote.Utilities;
using Komodex.Remote.Settings;
using Komodex.Remote.Localization;
using Komodex.Common.Phone;
using Microsoft.Phone.Net.NetworkInformation;
using System.Linq;
using Komodex.Analytics;
using Komodex.Common;
using Komodex.Remote.Pages.Pairing;

namespace Komodex.Remote.DACPServerManagement
{
    public static class DACPServerManager
    {
        private static readonly Log _log = new Log("DACP Manager");

        private static bool _suppressNavigateToHome = false;
        private static bool _isConnecting = false;
        private static bool _tryToReconnect = false;
        private static bool _isObscured = false;
        private static bool _connectOnNetworkAvailable = false;
        private static bool _clearPageHistoryOnNavigate;

        #region Properties

        private static DACPServer _Server = null;
        public static DACPServer Server
        {
            get { return _Server; }
            private set
            {
                _isConnecting = false;

                if (_Server != null)
                {
                    _Server.ServerUpdate -= new EventHandler<ServerUpdateEventArgs>(Server_ServerUpdate);
                    _Server.Stop();
                }

                _Server = value;

                if (_Server != null)
                {
                    _Server.ServerUpdate += new EventHandler<ServerUpdateEventArgs>(Server_ServerUpdate);
                }

                UpdatePopupDisplay();
                SendServerChanged();
                if (_suppressNavigateToHome)
                    _suppressNavigateToHome = false;
                else
                    NavigationManager.GoToFirstPage();
            }
        }

        private static bool _showPopups = true;
        public static bool ShowPopups
        {
            get { return _showPopups; }
            set
            {
                if (_showPopups == value)
                    return;
                _showPopups = value;
                UpdatePopupDisplay();
            }
        }

        private static bool _isNetworkAvailable;
        public static bool IsNetworkAvailable
        {
            get { return _isNetworkAvailable; }
            private set
            {
                if (_isNetworkAvailable == value)
                    return;
                _isNetworkAvailable = value;
                UpdatePopupDisplay();
            }
        }

        #endregion

        #region Network Availability

        private static void UpdateNetworkInterfaceAvailability()
        {
            _log.Info("Updating network availability...");

            NetworkInterfaceList interfaces = new NetworkInterfaceList();

            if (interfaces != null)
            {
#if DEBUG
                if (_log.EffectiveLevel <= LogLevel.Debug)
                {
                    string interfaceFormat = "[{0}]\n\tDescription: {1}\n\tType: {2}\n\tSubtype: {3}\n\tState: {4}\n\tBandwidth: {5}\n\n";
                    string interfaceList = string.Empty;
                    foreach (var i in interfaces)
                        interfaceList += string.Format(interfaceFormat, i.InterfaceName, i.Description,  i.InterfaceType, i.InterfaceSubtype,i.InterfaceState, i.Bandwidth);
                    _log.Debug("Network interfaces:\n" + interfaceList.TrimEnd());
                }
#endif

                IsNetworkAvailable = interfaces.Any(i => i.InterfaceState == ConnectState.Connected
                    && !(i.Bandwidth == -1 && (i.InterfaceName.Contains("Loopback") || i.Description.Contains("Loopback") || i.InterfaceName.Contains("{22C7611B-530E-11DB-BA31-806E6F6E6963}")))
                    && (i.InterfaceType == NetworkInterfaceType.Wireless80211 || i.InterfaceType == NetworkInterfaceType.Ethernet));
            }
            else
            {
                IsNetworkAvailable = false;
            }

            _log.Info("Network available: " + IsNetworkAvailable);

            if (IsNetworkAvailable && _connectOnNetworkAvailable)
            {
                _tryToReconnect = true;
                TryToReconnect();
                _connectOnNetworkAvailable = false;
            }
        }

        private static void DeviceNetworkInformation_NetworkAvailabilityChanged(object sender, NetworkNotificationEventArgs e)
        {
            _log.Info("Network availability changed. Name: '{0}' Type: '{1}'", e.NetworkInterface.InterfaceName, e.NotificationType);

            // TODO: Do something better than just refreshing the entire list
            UpdateNetworkInterfaceAvailability();
        }

        #endregion

        #region Popups

        private static Popup ConnectingPopup = null;
        private static ConnectingStatusControl ConnectingStatusControl = null;

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
                bool canShow = ShowPopups && !(currentPage is LibraryChooserPage || currentPage is AddLibraryPage || currentPage is AboutPage || currentPage is ManualPairingPage);
                bool hasServers = (DACPServerViewModel.Instance.Items.Count > 0);

                if (currentPage is MainPage)
                    ConnectingPopup.IsOpen = (connecting && canShow && hasServers);
                else
                    ConnectingPopup.IsOpen = (connecting && canShow);

                if (ConnectingPopup.IsOpen)
                    ConnectingStatusControl.UpdateFromServer();
            });
        }

        static void ConnectingStatusControl_ButtonClick(object sender, RoutedEventArgs e)
        {
            if (Server != null)
            {
                _suppressNavigateToHome = true;
                Server = null;
            }

            _clearPageHistoryOnNavigate = true;

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

            if (_clearPageHistoryOnNavigate)
            {
                _clearPageHistoryOnNavigate = false;
                NavigationManager.ClearPageHistory();
            }
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
        }

        #endregion

        #region Running Under Lock Screen

        static void RootVisual_Obscured(object sender, ObscuredEventArgs e)
        {
            _isObscured = true;
        }

        static void RootVisual_Unobscured(object sender, EventArgs e)
        {
            UpdateNetworkInterfaceAvailability();
            TryToReconnect();
            _isObscured = false;
        }

        #endregion

        #region Public Methods

        public static void DoFirstLoad(PhoneApplicationFrame frame)
        {
            DeviceNetworkInformation.NetworkAvailabilityChanged += new EventHandler<NetworkNotificationEventArgs>(DeviceNetworkInformation_NetworkAvailabilityChanged);
            UpdateNetworkInterfaceAvailability();

            RootVisual = frame;

            RootVisual.OrientationChanged += new EventHandler<OrientationChangedEventArgs>(RootVisual_OrientationChanged);
            RootVisual.SizeChanged += new SizeChangedEventHandler(RootVisual_SizeChanged);
            RootVisual.Navigating += new System.Windows.Navigation.NavigatingCancelEventHandler(RootVisual_Navigating);
            RootVisual.Navigated += new System.Windows.Navigation.NavigatedEventHandler(RootVisual_Navigated);
            RootVisual.Obscured += new EventHandler<ObscuredEventArgs>(RootVisual_Obscured);
            RootVisual.Unobscured += new EventHandler(RootVisual_Unobscured);

            if (TrialManager.Current.IsTrial)
                return;

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

                if (!IsNetworkAvailable)
                {
                    _connectOnNetworkAvailable = true;
                    UpdatePopupDisplay();
                    return;
                }

                _isConnecting = true;
                Server.Start();
            }
        }

        public static void ApplicationActivated()
        {
            UpdateNetworkInterfaceAvailability();
            TryToReconnect();
        }

        public static void ApplicationDeactivated()
        {
            _isConnecting = false;
            if (Server != null)
                Server.Stop();
        }

        #endregion

        #region Event Handlers

        static void Server_ServerUpdate(object sender, ServerUpdateEventArgs e)
        {
            if (sender != Server)
                return;

            _isConnecting = false;

            switch (e.Type)
            {
                case ServerUpdateType.ServerConnected:
                    _log.Info("Server connected successfully.");
                    _tryToReconnect = true;
                    UpdatePopupDisplay();
                    UpdateSavedLibraryName();
                    break;

                case ServerUpdateType.Error:
                    if (_isObscured)
                        break;

                    _log.Info("Server disconnected.");

                    TryToReconnect();

                    if (SettingsManager.Current.ExtendedErrorReporting && e.ErrorType == ServerErrorType.General && !string.IsNullOrEmpty(e.ErrorDetails))
                        CrashReporter.LogMessage(e.ErrorDetails, "Caught DACP Server Error", true);
                    break;

                case ServerUpdateType.LibraryError:
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        NowPlaying.NowPlayingPage.GoBackOnNextLoad = true;
                        MessageBox.Show(LocalizedStrings.LibraryErrorBody, LocalizedStrings.LibraryErrorTitle, MessageBoxButton.OK);
                    });
                    break;

                default:
                    break;
            }
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

        #region Methods

        private static void UpdateSavedLibraryName()
        {
            if (Server == null || !Server.IsConnected)
                return;

            DACPServerInfo serverInfo = DACPServerViewModel.Instance.CurrentDACPServer;
            if (serverInfo == null || serverInfo.ID != Server.ID)
                return;

            serverInfo.LibraryName = Server.LibraryName;

            DACPServerViewModel.Instance.Save();
        }

        private static void TryToReconnect()
        {
            if (Server != null && !Server.IsConnected && !_isConnecting)
            {
                if (!IsNetworkAvailable)
                {
                    _connectOnNetworkAvailable = true;
                    UpdatePopupDisplay();
                    return;
                }

                if (_tryToReconnect)
                {
                    _log.Info("Trying to reconnect...");
                    _tryToReconnect = false;
                    _isConnecting = true;
                    Server.Start();
                    UpdatePopupDisplay();
                }
                else
                {
                    Server = null;
                }
            }
        }

        #endregion
    }
}
