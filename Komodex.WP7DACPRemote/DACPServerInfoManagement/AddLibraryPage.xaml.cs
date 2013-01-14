using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Komodex.DACP;
using Komodex.WP7DACPRemote.DACPServerManagement;
using System.Text.RegularExpressions;
using Clarity.Phone.Controls;
using Microsoft.Phone.Shell;
using Komodex.WP7DACPRemote.Localization;
using Komodex.Common.Phone;

namespace Komodex.WP7DACPRemote.DACPServerInfoManagement
{
    public partial class AddLibraryPage : PhoneApplicationBasePage
    {
        DACPServer _currentServer;

        public AddLibraryPage()
        {
            InitializeComponent();

            connectingStatusControl.ButtonText = LocalizedStrings.CancelButton;

            InitializeApplicationBar();

            AnimationContext = LayoutRoot;

            // Setting this.IsTabStop = true so we can set focus to it later
            IsTabStop = true;

            UpdateAppBar();

#if DEBUG
            // Default field content to make debugging easier
            if (DACPServerViewModel.Instance.Items.Count == 0)
            {
                tbHost.Text = "10.0.0.1";
                tbPIN.Text = "1111";
            }
#endif
        }

        #region Overrides

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            this.PreserveState(tbHost);
            this.PreserveState(tbPIN);
            //StateUtils.PreserveFocusState(State, ContentPanel);

            State[StateUtils.SavedStateKey] = true;
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (State.ContainsKey(StateUtils.SavedStateKey))
            {
                this.RestoreState(tbHost, string.Empty);
                this.RestoreState(tbPIN, string.Empty);
                //StateUtils.RestoreFocusState(State, ContentPanel);
            }

            UpdateAppBar();
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            if (_currentServer != null)
            {
                StopServerConnection();
                e.Cancel = true;
                return;
            }

            base.OnBackKeyPress(e);
        }

        #endregion

        #region Application Bar

        protected override void InitializeApplicationBar()
        {
            base.InitializeApplicationBar();

            // Save
            AddApplicationBarIconButton(LocalizedStrings.SaveAppBarButton, "/icons/appbar.check.rest.png", () => VerifyServer());

            // Cancel
            AddApplicationBarIconButton(LocalizedStrings.CancelAppBarButton, "/icons/appbar.cancel.rest.png", () => NavigationService.GoBack());

            // About
            AddApplicationBarMenuItem(LocalizedStrings.AboutMenuItem, OpenAboutPage);
        }

        protected void UpdateAppBar()
        {
            ApplicationBarIconButton saveButton = (ApplicationBarIconButton)ApplicationBar.Buttons[0];

            saveButton.IsEnabled = HasValidData();
        }

        #endregion

        #region Diagnostic Data

        private string _iTunesVersion = null;
        private int _iTunesProtocolVersion = 0;
        private int _iTunesDMAPVersion = 0;
        private int _iTunesDAAPVersion = 0;

        private void OpenAboutPage()
        {
            NavigationManager.OpenAboutPage(_iTunesVersion ?? string.Empty, _iTunesProtocolVersion, _iTunesDMAPVersion, _iTunesDAAPVersion);
        }

        #endregion

        #region Control Event Handlers

        private void tbHost_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.PlatformKeyCode == 10 || e.Key == Key.Tab)
                tbPIN.Focus();

            UpdateAppBar();
        }

        private void tbHost_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateAppBar();
        }

        private void tbHost_LostFocus(object sender, RoutedEventArgs e)
        {
            tbHost.Text = tbHost.Text.Trim();
        }

        private void tbPIN_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.PlatformKeyCode == 10)
                VerifyServer();

            UpdateAppBar();
        }

        private void tbPIN_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateAppBar();
        }

        private void connectingStatusControl_ButtonClick(object sender, RoutedEventArgs e)
        {
            StopServerConnection();
        }

        #endregion

        #region Server Validation

        protected void SetStatusOverlayVisibility(bool visible)
        {
            ApplicationBar.IsVisible = !visible;
            connectingStatusControl.ShowProgressBar = visible;
            connectingStatusControl.Visibility = (visible) ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        }

        protected void server_ServerUpdate(object sender, ServerUpdateEventArgs e)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                switch (e.Type)
                {
                    case ServerUpdateType.ServerConnected:
                        // PIN was correct
                        DACPServerInfo serverInfo = SaveServerInfo(_currentServer);
                        StopServerConnection();

                        // Update trial expiration date
                        if (TrialManager.Current.TrialExpirationDate == DateTime.MinValue)
                            TrialManager.Current.ResetTrialExpiration();

                        DACPServerManager.ConnectToServer(serverInfo.ID);

                        NavigationService.GoBack();
                        break;
                    case ServerUpdateType.Error:
                        // Cache the version info
                        _iTunesVersion = _currentServer.ServerVersionString;
                        _iTunesProtocolVersion = _currentServer.ServerVersion;
                        _iTunesDMAPVersion = _currentServer.ServerDMAPVersion;
                        _iTunesDAAPVersion = _currentServer.ServerDAAPVersion;

                        if (e.ErrorType == ServerErrorType.UnsupportedVersion)
                            MessageBox.Show(LocalizedStrings.LibraryVersionErrorBody, LocalizedStrings.LibraryVersionErrorTitle, MessageBoxButton.OK);
                        else if (e.ErrorType == ServerErrorType.InvalidPIN)
                            MessageBox.Show(LocalizedStrings.LibraryPINErrorBody, LocalizedStrings.LibraryPINErrorTitle, MessageBoxButton.OK);
                        else if (RemoteUtility.CheckNetworkConnectivity())
                            MessageBox.Show(LocalizedStrings.LibraryConnectionErrorBody, LocalizedStrings.LibraryConnectionErrorTitle, MessageBoxButton.OK);
                        SetStatusOverlayVisibility(false);
                        _currentServer = null;
                        break;
                    default:
                        break;
                }
            });
        }

        #endregion

        #region Methods

        protected bool HasValidData()
        {
            return (!string.IsNullOrEmpty(tbHost.Text.Trim()) && tbPIN.Text.Length == 4);
        }

        private void VerifyServer()
        {
            // Remove focus from textbox so the on-screen keyboard disappears
            Focus();

            StopServerConnection();

            if (!HasValidData() || !tbPIN.IntValue.HasValue)
                return;

            string pairingKey = string.Format("{0:0000}{0:0000}{0:0000}{0:0000}", tbPIN.IntValue.Value);

            // Clear cached version info
            _iTunesVersion = null;
            _iTunesProtocolVersion = _iTunesDMAPVersion = _iTunesDAAPVersion = 0;

            // Validate the server info
            SetStatusOverlayVisibility(true);
            _currentServer = new DACPServer(tbHost.Text.Trim(), pairingKey);
            _currentServer.ServerUpdate += server_ServerUpdate;
            _currentServer.Start(false);
        }

        private void StopServerConnection()
        {
            if (_currentServer != null)
            {
                _currentServer.ServerUpdate -= server_ServerUpdate;
                _currentServer.Stop();
                _currentServer = null;
            }
            SetStatusOverlayVisibility(false);
        }

        protected DACPServerInfo SaveServerInfo(DACPServer server)
        {
            DACPServerInfo serverInfo = new DACPServerInfo();
            serverInfo.ID = Guid.NewGuid();
            serverInfo.HostName = server.HostName;
            serverInfo.PIN = int.Parse(server.PairingKey.Substring(0, 4)); // TODO: Fix this, should store the whole pairing key instead
            serverInfo.LibraryName = server.LibraryName;

            // Get the service ID for Bonjour
            // In iTunes 10.1 and later, the service name comes from ServiceID (parameter aeIM).
            // In foo_touchremote the service name is the same as the database ID (parameter mper).
            // In MonkeyTunes, the service ID is not available from the database query. TODO.
            if (server.ServiceID > 0)
                serverInfo.ServiceID = server.ServiceID.ToString("x16").ToUpper();
            else
                serverInfo.ServiceID = server.DatabasePersistentID.ToString("x16").ToUpper();

            // Save to the list of servers
            DACPServerViewModel.Instance.Items.Add(serverInfo);

            return serverInfo;
        }

        #endregion

    }
}