using Komodex.Common;
using Komodex.Common.Phone;
using Komodex.DACP;
using Komodex.Remote.Localization;
using Komodex.Remote.ServerManagement;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;

namespace Komodex.Remote.Controls
{
    public partial class ManualPairingDialog : DialogUserControlBase
    {
        protected DACPServer _server;

        public ManualPairingDialog()
        {
            InitializeComponent();
        }

        protected override void Show(ContentPresenter container)
        {
            // Prepare the dialog
            NetworkManager.NetworkAvailabilityChanged += NetworkManager_NetworkAvailabilityChanged;
            UpdateWizardItem(false);

            base.Show(container);
        }

        protected override void DialogService_Closed(object sender, EventArgs e)
        {
            base.DialogService_Closed(sender, e);

            NetworkManager.NetworkAvailabilityChanged -= NetworkManager_NetworkAvailabilityChanged;
        }

        private void NetworkManager_NetworkAvailabilityChanged(object sender, NetworkAvailabilityChangedEventArgs e)
        {
            Utility.BeginInvokeOnUIThread(() => UpdateWizardItem(true));
        }

        protected void UpdateWizardItem(bool useTransitions)
        {
            if (NetworkManager.IsLocalNetworkAvailable)
            {
                if (_server != null)
                {
                    wizard.SetSelectedItem(wizardItemConnecting, useTransitions);
                    leftButton.Content = LocalizedStrings.CancelDialogButton;
                }
                else
                {
                    wizard.SetSelectedItem(wizardItemHostnamePIN, useTransitions);
                    leftButton.Content = LocalizedStrings.ConnectDialogButton;
                }
            }
            else
            {
                wizard.SetSelectedItem(wizardItemWiFi, useTransitions);
                leftButton.Content = LocalizedStrings.WiFiSettingsDialogButton;
            }

            UpdateButtonEnabledState();
        }

        private void leftButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = wizard.SelectedItem;
            if (selectedItem == wizardItemWiFi)
            {
                ConnectionSettingsTask task = new ConnectionSettingsTask();
                task.ConnectionSettingsType = ConnectionSettingsType.WiFi;
                task.Show();
            }
            else if (selectedItem == wizardItemHostnamePIN)
            {
                ConnectToServer();
            }
            else if (selectedItem == wizardItemConnecting)
            {
                // TODO: Cancel connection
            }
        }

        protected bool ParseHostname(out string host, out int port)
        {
            host = null;
            port = 3689;

            string input = hostTextBox.Text.Trim();
            if (string.IsNullOrEmpty(input))
                return false;

            int colonIndex = input.IndexOf(':');
            if (colonIndex > 0)
            {
                host = input.Substring(0, colonIndex);
                string portString = input.Substring(colonIndex + 1);
                bool result = int.TryParse(portString, out port);
                if (result && !string.IsNullOrWhiteSpace(host))
                    return true;
                return false;
            }
            else
            {
                host = input;
                return true;
            }
        }

        protected bool HasValidData()
        {
            string host;
            int port;
            if (!ParseHostname(out host, out port))
                return false;

            return (pinTextBox.Text.Length == 4);
        }

        protected void UpdateButtonEnabledState()
        {
            if (wizard.SelectedItem == wizardItemHostnamePIN)
                leftButton.IsEnabled = HasValidData();
            else
                leftButton.IsEnabled = true;
        }

        private void hostTextBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.PlatformKeyCode == 10 || e.Key == Key.Tab)
                pinTextBox.Focus();

            UpdateButtonEnabledState();
        }

        private void hostTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateButtonEnabledState();
        }

        private void pinTextBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.PlatformKeyCode == 10)
                ConnectToServer();

            UpdateButtonEnabledState();
        }

        private void pinTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateButtonEnabledState();
        }

        #region Server Connections

        protected async void ConnectToServer()
        {
            if (_server != null)
                return;

            if (!HasValidData())
                return;

            string host;
            int port;
            ParseHostname(out host, out port);

            string pairingCode = string.Format("{0:0000}{0:0000}{0:0000}{0:0000}", pinTextBox.IntValue.Value);

            _server = new DACPServer(host, port, pairingCode);

            UpdateWizardItem(true);

            var result = await _server.ConnectAsync();
            switch (result)
            {
                case ConnectionResult.Success:
                    // Save the server connection info
                    ServerConnectionInfo info = new ServerConnectionInfo();
                    info.Name = _server.LibraryName;
                    info.PairingCode = _server.PairingCode;
                    info.LastHostname = _server.Hostname;
                    info.LastIPAddress = _server.Hostname;
                    info.LastPort = _server.Port;

                    // Get the service ID for Bonjour
                    // In iTunes 10.1 and later, the service name comes from ServiceID (parameter aeIM).
                    // In foo_touchremote the service name is the same as the database ID (parameter mper).
                    // In MonkeyTunes, the service ID is not available from the database query. TODO.
                    if (_server.MainDatabase.ServiceID > 0)
                        info.ServiceID = _server.MainDatabase.ServiceID.ToString("x16").ToUpper();
                    else
                        info.ServiceID = _server.MainDatabase.PersistentID.ToString("x16").ToUpper();

                    ServerManager.AddServerInfo(info);
                    ServerManager.ChooseServer(info);

                    Hide();

                    NavigationManager.GoToFirstPage();
                    break;

                case ConnectionResult.InvalidPIN:
                    MessageBox.Show(LocalizedStrings.LibraryPINErrorBody, LocalizedStrings.LibraryPINErrorTitle, MessageBoxButton.OK);
                    _server = null;
                    UpdateWizardItem(true);
                    break;

                case ConnectionResult.ConnectionError:
                    // TODO: Display error
                    _server = null;
                    UpdateWizardItem(true);
                    break;
            }
        }

        #endregion
    }
}
