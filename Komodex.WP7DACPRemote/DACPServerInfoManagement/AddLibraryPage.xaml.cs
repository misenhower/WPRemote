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

namespace Komodex.WP7DACPRemote.DACPServerInfoManagement
{
    public partial class AddLibraryPage : PhoneApplicationPage
    {
        DACPServerInfo serverInfo = null;
        DACPServer server = null;

        public AddLibraryPage()
        {
            InitializeComponent();

            serverInfo = new DACPServerInfo();
            DataContext = serverInfo;
        }

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
            UpdateBoundData();
            SetVisibility(true);
            server = new DACPServer(serverInfo.HostName, serverInfo.PairingCode);
            server.ServerUpdate += new EventHandler<ServerUpdateEventArgs>(server_ServerUpdate);
            server.GetServerInfo();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            NavigationService.GoBack();
        }

        #endregion

        #region Control Event Handlers

        private void tbPIN_KeyDown(object sender, KeyEventArgs e)
        {
            // Only allow numeric characters
            bool validCharacter = (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9) || (e.Key >= Key.D0 && e.Key <= Key.D9);

            if (!validCharacter)
                e.Handled = true;
        }

        #endregion

        #region Server Validation

        void SetVisibility(bool isConnecting)
        {
            connectingStatusControl.ShowProgress = isConnecting;
            ApplicationBar.IsVisible = !isConnecting;
            ContentPanel.Visibility = (isConnecting) ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
        }

        void server_ServerUpdate(object sender, ServerUpdateEventArgs e)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                switch (e.Type)
                {
                    case ServerUpdateType.ServerInfoResponse:
                        // Get the library name
                        serverInfo.LibraryName = server.LibraryName;
                        // Validate the PIN
                        server.Start(false);
                        break;
                    case ServerUpdateType.ServerConnected:
                        // PIN was correct
                        break;
                    case ServerUpdateType.Error:
                        MessageBox.Show("Could not connect to library. Please check your settings and try again.");
                        SetVisibility(false);
                        break;
                    default:
                        break;
                }
            });
        }

        #endregion
    }
}