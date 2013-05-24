using Komodex.Bonjour;
using Komodex.Common;
using Komodex.Common.Phone;
using Komodex.DACP;
using Komodex.Remote.Localization;
using Komodex.Remote.Pairing;
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
    public partial class UtilityPairingDialog : DialogUserControlBase
    {
        protected readonly Log _log = new Log("Utility Pairing Dialog");

        protected DACPServer _server;
        protected NetService _utilityService;
        protected NetService _libraryService;

        public UtilityPairingDialog()
        {
            InitializeComponent();
        }

        protected override void Show(ContentPresenter container)
        {
            // Prepare the dialog
            NetworkManager.NetworkAvailabilityChanged += NetworkManager_NetworkAvailabilityChanged;
            UpdateWizardItem(false);
            StartManualPairingManager();

            base.Show(container);

            _dialogService.HideOnNavigate = false;
        }

        protected override void DialogService_Closed(object sender, EventArgs e)
        {
            base.DialogService_Closed(sender, e);

            NetworkManager.NetworkAvailabilityChanged -= NetworkManager_NetworkAvailabilityChanged;
            StopManualPairingManager();
        }

        private void NetworkManager_NetworkAvailabilityChanged(object sender, NetworkAvailabilityChangedEventArgs e)
        {
            // TODO: NetworkManager may need to indicate whether an update is occurring because of launch/resume.
            // In this case, update without animation transitions.
            Utility.BeginInvokeOnUIThread(() => UpdateWizardItem(true));
        }

        protected void UpdateWizardItem(bool useTransitions)
        {
            if (NetworkManager.IsLocalNetworkAvailable)
            {
                if (_server != null)
                {
                    wizard.SetSelectedItem(wizardItemConnecting, useTransitions);
                    leftButton.Content = "cancel";
                }
                else if (ManualPairingManager.DiscoveredPairingUtilities.Count > 0)
                {
                    wizard.SetSelectedItem(wizardItemEnterPIN, useTransitions);
                    leftButton.Content = "connect";
                }
                else
                {
                    wizard.SetSelectedItem(wizardItemWaitingForUtility, useTransitions);
                    leftButton.Content = "cancel";
                }
            }
            else
            {
                wizard.SetSelectedItem(wizardItemWiFi, useTransitions);
                leftButton.Content = "wi-fi settings";
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
            else if (selectedItem == wizardItemEnterPIN)
            {
                ConnectToServer();
            }
            else if (selectedItem == wizardItemConnecting)
            {
                // TODO: Cancel connection
            }
            else
            {
                Hide();
            }
        }

        protected bool IsPINValid()
        {
            return (pinTextBox.Text.Length == 4);
        }

        protected void UpdateButtonEnabledState()
        {
            if (wizard.SelectedItem == wizardItemEnterPIN)
                leftButton.IsEnabled = IsPINValid();
            else
                leftButton.IsEnabled = true;
        }

        private void libraryPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            pinTextBox.Text = string.Empty;
        }

        private void pinTextBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.PlatformKeyCode == 10)
                ConnectToServer();
        }

        private void pinTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateButtonEnabledState();
        }

        #region Manual Pairing Manager

        protected void StartManualPairingManager()
        {
            ManualPairingManager.DiscoveredPairingUtilities.CollectionChanged += DiscoveredPairingUtilities_CollectionChanged;
            libraryPicker.ItemsSource = ManualPairingManager.DiscoveredPairingUtilities;
            ManualPairingManager.SearchForPairingUtility();
        }

        protected void StopManualPairingManager()
        {
            ManualPairingManager.DiscoveredPairingUtilities.CollectionChanged -= DiscoveredPairingUtilities_CollectionChanged;
            ManualPairingManager.StopSearchingForPairingUtility();
        }

        private void DiscoveredPairingUtilities_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Utility.BeginInvokeOnUIThread(() => UpdateWizardItem(true));
        }

        #endregion

        #region Server Connections

        protected void ConnectToServer()
        {
            if (_server != null)
                return;

            if (!IsPINValid())
                return;

            DiscoveredPairingUtility utility = libraryPicker.SelectedItem as DiscoveredPairingUtility;
            if (utility == null)
                return;

            _utilityService = utility.Service;

            string serviceName = _utilityService.Name; // TODO: This should come from the dictionary instead
            if (!BonjourManager.DiscoveredServers.ContainsKey(serviceName))
            {
                MessageBox.Show("The selected library could not be located. Make sure iTunes (or another compatible application) is running on your computer and try again.", "Connection error", MessageBoxButton.OK);
                return;
            }

            _libraryService = BonjourManager.DiscoveredServers[serviceName];

            string hostname = _libraryService.IPAddresses[0].ToString();
            string pairingCode = string.Format("{0:0000}{0:0000}{0:0000}{0:0000}", pinTextBox.IntValue.Value);

            _server = new DACPServer(hostname, _libraryService.Port, pairingCode);
            _server.ServerUpdate += DACPServer_ServerUpdate;

            UpdateWizardItem(true);

            _log.Info("Connecting to server with ID '{0}' at {1}:{2}...", serviceName, _server.Hostname, _server.Port);

            _server.Start(false);
        }

        private void DACPServer_ServerUpdate(object sender, ServerUpdateEventArgs e)
        {
            Utility.BeginInvokeOnUIThread(() =>
            {
                if (sender != _server)
                    return;

                switch (e.Type)
                {
                    case ServerUpdateType.ServerConnected:
                        _log.Info("Successfully connected to server at {0}:{1}", _server.Hostname, _server.Port);
                        _server.ServerUpdate -= DACPServer_ServerUpdate;

                        // Save the server connection info
                        ServerConnectionInfo info = new ServerConnectionInfo();
                        info.Name = _server.LibraryName;
                        info.ServiceID = _libraryService.Name;
                        info.PairingCode = _server.PairingCode;
                        info.LastHostname = _libraryService.Hostname;
                        info.LastIPAddress = _server.Hostname;
                        info.LastPort = _server.Port;

                        ServerManager.AddServerInfo(info);
                        ServerManager.ChooseServer(info);

                        Hide();

                        NavigationManager.GoToFirstPage();
                        break;

                    case ServerUpdateType.Error:
                    default:
                        _log.Warning("Connection error from server at {0}:{1}: {2}", _server.Hostname, _server.Port, e.Type);

                        if (e.ErrorType == ServerErrorType.InvalidPIN)
                        {
                            MessageBox.Show(LocalizedStrings.LibraryPINErrorBody, LocalizedStrings.LibraryPINErrorTitle, MessageBoxButton.OK);
                            _server = null;
                            UpdateWizardItem(true);
                            return;
                        }

                        // Check whether there are any other IP addresses we could try
                        var ipStrings = _libraryService.IPAddresses.Select(ip => ip.ToString()).ToList();
                        var ipIndex = ipStrings.IndexOf(_server.Hostname);
                        if (ipIndex >= 0 && ipIndex < (ipStrings.Count - 1))
                        {
                            ipIndex++;
                            string nextIP = ipStrings[ipIndex];
                            _server.Hostname = nextIP;
                            _server.Port = _libraryService.Port;
                            _log.Info("Retrying connection on new IP: {0}:{1}", _server.Hostname, _server.Port);
                            _server.Start(false);
                            return;
                        }

                        // No other IPs, so we can't do anything else
                        _server.ServerUpdate -= DACPServer_ServerUpdate;
                        _server = null;
                        UpdateWizardItem(true);
                        break;
                }
            });
        }

        #endregion
    }
}
