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

namespace Komodex.WP7DACPRemote.DACPServerInfoManagement
{
    public partial class AddLibraryPage : AnimatedBasePage
    {
        DACPServerInfo serverInfo = null;
        DACPServer server = null;

        public AddLibraryPage()
        {
            InitializeComponent();

            serverInfo = new DACPServerInfo();
            serverInfo.ID = Guid.NewGuid();
            DataContext = serverInfo;

            AnimationContext = LayoutRoot;

            // Setting this.IsTabStop = true so we can set focus to it later
            IsTabStop = true;

            UpdateAppBar();
        }

        private string iTunesVersion = null;
        private int iTunesProtocolVersion = 0;
        private int iTunesDMAPVersion = 0;
        private int iTunesDAAPVersion = 0;

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

        #region AppBar Button Handlers

        private void UpdateBoundData()
        {
            try
            {
                TextBox tb = (TextBox)FocusManager.GetFocusedElement();
                var binding = tb.GetBindingExpression(TextBox.TextProperty);
                binding.UpdateSource();
            }
            catch { }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            SaveServer();
        }

        private void SaveServer()
        {
            if (!HasValidData())
                return;

            // Make sure the newly entered data has been bound to the DACPServerInfo object
            UpdateBoundData();

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

        private void btnCancel_Click(object sender, EventArgs e)
        {
            NavigationService.GoBack();
        }

        #endregion

        #region Control Event Handlers

        private void mnuAbout_Click(object sender, EventArgs e)
        {
            NavigationManager.OpenAboutPage(iTunesVersion ?? string.Empty, iTunesProtocolVersion, iTunesDMAPVersion, iTunesDAAPVersion);
        }

        private void tbHost_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Tab)
                tbPIN.Focus();

            UpdateAppBar();
        }

        private void tbPIN_KeyDown(object sender, KeyEventArgs e)
        {
            // Only allow numeric characters
            bool validCharacter = (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9) || (e.Key >= Key.D0 && e.Key <= Key.D9);

            if (!validCharacter)
                e.Handled = true;
        }

        private void tbPIN_KeyUp(object sender, KeyEventArgs e)
        {
            TextBox textBox = (TextBox)sender;

            int selectionStart = textBox.SelectionStart;
            int textLen = textBox.Text.Length;

            textBox.Text = Regex.Replace(textBox.Text, "\\D", string.Empty);

            int newTextLen = textBox.Text.Length;
            if (newTextLen < textLen && selectionStart > 0)
                selectionStart--;

            textBox.SelectionStart = selectionStart;

            if (e.Key == Key.Enter)
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
                        NavigationService.GoBack();
                        break;
                    case ServerUpdateType.Error:
                        // Cache the version info
                        iTunesVersion = server.ServerVersionString;
                        iTunesProtocolVersion = server.ServerVersion;
                        iTunesDMAPVersion = server.ServerDMAPVersion;
                        iTunesDAAPVersion = server.ServerDAAPVersion;

                        if (e.ErrorType == ServerErrorType.UnsupportedVersion)
                            MessageBox.Show("This application currently requires iTunes version 10.1 or higher. "
                                + "Please upgrade to the latest version of iTunes to continue.", "iTunes Version Error", MessageBoxButton.OK);
                        else if (e.ErrorType == ServerErrorType.InvalidPIN)
                            MessageBox.Show("Could not connect to iTunes. Please check the PIN and try again.", "PIN Error", MessageBoxButton.OK);
                        else if (Utility.CheckNetworkConnectivity())
                            MessageBox.Show("Could not connect to iTunes. Please make sure your phone is connected to the correct Wi-fi network "
                                + "and check the value entered in the hostname field.", "Connection Error", MessageBoxButton.OK);
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

        protected void UpdateAppBar()
        {
            ApplicationBarIconButton saveButton = (ApplicationBarIconButton)ApplicationBar.Buttons[0];

            saveButton.IsEnabled = HasValidData();
        }

        protected bool HasValidData()
        {
            //return (!string.IsNullOrEmpty(serverInfo.HostName) && serverInfo.PIN.HasValue && serverInfo.PIN.Value != 0);
            //return (!string.IsNullOrEmpty(tbHost.Text.Trim()) && !string.IsNullOrEmpty(tbPIN.Text.Trim()));
            return (!string.IsNullOrEmpty(tbHost.Text.Trim()) && tbPIN.Text.Length == 4);
        }

        #endregion
    }
}