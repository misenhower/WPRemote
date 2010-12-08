using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Komodex.DACP;
using Komodex.WP7DACPRemote.DACPServerManagement;
using Microsoft.Phone.Shell;
using Clarity.Phone.Controls;

namespace Komodex.WP7DACPRemote
{
    public class DACPServerBoundPhoneApplicationPage : AnimatedBasePage
    {
        #region Properties

        protected DACPServer DACPServer
        {
            get { return DataContext as DACPServer; }
            set
            {
                DetachServerEvents();

                DataContext = value;

                AttachServerEvents();
            }
        }

        #endregion

        #region Standard Play Transport Application Bar

        private ApplicationBarIconButton AppBarPreviousTrackButton = null;
        private ApplicationBarIconButton AppBarPlayPauseButton = null;
        private ApplicationBarIconButton AppBarNextTrackButton = null;
        private readonly Uri iconPlay = new Uri("/icons/appbar.transport.play.rest.png", UriKind.Relative);
        private readonly Uri iconPause = new Uri("/icons/appbar.transport.pause.rest.png", UriKind.Relative);

        protected bool UsesStandardPlayTransportApplicationBar { get; private set; }

        protected void InitializeStandardPlayTransportApplicationBar()
        {
            // Previous track
            AppBarPreviousTrackButton = new ApplicationBarIconButton(new Uri("/icons/appbar.transport.rew.rest.png", UriKind.Relative));
            AppBarPreviousTrackButton.Text = "previous";
            AppBarPreviousTrackButton.Click += new EventHandler(AppBarPreviousTrackButton_Click);
            ApplicationBar.Buttons.Add(AppBarPreviousTrackButton);

            // Play/pause
            AppBarPlayPauseButton = new ApplicationBarIconButton(iconPlay);
            AppBarPlayPauseButton.Text = "play/pause";
            AppBarPlayPauseButton.Click += new EventHandler(AppBarPlayPauseButton_Click);
            ApplicationBar.Buttons.Add(AppBarPlayPauseButton);

            // Next track
            AppBarNextTrackButton = new ApplicationBarIconButton(new Uri("/icons/appbar.transport.ff.rest.png", UriKind.Relative));
            AppBarNextTrackButton.Text = "next";
            AppBarNextTrackButton.Click += new EventHandler(AppBarNextTrackButton_Click);
            ApplicationBar.Buttons.Add(AppBarNextTrackButton);

            // Set the flag
            UsesStandardPlayTransportApplicationBar = true;
        }

        private void UpdateTransportButtons()
        {
            if (!UsesStandardPlayTransportApplicationBar)
                return;

            bool isStopped = true;
            bool isPlaying = false;

            if (DACPServer != null)
            {
                isStopped = (DACPServer.PlayState == PlayStates.Stopped && DACPServer.CurrentSongName == null);
                isPlaying = (DACPServer.PlayState == PlayStates.Playing);
            }

            AppBarPreviousTrackButton.IsEnabled = AppBarPlayPauseButton.IsEnabled = AppBarNextTrackButton.IsEnabled = !isStopped;

            if (isPlaying)
                AppBarPlayPauseButton.IconUri = iconPause;
            else
                AppBarPlayPauseButton.IconUri = iconPlay;
        }

        private void AppBarPreviousTrackButton_Click(object sender, EventArgs e)
        {
            DACPServer.SendPrevItemCommand();
        }

        private void AppBarNextTrackButton_Click(object sender, EventArgs e)
        {
            DACPServer.SendNextItemCommand();
        }

        private void AppBarPlayPauseButton_Click(object sender, EventArgs e)
        {
            DACPServer.SendPlayPauseCommand();
        }

        #endregion

        #region Standard App Navigation Application Bar

        private ApplicationBarIconButton AppBarNowPlayingButton = null;
        private ApplicationBarIconButton AppBarBrowseButton = null;
        private ApplicationBarIconButton AppBarSearchButton = null;

        protected bool UsesStandardAppNavApplicationBar { get; private set; }

        protected void InitializeStandardAppNavApplicationBar(bool addNowPlayingButton = true, bool addBrowseButton = true, bool addSearchButton = true)
        {
            if (addNowPlayingButton)
            {
                AppBarNowPlayingButton = new ApplicationBarIconButton(new Uri("/icons/custom.appbar.itunes.png", UriKind.Relative));
                AppBarNowPlayingButton.Text = "now playing";
                AppBarNowPlayingButton.Click += new EventHandler(AppBarNowPlayingButton_Click);
                ApplicationBar.Buttons.Add(AppBarNowPlayingButton);
            }

            if (addBrowseButton)
            {
                AppBarBrowseButton = new ApplicationBarIconButton(new Uri("/icons/custom.appbar.browse.png", UriKind.Relative));
                AppBarBrowseButton.Text = "browse";
                AppBarBrowseButton.Click += new EventHandler(AppBarBrowseButton_Click);
                ApplicationBar.Buttons.Add(AppBarBrowseButton);
            }

            if (addSearchButton)
            {
                AppBarSearchButton = new ApplicationBarIconButton(new Uri("/icons/appbar.feature.search.rest.png", UriKind.Relative));
                AppBarSearchButton.Text = "search";
                AppBarSearchButton.Click += new EventHandler(AppBarSearchButton_Click);
                ApplicationBar.Buttons.Add(AppBarSearchButton);
            }

            UsesStandardAppNavApplicationBar = true;
        }

        private void UpdateAppNavButtons()
        {
            if (!UsesStandardAppNavApplicationBar)
                return;

            if (AppBarNowPlayingButton != null)
                AppBarNowPlayingButton.IsEnabled = (DACPServer != null && DACPServer.IsConnected && !(DACPServer.PlayState == PlayStates.Stopped && DACPServer.CurrentSongName == null));
        }

        void AppBarNowPlayingButton_Click(object sender, EventArgs e)
        {
            NavigationManager.OpenNowPlayingPage();
        }

        void AppBarBrowseButton_Click(object sender, EventArgs e)
        {
            NavigationManager.OpenMainLibraryPage();
        }

        void AppBarSearchButton_Click(object sender, EventArgs e)
        {
            NavigationManager.OpenSearchPage();
        }

        #endregion

        #region Standard Application Bar Menu Items

        protected void AddChooseLibraryApplicationBarMenuItem()
        {
            ApplicationBarMenuItem menuItem = new ApplicationBarMenuItem("choose library");
            menuItem.Click += new EventHandler(ChooseLibraryMenuItem_Click);
            ApplicationBar.MenuItems.Add(menuItem);
        }

        void ChooseLibraryMenuItem_Click(object sender, EventArgs e)
        {
            NavigationManager.OpenLibraryChooserPage();
        }

        protected void AddAboutApplicationBarMenuItem()
        {
            ApplicationBarMenuItem menuItem = new ApplicationBarMenuItem("about");
            menuItem.Click += new EventHandler(AboutMenuItem_Click);
            ApplicationBar.MenuItems.Add(menuItem);
        }

        void AboutMenuItem_Click(object sender, EventArgs e)
        {
            NavigationManager.OpenAboutPage();
        }

        #endregion

        #region Overrides

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            DACPServerManager.ServerChanged += new EventHandler(DACPServerManager_ServerChanged);

            if (DACPServer != DACPServerManager.Server)
                DACPServer = DACPServerManager.Server;
            else
                AttachServerEvents();

            UpdateTransportButtons();
            UpdateAppNavButtons();
            UpdateApplicationBarVisibility();
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            DACPServerManager.ServerChanged -= new EventHandler(DACPServerManager_ServerChanged);
            DetachServerEvents();
        }

        #endregion

        #region Event Handlers

        protected virtual void DACPServerManager_ServerChanged(object sender, EventArgs e)
        {
            DACPServer = DACPServerManager.Server;
            UpdateApplicationBarVisibility();
        }

        protected virtual void DACPServer_ServerUpdate(object sender, ServerUpdateEventArgs e)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                UpdateApplicationBarVisibility();
            });
        }

        protected virtual void DACPServer_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "PlayState")
            {
                UpdateTransportButtons();
                UpdateAppNavButtons();
            }
        }

        #endregion

        #region Methods

        private void UpdateApplicationBarVisibility()
        {
            if (ApplicationBar == null)
                return;

            ApplicationBar.IsVisible = (!NavigationManager.NavigatingToFirstPage && DACPServer != null && DACPServer.IsConnected);
        }

        private void AttachServerEvents()
        {
            if (DACPServer != null)
            {
                DACPServer.ServerUpdate += new EventHandler<ServerUpdateEventArgs>(DACPServer_ServerUpdate);
                DACPServer.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(DACPServer_PropertyChanged);
            }
        }

        private void DetachServerEvents()
        {
            if (DACPServer != null)
            {
                DACPServer.ServerUpdate -= new EventHandler<ServerUpdateEventArgs>(DACPServer_ServerUpdate);
                DACPServer.PropertyChanged -= new System.ComponentModel.PropertyChangedEventHandler(DACPServer_PropertyChanged);
            }
        }

        #endregion
    }
}
