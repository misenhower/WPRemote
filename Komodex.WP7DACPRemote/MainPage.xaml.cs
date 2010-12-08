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

            InitializeStandardPlayTransportApplicationBar();

            AnimationContext = LayoutRoot;

            AddChooseLibraryApplicationBarMenuItem();
            AddAboutApplicationBarMenuItem();
        }

        #region Overrides

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            UpdateBindings();
            UpdateVisualState(false);
        }

        protected override void DACPServer_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            base.DACPServer_PropertyChanged(sender, e);
            UpdateBindings();
            UpdateVisualState();
        }

        protected override void DACPServer_ServerUpdate(object sender, ServerUpdateEventArgs e)
        {
            base.DACPServer_ServerUpdate(sender, e);
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                UpdateBindings();
                UpdateVisualState();
            });
        }

        protected override void DACPServerManager_ServerChanged(object sender, EventArgs e)
        {
            base.DACPServerManager_ServerChanged(sender, e);
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                UpdateBindings();
                UpdateVisualState();
            });
        }

        #endregion

        #region Methods

        private void UpdateBindings()
        {
            // Page title
            if (DACPServer == null || string.IsNullOrEmpty(DACPServer.LibraryName))
                ApplicationTitle.Text = "REMOTE";
            else
                ApplicationTitle.Text = DACPServer.LibraryName;

            // Panel visibility
            ContentPanel.Visibility = (DACPServer != null && DACPServer.IsConnected) ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            FirstStartPanel.Visibility = (DACPServer == null && DACPServerViewModel.Instance.Items.Count == 0) ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        }

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

        private void btnAddLibrary_Click(object sender, RoutedEventArgs e)
        {
            NavigationManager.OpenAddNewServerPage();
        }

        #endregion

    }
}