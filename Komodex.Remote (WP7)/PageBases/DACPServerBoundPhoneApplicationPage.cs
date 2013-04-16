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
using Komodex.Remote.DACPServerManagement;
using Microsoft.Phone.Shell;
using Clarity.Phone.Controls;
using Komodex.Remote.Localization;
using Komodex.Common.Phone;

namespace Komodex.Remote
{
    // TODO: This class needs a lot of cleanup.
    public class DACPServerBoundPhoneApplicationPage : RemoteBasePage
    {
        public DACPServerBoundPhoneApplicationPage()
        {
            ApplicationBarOpenOpacity = 0.9;
            ApplicationBarClosedOpacity = 0.5;
        }

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

        private ApplicationBarIconButton _appBarPreviousTrackButton;
        private ApplicationBarIconButton _appBarPlayPauseButton;
        private ApplicationBarIconButton _appBarNextTrackButton;
        private readonly Uri iconPlay = new Uri("/icons/appbar.transport.play.rest.png", UriKind.Relative);
        private readonly Uri iconPause = new Uri("/icons/appbar.transport.pause.rest.png", UriKind.Relative);

        private bool _usesStandardPlayTransportApplicationBar;

        protected void InitializeStandardPlayTransportApplicationBar()
        {
            // Previous track
            _appBarPreviousTrackButton = AddApplicationBarIconButton(LocalizedStrings.PreviousAppBarButton, "/icons/appbar.transport.rew.rest.png", () => DACPServer.SendPrevItemCommand());

            // Play/pause
            _appBarPlayPauseButton = AddApplicationBarIconButton(LocalizedStrings.PlayPauseAppBarButton, iconPlay, () => DACPServer.SendPlayPauseCommand());

            // Next track
            _appBarNextTrackButton = AddApplicationBarIconButton(LocalizedStrings.NextAppBarButton, "/icons/appbar.transport.ff.rest.png", () => DACPServer.SendNextItemCommand());

            // Set the flag
            _usesStandardPlayTransportApplicationBar = true;
        }

        private void UpdateTransportButtons()
        {
            if (!_usesStandardPlayTransportApplicationBar)
                return;

            bool isStopped = true;
            bool isPlaying = false;

            if (DACPServer != null)
            {
                isStopped = (DACPServer.PlayState == PlayStates.Stopped && DACPServer.CurrentSongName == null);
                isPlaying = (DACPServer.PlayState == PlayStates.Playing);
            }

            _appBarPreviousTrackButton.IsEnabled = _appBarPlayPauseButton.IsEnabled = _appBarNextTrackButton.IsEnabled = !isStopped;

            if (isPlaying)
                _appBarPlayPauseButton.IconUri = iconPause;
            else
                _appBarPlayPauseButton.IconUri = iconPlay;
        }

        #endregion

        #region Standard App Navigation Application Bar

        private ApplicationBarIconButton _appBarNowPlayingButton;

        private bool _usesStandardAppNavApplicationBar;

        protected void InitializeStandardAppNavApplicationBar(bool addNowPlayingButton = true, bool addBrowseButton = true, bool addSearchButton = true)
        {
            if (addNowPlayingButton)
                _appBarNowPlayingButton = AddApplicationBarIconButton(LocalizedStrings.NowPlayingAppBarButton, "/icons/custom.appbar.itunes.png", () => NavigationManager.OpenNowPlayingPage());

            if (addBrowseButton)
                AddApplicationBarIconButton(LocalizedStrings.BrowseLibraryAppBarButton, "/icons/custom.appbar.browse.png", () => NavigationManager.OpenMainLibraryPage());

            if (addSearchButton)
                AddApplicationBarIconButton(LocalizedStrings.SearchAppBarButton, "/icons/appbar.feature.search.rest.png", () => NavigationManager.OpenSearchPage());

            _usesStandardAppNavApplicationBar = true;
        }

        private void UpdateAppNavButtons()
        {
            if (!_usesStandardAppNavApplicationBar)
                return;

            if (_appBarNowPlayingButton != null)
                _appBarNowPlayingButton.IsEnabled = (DACPServer != null && DACPServer.IsConnected && !(DACPServer.PlayState == PlayStates.Stopped && DACPServer.CurrentSongName == null));
        }

        #endregion

        #region Standard Application Bar Menu Items

        protected void AddChooseLibraryApplicationBarMenuItem()
        {
            AddApplicationBarMenuItem(LocalizedStrings.ChooseLibraryMenuItem, () => NavigationManager.OpenLibraryChooserPage());
        }

        protected void AddAboutApplicationBarMenuItem()
        {
            AddApplicationBarMenuItem(LocalizedStrings.AboutMenuItem, () => NavigationManager.OpenAboutPage());
        }

        protected void AddSettingsApplicationBarMenuItem()
        {
            AddApplicationBarMenuItem(LocalizedStrings.SettingsMenuItem, () => NavigationManager.OpenSettingsPage());
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
            UpdateBusyState();
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
            switch (e.PropertyName)
            {
                case "PlayState":
                case "CurrentSongName":
                    UpdateTransportButtons();
                    UpdateAppNavButtons();
                    break;
                case "GettingData":
                    UpdateBusyState();
                    break;
                default:
                    break;
            }
        }

        #endregion

        #region Methods

        private void UpdateApplicationBarVisibility()
        {
            if (ApplicationBar == null)
                return;

            ApplicationBar.IsVisible = (DACPServer != null && DACPServer.IsConnected);
        }

        private void UpdateBusyState()
        {
            if (DACPServer != null && DACPServer.IsConnected && DACPServer.GettingData)
                SetProgressIndicator(null, true);
            else
                ClearProgressIndicator();
        }

        protected virtual void AttachServerEvents()
        {
            if (DACPServer != null)
            {
                DACPServer.ServerUpdate += new EventHandler<ServerUpdateEventArgs>(DACPServer_ServerUpdate);
                DACPServer.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(DACPServer_PropertyChanged);
            }
        }

        protected virtual void DetachServerEvents()
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
