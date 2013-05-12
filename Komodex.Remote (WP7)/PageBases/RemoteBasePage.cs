using Komodex.Common;
using Komodex.Common.Phone;
using Komodex.DACP;
using Komodex.Remote.Localization;
using Komodex.Remote.ServerManagement;
using Microsoft.Phone.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace Komodex.Remote
{
    public class RemoteBasePage : PhoneApplicationBasePage
    {
        public RemoteBasePage()
        {
            ApplicationBarMenuOpenOpacity = 0.9;
            ApplicationBarMenuClosedOpacity = 0.5;
        }

        #region Connection Status Popup

        private bool _disableConnectionStatusPopup;
        public bool DisableConnectionStatusPopup
        {
            get { return _disableConnectionStatusPopup; }
            protected set
            {
                if (_disableConnectionStatusPopup == value)
                    return;

                _disableConnectionStatusPopup = value;
                ConnectionStatusPopupManager.UpdatePopupVisibility();
            }
        }

        #endregion

        #region Overrides

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            ServerManager.CurrentServerChanged += ServerManager_CurrentServerChanged;
            CurrentServer = ServerManager.CurrentServer;
            UpdateBusyState();
            UpdateAppBar();
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            ServerManager.CurrentServerChanged -= ServerManager_CurrentServerChanged;
            DetachServerEvents(CurrentServer);
        }

        #endregion

        #region CurrentServer

        public static DependencyProperty CurrentServerProperty =
            DependencyProperty.Register("CurrentServer", typeof(DACPServer), typeof(PhoneApplicationBasePage), new PropertyMetadata(CurrentServerPropertyChanged));

        private static void CurrentServerPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RemoteBasePage page = (RemoteBasePage)d;

            page.DetachServerEvents(e.OldValue as DACPServer);
            page.AttachServerEvents(e.NewValue as DACPServer);
            page.OnServerChanged();
        }

        public DACPServer CurrentServer
        {
            get { return (DACPServer)GetValue(CurrentServerProperty); }
            set { SetValue(CurrentServerProperty, value); }
        }

        private void ServerManager_CurrentServerChanged(object sender, EventArgs e)
        {
            CurrentServer = ServerManager.CurrentServer;
        }

        #endregion

        #region Server Events

        private void AttachServerEvents(DACPServer server)
        {
            if (server == null)
                return;

            server.ServerUpdate += CurrentServer_ServerUpdate;
            server.PropertyChanged += CurrentServer_PropertyChanged;
            server.AirPlaySpeakerUpdate += CurrentServer_AirPlaySpeakerUpdate;
        }

        

        private void DetachServerEvents(DACPServer server)
        {
            if (server == null)
                return;

            server.ServerUpdate -= CurrentServer_ServerUpdate;
            server.PropertyChanged -= CurrentServer_PropertyChanged;
            server.AirPlaySpeakerUpdate -= CurrentServer_AirPlaySpeakerUpdate;
        }

        protected virtual void OnServerChanged()
        {
        }

        protected virtual void CurrentServer_ServerUpdate(object sender, ServerUpdateEventArgs e)
        {
        }

        protected virtual void CurrentServer_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "PlayState":
                case "CurrentSongName":
                    UpdateAppBar();
                    break;

                case "GettingData":
                    UpdateBusyState();
                    break;
            }
        }

        protected virtual void CurrentServer_AirPlaySpeakerUpdate(object sender, EventArgs e)
        {
        }

        private void UpdateBusyState()
        {
            Utility.BeginInvokeOnUIThread(() =>
            {
                if (CurrentServer != null && CurrentServer.IsConnected && CurrentServer.GettingData)
                    SetProgressIndicator(null, true);
                else
                    ClearProgressIndicator();
            });
        }

        #endregion

        #region Application Bar

        private void UpdateAppBar()
        {
            UpdateAppBarPlayTransportButtons();
            UpdateAppBarNowPlayingButtons();
        }

        private bool _hideApplicationBar;
        public bool HideApplicationBar
        {
            get { return _hideApplicationBar; }
            protected set
            {
                if (_hideApplicationBar == value)
                    return;

                _hideApplicationBar = value;
                // ConnectionStatusPopupManager will update the application bar's visibility
                ConnectionStatusPopupManager.UpdatePopupVisibility();
                // TODO: There may be a better way to handle this
            }
        }

        #region Play Transport Icons

        private bool _playTransportButtonsAdded;
        private ApplicationBarIconButton _playTransportPreviousTrackButton;
        private ApplicationBarIconButton _playTransportPlayPauseButton;
        private ApplicationBarIconButton _playTransportNextTrackButton;
        private static readonly Uri _playTransportPreviousTrackIcon = new Uri("/icons/appbar.transport.rew.rest.png", UriKind.Relative);
        private static readonly Uri _playTransportPlayIcon = new Uri("/icons/appbar.transport.play.rest.png", UriKind.Relative);
        private static readonly Uri _playTransportPauseIcon = new Uri("/icons/appbar.transport.pause.rest.png", UriKind.Relative);
        private static readonly Uri _playTransportNextTrackIcon = new Uri("/icons/appbar.transport.ff.rest.png", UriKind.Relative);

        protected void AddAppBarPlayTransportButtons()
        {
            if (_playTransportButtonsAdded)
                return;

            _playTransportPreviousTrackButton = AddApplicationBarIconButton(LocalizedStrings.PreviousAppBarButton, _playTransportPreviousTrackIcon, () => CurrentServer.SendPrevItemCommand());
            _playTransportPlayPauseButton = AddApplicationBarIconButton(LocalizedStrings.PlayPauseAppBarButton, _playTransportPlayIcon, () => CurrentServer.SendPlayPauseCommand());
            _playTransportNextTrackButton = AddApplicationBarIconButton(LocalizedStrings.NextAppBarButton, _playTransportNextTrackIcon, () => CurrentServer.SendNextItemCommand());
            _playTransportButtonsAdded = true;
        }

        protected void RemoveAppBarPlayTransportButtons()
        {
            if (!_playTransportButtonsAdded)
                return;

            _playTransportButtonsAdded = false;

            ApplicationBar.Buttons.Remove(_playTransportPreviousTrackButton);
            ApplicationBar.Buttons.Remove(_playTransportPlayPauseButton);
            ApplicationBar.Buttons.Remove(_playTransportNextTrackButton);

            _playTransportPreviousTrackButton = null;
            _playTransportPlayPauseButton = null;
            _playTransportNextTrackButton = null;
        }

        private void UpdateAppBarPlayTransportButtons()
        {
            if (!_playTransportButtonsAdded)
                return;

            bool isStopped = true;
            bool isPlaying = false;

            if (CurrentServer != null)
            {
                isStopped = (CurrentServer.PlayState == PlayStates.Stopped && CurrentServer.CurrentSongName == null);
                isPlaying = (CurrentServer.PlayState == PlayStates.Playing);
            }

            _playTransportPreviousTrackButton.IsEnabled = !isStopped;
            _playTransportPlayPauseButton.IsEnabled = !isStopped;
            _playTransportNextTrackButton.IsEnabled = !isStopped;

            if (isPlaying)
                _playTransportPlayPauseButton.IconUri = _playTransportPauseIcon;
            else
                _playTransportPlayPauseButton.IconUri = _playTransportPlayIcon;
        }

        #endregion

        #region "Now Playing" Items

        private ApplicationBarIconButton _nowPlayingButton;
        private ApplicationBarMenuItem _nowPlayingMenuItem;

        protected void AddAppBarNowPlayingButton()
        {
            _nowPlayingButton = AddApplicationBarIconButton(LocalizedStrings.NowPlayingAppBarButton, "/icons/custom.appbar.itunes.png", NavigationManager.OpenNowPlayingPage);
        }

        protected void AddAppBarNowPlayingMenuItem()
        {
            _nowPlayingMenuItem = AddApplicationBarMenuItem(LocalizedStrings.NowPlayingAppBarButton, NavigationManager.OpenNowPlayingPage);
        }

        private void UpdateAppBarNowPlayingButtons()
        {
            Utility.BeginInvokeOnUIThread(() =>
            {
                bool isEnabled = (CurrentServer != null && CurrentServer.IsConnected && !(CurrentServer.PlayState == PlayStates.Stopped && CurrentServer.CurrentSongName == null));

                if (_nowPlayingButton != null)
                    _nowPlayingButton.IsEnabled = isEnabled;

                if (_nowPlayingMenuItem != null)
                    _nowPlayingMenuItem.IsEnabled = isEnabled;
            });
        }

        #endregion

        #endregion
    }
}
