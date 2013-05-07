using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
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
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            PairingManager.PairingComplete += PairingManager_PairingComplete;
            PairingManager.Start();
            pinTextBox.Text = PairingManager.PIN;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            PairingManager.PairingComplete -= PairingManager_PairingComplete;
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
    }
}