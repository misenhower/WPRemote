﻿using System;
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
        DACPServerInfo serverInfo = null;
        DACPServer server = null;

        public AddLibraryPage()
        {
            InitializeComponent();

            connectingStatusControl.ButtonText = LocalizedStrings.CancelButton;

            InitializeApplicationBar();

            // Server info
            serverInfo = new DACPServerInfo();
            serverInfo.ID = Guid.NewGuid();
            DataContext = serverInfo;

            AnimationContext = LayoutRoot;

            // Setting this.IsTabStop = true so we can set focus to it later
            IsTabStop = true;

            UpdateAppBar();

#if DEBUG
            // Default field content to make debugging easier
            if (DACPServerViewModel.Instance.Items.Count == 0)
            {
                serverInfo.HostName = "10.0.0.40";
                serverInfo.PIN = 1111;
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
            if (server != null)
            {
                StopServer();
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
            AddApplicationBarIconButton(LocalizedStrings.SaveAppBarButton, "/icons/appbar.check.rest.png", () => SaveServer());

            // Cancel
            AddApplicationBarIconButton(LocalizedStrings.CancelAppBarButton, "/icons/appbar.cancel.rest.png", () => NavigationService.GoBack());

            // About
            AddApplicationBarMenuItem(LocalizedStrings.AboutMenuItem, OpenAboutPage);
        }

        private void SaveServer()
        {
            if (!HasValidData())
                return;

            // Make sure the newly entered data has been bound to the DACPServerInfo object
            PhoneUtility.BindFocusedTextBox();

            // Remove focus from textbox
            Focus();

            // Clear cached version info
            iTunesVersion = null;
            iTunesProtocolVersion = iTunesDMAPVersion = iTunesDAAPVersion = 0;

            // Validate the server info
            SetVisibility(true);
            server = new DACPServer(serverInfo.HostName, serverInfo.PairingCode);
            server.ServerUpdate += new EventHandler<ServerUpdateEventArgs>(server_ServerUpdate);
            server.Start(false);
        }

        protected void UpdateAppBar()
        {
            ApplicationBarIconButton saveButton = (ApplicationBarIconButton)ApplicationBar.Buttons[0];

            saveButton.IsEnabled = HasValidData();
        }

        protected bool HasValidData()
        {
            return (!string.IsNullOrEmpty(tbHost.Text.Trim()) && tbPIN.Text.Length == 4);
        }

        #endregion

        #region Diagnostic Data

        private string iTunesVersion = null;
        private int iTunesProtocolVersion = 0;
        private int iTunesDMAPVersion = 0;
        private int iTunesDAAPVersion = 0;

        private void OpenAboutPage()
        {
            NavigationManager.OpenAboutPage(iTunesVersion ?? string.Empty, iTunesProtocolVersion, iTunesDMAPVersion, iTunesDAAPVersion);
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

        private void tbPIN_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.PlatformKeyCode == 10)
                SaveServer();

            UpdateAppBar();
        }

        private void tbPIN_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateAppBar();
        }

        private void connectingStatusControl_ButtonClick(object sender, RoutedEventArgs e)
        {
            StopServer();
        }

        #endregion

        #region Server Validation

        void SetVisibility(bool isConnecting)
        {
            ApplicationBar.IsVisible = !isConnecting;
            connectingStatusControl.ShowProgressBar = isConnecting;
            connectingStatusControl.Visibility = (isConnecting) ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        }

        void server_ServerUpdate(object sender, ServerUpdateEventArgs e)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                switch (e.Type)
                {
                    case ServerUpdateType.ServerConnected:
                        // PIN was correct
                        serverInfo.LibraryName = server.LibraryName;
                        server.Stop();
                        server = null;
                        DACPServerViewModel.Instance.Items.Add(serverInfo);
                        DACPServerManager.ConnectToServer(serverInfo.ID);

                        // Update trial expiration date
                        if (TrialManager.Current.TrialExpirationDate == DateTime.MinValue)
                            TrialManager.Current.ResetTrialExpiration();

                        NavigationService.GoBack();
                        break;
                    case ServerUpdateType.Error:
                        // Cache the version info
                        iTunesVersion = server.ServerVersionString;
                        iTunesProtocolVersion = server.ServerVersion;
                        iTunesDMAPVersion = server.ServerDMAPVersion;
                        iTunesDAAPVersion = server.ServerDAAPVersion;

                        if (e.ErrorType == ServerErrorType.UnsupportedVersion)
                            MessageBox.Show(LocalizedStrings.LibraryVersionErrorBody, LocalizedStrings.LibraryVersionErrorTitle, MessageBoxButton.OK);
                        else if (e.ErrorType == ServerErrorType.InvalidPIN)
                            MessageBox.Show(LocalizedStrings.LibraryPINErrorBody, LocalizedStrings.LibraryPINErrorTitle, MessageBoxButton.OK);
                        else if (RemoteUtility.CheckNetworkConnectivity())
                            MessageBox.Show(LocalizedStrings.LibraryConnectionErrorBody, LocalizedStrings.LibraryConnectionErrorTitle, MessageBoxButton.OK);
                        SetVisibility(false);
                        server = null;
                        break;
                    default:
                        break;
                }
            });
        }

        #endregion

        #region Methods

        private void StopServer()
        {
            if (server != null)
            {
                server.Stop();
                server = null;
            }
            SetVisibility(false);
        }

        #endregion
    }
}