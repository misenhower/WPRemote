using Komodex.Common;
using Komodex.Common.Phone;
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
using System.Windows.Navigation;

namespace Komodex.Remote.Controls
{
    public partial class PairingDialog : DialogUserControlBase
    {
        public PairingDialog()
        {
            InitializeComponent();
        }

        protected override void Show(ContentPresenter container)
        {
            // Prepare the dialog
            NetworkManager.NetworkAvailabilityChanged += NetworkManager_NetworkAvailabilityChanged;
            UpdateWizardItem(false);
            StartPairingManager();

            base.Show(container);

            _dialogService.HideOnNavigate = false;
        }

        protected override void DialogService_Closed(object sender, EventArgs e)
        {
            base.DialogService_Closed(sender, e);

            NetworkManager.NetworkAvailabilityChanged -= NetworkManager_NetworkAvailabilityChanged;
            StopPairingManager();
        }

        private void NetworkManager_NetworkAvailabilityChanged(object sender, NetworkAvailabilityChangedEventArgs e)
        {
            Utility.BeginInvokeOnUIThread(() => UpdateWizardItem(true));
        }

        protected void UpdateWizardItem(bool useTransitions)
        {
            if (NetworkManager.IsLocalNetworkAvailable)
            {
                wizard.SetSelectedItem(wizardItemPasscode, useTransitions);
                leftButton.Content = LocalizedStrings.CancelDialogButton;
            }
            else
            {
                wizard.SetSelectedItem(wizardItemWiFi, useTransitions);
                leftButton.Content = LocalizedStrings.WiFiSettingsDialogButton;
            }
        }

        private void leftButton_Click(object sender, RoutedEventArgs e)
        {
            if (wizard.SelectedItem == wizardItemWiFi)
            {
                ConnectionSettingsTask task = new ConnectionSettingsTask();
                task.ConnectionSettingsType = ConnectionSettingsType.WiFi;
                task.Show();
            }
            else
            {
                Hide();
            }
        }

        #region Pairing Manager

        protected void StartPairingManager()
        {
            PairingManager.PairingComplete += PairingManager_PairingComplete;
            PairingManager.Start();

            string pin = PairingManager.PIN;
            pinTextBox1.Text = pin[0].ToString();
            pinTextBox2.Text = pin[1].ToString();
            pinTextBox3.Text = pin[2].ToString();
            pinTextBox4.Text = pin[3].ToString();
        }

        protected void StopPairingManager()
        {
            PairingManager.PairingComplete += PairingManager_PairingComplete;
            PairingManager.Stop();
        }

        private void PairingManager_PairingComplete(object sender, ServerManagement.ServerConnectionInfoEventArgs e)
        {
            Utility.BeginInvokeOnUIThread(() =>
            {
                ServerManager.ChooseServer(e.ConnectionInfo);
                Hide();
                NavigationManager.GoToFirstPage();
            });
        }

        #endregion
    }
}
