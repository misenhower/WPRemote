using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using Komodex.Remote.Pairing;
using Komodex.Remote.ServerManagement;
using Komodex.Common;

namespace Komodex.Remote.Pages.Pairing
{
    public partial class PairingPage : RemoteBasePage
    {
        public PairingPage()
        {
            InitializeComponent();

            DisableConnectionStatusPopup = true;

            InitializeApplicationBar();
            ApplicationBar.Mode = ApplicationBarMode.Minimized;
            AddApplicationBarMenuItem("manual pairing", null);
            AddApplicationBarMenuItem("about", null);
        }

        #region Actions

        private void WiFiSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            ConnectionSettingsTask task = new ConnectionSettingsTask();
            task.ConnectionSettingsType = ConnectionSettingsType.WiFi;
            task.Show();
        }

        #endregion

#if WP7
#endif

#if WP8

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            NetworkManager.NetworkAvailabilityChanged += NetworkManager_NetworkAvailabilityChanged;

            StartPairingManager();

            if (NetworkManager.IsLocalNetworkAvailable)
                wizard.SetSelectedItem(wizardItemPasscode, false);
            else
                wizard.SetSelectedItem(wizardItemWiFi, false);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            NetworkManager.NetworkAvailabilityChanged -= NetworkManager_NetworkAvailabilityChanged;

            StopPairingManager();
        }

        private void NetworkManager_NetworkAvailabilityChanged(object sender, NetworkAvailabilityChangedEventArgs e)
        {
            Utility.BeginInvokeOnUIThread(() =>
            {
                if (e.IsLocalNetworkAvailable)
                {
                    if (wizard.SelectedItem == wizardItemWiFi)
                        wizard.SelectedItem = wizardItemPasscode;
                }
                else
                {
                    wizard.SelectedItem = wizardItemWiFi;
                }
            });
        }

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
                NavigationManager.GoToFirstPage();
            });
        }
#endif

    }
}