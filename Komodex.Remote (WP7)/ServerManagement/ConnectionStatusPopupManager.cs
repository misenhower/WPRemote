using Komodex.Common;
using Komodex.Remote.Controls;
using Komodex.Remote.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls.Primitives;

namespace Komodex.Remote.ServerManagement
{
    public static class ConnectionStatusPopupManager
    {
        private static Popup _popup;
        private static ConnectingStatusControl _statusControl;

        private static bool _canDisplay;
        private static bool _showProgressBar;

        public static bool IsVisible { get; private set; }

        public static void Initialize()
        {
            // Popup display size
            App.RootFrame.OrientationChanged += (sender, e) => UpdateSize();
            App.RootFrame.SizeChanged += (sender, e) => UpdateSize();

            // Navigation and server updates
            App.RootFrame.Navigated += (sender, e) => UpdatePopupVisibility();
            ServerManager.ConnectionStateChanged += (sender, e) => UpdatePopupContent();

            // Complete initialization on first navigation
            App.RootFrame.Navigated += CompleteInitialization;
        }

        private static void CompleteInitialization(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            App.RootFrame.Navigated -= CompleteInitialization;

            _popup = new Popup();
            _statusControl = new ConnectingStatusControl();
            _statusControl.ButtonClick += StatusControl_ButtonClick;
            _popup.Child = _statusControl;

            UpdateSize();
            UpdatePopupContent();
        }

        private static void UpdateSize()
        {
            if (_statusControl == null)
                return;

            _statusControl.Width = App.RootFrame.ActualWidth;
            _statusControl.Height = App.RootFrame.ActualHeight;
        }

        private static void UpdatePopupContent()
        {
            if (_statusControl == null)
                return;

            Utility.BeginInvokeOnUIThread(() =>
            {
                switch (ServerManager.ConnectionState)
                {
                    case ServerConnectionState.NoLibrarySelected:
                        _canDisplay = true;
                        _showProgressBar = false;
                        _statusControl.LibraryConnectionText = LocalizedStrings.StatusTapChooseLibrary;
                        _statusControl.LibraryName = string.Empty;
                        break;

                    case ServerConnectionState.WaitingForWiFiConnection:
                        _canDisplay = true;
                        _showProgressBar = true;
                        _statusControl.LibraryConnectionText = LocalizedStrings.WaitingForWiFiConnection;
                        _statusControl.LibraryName = string.Empty;
                        break;

                    case ServerConnectionState.LookingForLibrary:
                        _canDisplay = true;
                        _showProgressBar = true;
                        _statusControl.LibraryConnectionText = "Looking for Library"; // TODO: Localization
                        _statusControl.LibraryName = ServerManager.SelectedServerInfo.Name;
                        break;

                    case ServerConnectionState.ConnectingToLibrary:
                        _canDisplay = true;
                        _showProgressBar = true;
                        _statusControl.LibraryConnectionText = LocalizedStrings.StatusConnectingToLibrary;
                        _statusControl.LibraryName = ServerManager.SelectedServerInfo.Name;
                        break;

                    case ServerConnectionState.Connected:
                    default:
                        _canDisplay = false;
                        _showProgressBar = false;
                        _statusControl.LibraryConnectionText = string.Empty;
                        _statusControl.LibraryName = string.Empty;
                        break;
                }

                UpdatePopupVisibility();
            });
        }

        public static void UpdatePopupVisibility()
        {
            if (_statusControl == null)
                return;

            Utility.BeginInvokeOnUIThread(() =>
            {
                bool visible = _canDisplay;
                RemoteBasePage page = App.RootFrame.Content as RemoteBasePage;

                if (_canDisplay)
                {
                    if (page != null && page.DisableConnectionStatusPopup)
                        visible = false;
                }

                if (visible)
                {
                    _statusControl.ShowProgressBar = _showProgressBar;
                    _popup.IsOpen = true;
                }
                else
                {
                    _statusControl.ShowProgressBar = false;
                    _popup.IsOpen = false;
                }

                IsVisible = visible;
                if (page != null)
                    page.UpdateApplicationBarVisibility();
            });
        }

        private static void StatusControl_ButtonClick(object sender, System.Windows.RoutedEventArgs e)
        {
            NavigationManager.OpenChooseLibraryPage();
        }
    }
}
