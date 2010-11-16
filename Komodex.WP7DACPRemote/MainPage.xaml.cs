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
using Komodex.WP7DACPRemote.DACPServerInfoManagement;
using Microsoft.Phone.Shell;

namespace Komodex.WP7DACPRemote
{
    public partial class MainPage : PhoneApplicationPage
    {
        public MainPage()
        {
            InitializeComponent();

            // ApplicationBar button and menu item references must be referenced at run time
            btnPlayPause = (ApplicationBarIconButton)ApplicationBar.Buttons[1];
        }

        private ApplicationBarIconButton btnPlayPause = null;
        private readonly Uri iconPlay = new Uri("/icons/appbar.transport.play.rest.png", UriKind.Relative);
        private readonly Uri iconPause = new Uri("/icons/appbar.transport.pause.rest.png", UriKind.Relative);

        #region Properties

        private DACPServer _Server = null;
        private DACPServer Server
        {
            get { return _Server; }
            set
            {
                if (_Server != null)
                {
                    _Server.ServerUpdate -= new EventHandler<ServerUpdateEventArgs>(DACPServerUpdate);
                    _Server.PropertyChanged -= new System.ComponentModel.PropertyChangedEventHandler(Server_PropertyChanged);
                }

                _Server = value;
                DataContext = _Server;

                if (_Server != null)
                {
                    _Server.ServerUpdate += new EventHandler<ServerUpdateEventArgs>(DACPServerUpdate);
                    _Server.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(Server_PropertyChanged);
                }
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

        void Server_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "PlayState":
                    UpdatePlayPauseButton();
                    break;
                default:
                    break;
            }
        }

        private void UpdatePlayPauseButton()
        {
            if (Server.PlayState == PlayStates.Playing)
                btnPlayPause.IconUri = iconPause;
            else
                btnPlayPause.IconUri = iconPlay;
        }

        #endregion

        #region Actions

        private void mnuSettings_Click(object sender, EventArgs e)
        {
            GoToSettingsPage();
        }

        private void btnPrevious_Click(object sender, EventArgs e)
        {
            Server.SendPrevItemCommand();
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            Server.SendNextItemCommand();
        }

        private void btnPlayPause_Click(object sender, EventArgs e)
        {
            Server.SendPlayPauseCommand();
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