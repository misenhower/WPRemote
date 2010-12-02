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
using Microsoft.Phone.Shell;
using Komodex.WP7DACPRemote.DACPServerManagement;
using Komodex.DACP.Library;

namespace Komodex.WP7DACPRemote
{
    public partial class MainPage : DACPServerBoundPhoneApplicationPage
    {
        public MainPage()
        {
            InitializeComponent();

            InitializeStandardTransportApplicationBar();

            AnimationContext = LayoutRoot;
        }

        #region Overrides

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            UpdateVisualState(false);
        }

        protected override void DACPServer_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            base.DACPServer_PropertyChanged(sender, e);
            UpdateVisualState();
        }

        protected override void DACPServer_ServerUpdate(object sender, ServerUpdateEventArgs e)
        {
            base.DACPServer_ServerUpdate(sender, e);
            Deployment.Current.Dispatcher.BeginInvoke(() => UpdateVisualState());
        }

        protected override void DACPServerManager_ServerChanged(object sender, EventArgs e)
        {
            base.DACPServerManager_ServerChanged(sender, e);
            Deployment.Current.Dispatcher.BeginInvoke(() => UpdateVisualState());
        }

        #endregion

        #region Methods

        private void UpdateVisualState(bool useTransitions = true)
        {
            if (DACPServer == null || (DACPServer.PlayState == DACP.PlayStates.Stopped && DACPServer.CurrentSongName == null))
            {
                VisualStateManager.GoToState(this, "StoppedState", useTransitions);
                btnNowPlaying.IsEnabled = false;
            }
            else
            {
                VisualStateManager.GoToState(this, "PlayingState", useTransitions);
                btnNowPlaying.IsEnabled = true;
            }
        }

        #endregion

        #region Actions

        private void btnNowPlaying_Click(object sender, RoutedEventArgs e)
        {
            NavigationManager.OpenNowPlayingPage();
        }

        private void btnLibrary_Click(object sender, RoutedEventArgs e)
        {
            NavigationManager.OpenMainLibraryPage();
        }

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            NavigationManager.OpenSearchPage();
        }

        private void mnuSettings_Click(object sender, EventArgs e)
        {
            NavigationManager.OpenLibraryChooserPage();
        }

        #endregion

    }
}