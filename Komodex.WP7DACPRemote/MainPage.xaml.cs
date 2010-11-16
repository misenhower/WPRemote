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
using Komodex.WP7DACPRemote.DACPServerInfoManagement;

namespace Komodex.WP7DACPRemote
{
    public partial class MainPage : PhoneApplicationPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        #region Properties

        private DACPServer _Server = null;
        private DACPServer Server
        {
            get { return _Server; }
            set
            {
                if (_Server != null)
                    _Server.ServerUpdate -= new EventHandler<ServerUpdateEventArgs>(DACPServerUpdate);

                _Server = value;
                DataContext = _Server;

                if (_Server != null)
                    _Server.ServerUpdate += new EventHandler<ServerUpdateEventArgs>(DACPServerUpdate);
            }
        }

        #endregion

        #region Overrides

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            SetVisibility(false);

            DACPServerViewModel viewModel = DACPServerViewModel.Instance;

            DACPServerInfo serverInfo = viewModel.Items.FirstOrDefault(si => si.ID == viewModel.SelectedServerGuid);

            if (serverInfo == null)
            {
                GoToSettingsPage();
            }
            else
            {
                Server = new DACPServer(serverInfo.HostName, serverInfo.PairingCode);
                Server.LibraryName = serverInfo.LibraryName;
                Server.Start();
            }
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            if (Server != null)
            {
                Server.Stop();
                Server = null;
            }
        }

        #endregion

        #region Event Handlers

        void DACPServerUpdate(object sender, ServerUpdateEventArgs e)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                switch (e.Type)
                {
                    case ServerUpdateType.ServerInfoResponse:
                        break;
                    case ServerUpdateType.ServerConnected:
                        SetVisibility(true);
                        break;
                    case ServerUpdateType.Error:
                        GoToSettingsPage();
                        break;
                    default:
                        break;
                }
            });
        }

        #endregion

        #region Actions

        private void mnuSettings_Click(object sender, EventArgs e)
        {
            GoToSettingsPage();
        }

        #endregion

        #region Methods

        private void SetVisibility(bool serverConnected)
        {
            if (serverConnected)
            {
                pivotControl.Visibility = System.Windows.Visibility.Visible;
                ApplicationBar.IsVisible = true;
                connectingStatusControl.ShowProgress = false;
            }
            else
            {
                pivotControl.Visibility = System.Windows.Visibility.Collapsed;
                ApplicationBar.IsVisible = false;
                connectingStatusControl.ShowProgress = true;
            }
        }

        private void GoToSettingsPage()
        {
            NavigationService.Navigate(new Uri("/DACPServerInfoManagement/LibraryChooserPage.xaml", UriKind.Relative));
        }

        #endregion
    }
}