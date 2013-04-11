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
using Komodex.Common.Phone;
using Komodex.Common;
using Komodex.Common.Phone.Controls;
using Komodex.Remote.ServerManagement;
using Komodex.DACP;
using Komodex.Bonjour;

namespace Komodex.Remote.Pages.Pairing
{
    public partial class ManualPairingPage : PhoneApplicationBasePage
    {
        public ManualPairingPage()
        {
            InitializeComponent();

            libraryList.ItemsSource = ManualPairingManager.DiscoveredPairingUtilities;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            ManualPairingManager.SearchForPairingUtility();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            ManualPairingManager.StopSearchingForPairingUtility();
        }

        CustomMessageBox _messageBox;
        NumericTextBox _pinTextBox;
        DiscoveredPairingUtility _selectedUtilityInfo;

        private void libraryList_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            _selectedUtilityInfo = libraryList.SelectedItem as DiscoveredPairingUtility;
            if (_selectedUtilityInfo == null)
                return;

            _messageBox = new CustomMessageBox();
            _messageBox.Dismissing += _messageBox_Dismissing;
            _messageBox.LeftButtonContent = "ok";
            _messageBox.Caption = "Add new library";
            _messageBox.Message = "Please enter the PIN for the library:";

            _pinTextBox = new NumericTextBox();
            _pinTextBox.MaxLength = 4;

            _messageBox.Content = _pinTextBox;
            _messageBox.Show();
        }

        void _messageBox_Dismissing(object sender, DismissingEventArgs e)
        {
            if (e.Result != CustomMessageBoxResult.LeftButton)
                return;

            e.Cancel = true;

            if (BonjourManager.DiscoveredServers.ContainsKey(_selectedUtilityInfo.Service.Name))
            {
                _messageBox.IsLeftButtonEnabled = false; // TODO: This doesn't seem to have an effect once the message box is displayed
                SetProgressIndicator("Connecting to Library...", true);

                NetService service = BonjourManager.DiscoveredServers[_selectedUtilityInfo.Service.Name];
                string host = string.Format("{0}:{1}", service.IPAddresses[0], service.Port);
                string pin = string.Format("{0:0000}{0:0000}{0:0000}{0:0000}", _pinTextBox.IntValue ?? 0);
                DACPServer server = new DACPServer(host, pin);
                server.ServerUpdate += DACPServer_ServerUpdate;
                server.Start(false);
            }
            else
            {
                // TODO: Something better...
                // Should services even appear if we don't have them in BonjourManager?
                MessageBox.Show("Error: could not find service.");
            }
        }

        private void DACPServer_ServerUpdate(object sender, ServerUpdateEventArgs e)
        {
            Utility.BeginInvokeOnUIThread(() =>
            {
                switch (e.Type)
                {
                    case ServerUpdateType.ServerConnected:
                        ClearProgressIndicator("Connected!");
                        break;
                    case ServerUpdateType.Error:
                    default:
                        ClearProgressIndicator("Error");
                        break;
                }
            });
        }

    }
}