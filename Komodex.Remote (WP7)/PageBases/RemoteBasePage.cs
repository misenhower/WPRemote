﻿using Komodex.Common;
using Komodex.Common.Phone;
using Komodex.DACP;
using Komodex.Remote.Controls;
using Komodex.Remote.Localization;
using Komodex.Remote.ServerManagement;
using Microsoft.Phone.Controls;
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
            ServerManager.ConnectionStateChanged += ServerManager_ConnectionStateChanged;
            CurrentServer = ServerManager.CurrentServer;
            AttachServerEvents(CurrentServer);
            UpdateBusyState();
            UpdateAppBar();
        }


        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            ServerManager.CurrentServerChanged -= ServerManager_CurrentServerChanged;
            ServerManager.ConnectionStateChanged -= ServerManager_ConnectionStateChanged;
            DetachServerEvents(CurrentServer);
        }

        #endregion

        #region CurrentServer

        public static DependencyProperty CurrentServerProperty =
            DependencyProperty.Register("CurrentServer", typeof(DACPServer), typeof(PhoneApplicationBasePage), new PropertyMetadata(CurrentServerPropertyChanged));

        private static void CurrentServerPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RemoteBasePage page = (RemoteBasePage)d;

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

        private DACPServer _attachedServer;

        private void AttachServerEvents(DACPServer server)
        {
            // Make sure we only attach once and to one server at a time
            if (server == _attachedServer)
                return;
            DetachServerEvents(_attachedServer);

            if (server == null)
                return;

            server.PropertyChanged += CurrentServer_PropertyChanged;
            server.AirPlaySpeakerUpdate += CurrentServer_AirPlaySpeakerUpdate;
            server.LibraryUpdate += CurrentServer_LibraryUpdate;

            _attachedServer = server;
        }

        private void DetachServerEvents(DACPServer server)
        {
            if (server == null)
                return;

            server.PropertyChanged -= CurrentServer_PropertyChanged;
            server.AirPlaySpeakerUpdate -= CurrentServer_AirPlaySpeakerUpdate;
            server.LibraryUpdate -= CurrentServer_LibraryUpdate;

            if (server == _attachedServer)
                _attachedServer = null;
        }

        protected virtual void OnServerChanged()
        {
        }

        protected virtual void ServerManager_ConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
#if WP8
            ThreadUtility.RunOnUIThread(UpdateAppleTVControlButton);
#endif
        }

        protected virtual void CurrentServer_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "PlayState":
                case "CurrentSongName":
                case "IsCurrentlyPlayingiTunesRadio":
                case "IsiTunesRadioNextButtonEnabled":
                case "IsiTunesRadioMenuEnabled":
                case "IsiTunesRadioSongFavorited":
                case "IsCurrentlyPlayingGeniusShuffle":
                case "IsAppleTV":
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

        protected virtual void CurrentServer_LibraryUpdate(object sender, EventArgs e)
        {
        }

        protected virtual void UpdateBusyState()
        {
            ThreadUtility.RunOnUIThread(() =>
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
#if WP8
            UpdateAppleTVControlButton();
#endif
        }

        public virtual void UpdateApplicationBarVisibility()
        {
            if (ApplicationBar == null)
                return;

            ApplicationBar.IsVisible = !ConnectionStatusPopupManager.IsVisible;
        }

        #region Play Transport Icons

        private bool _playTransportButtonsAdded;
        private ApplicationBarIconButton _playTransportPreviousTrackButton;
        private ApplicationBarIconButton _playTransportPlayPauseButton;
        private ApplicationBarIconButton _playTransportNextTrackButton;
        private static readonly Uri _playTransportPreviousTrackIcon = ResolutionUtility.GetUriWithResolutionSuffix("/Assets/Icons/Transport.Rew.png");
        private static readonly Uri _playTransportPlayIcon = ResolutionUtility.GetUriWithResolutionSuffix("/Assets/Icons/Transport.Play.png");
        private static readonly Uri _playTransportPauseIcon = ResolutionUtility.GetUriWithResolutionSuffix("/Assets/Icons/Transport.Pause.png");
        private static readonly Uri _playTransportNextTrackIcon = ResolutionUtility.GetUriWithResolutionSuffix("/Assets/Icons/Transport.FF.png");
        private static readonly Uri _playTransportiTunesRadioIcon = ResolutionUtility.GetUriWithResolutionSuffix("/Assets/Icons/Transport.iTunesRadio.png");
        private static readonly Uri _playTransportGeniusShuffleIcon = ResolutionUtility.GetUriWithResolutionSuffix("/Assets/Icons/Transport.GeniusShuffle.png");

        protected void AddAppBarPlayTransportButtons()
        {
            if (_playTransportButtonsAdded)
                return;

            _playTransportPreviousTrackButton = AddApplicationBarIconButton(LocalizedStrings.PreviousAppBarButton, _playTransportPreviousTrackIcon, PlayTransportPreviousTrackButton_Click);
            _playTransportPlayPauseButton = AddApplicationBarIconButton(LocalizedStrings.PlayPauseAppBarButton, _playTransportPlayIcon, () => CurrentServer.SendPlayPauseCommand());
            _playTransportNextTrackButton = AddApplicationBarIconButton(LocalizedStrings.NextAppBarButton, _playTransportNextTrackIcon, () => CurrentServer.SendNextItemCommand());
            _playTransportButtonsAdded = true;
        }

        private async void PlayTransportPreviousTrackButton_Click()
        {
            if (CurrentServer != null && CurrentServer.IsCurrentlyPlayingiTunesRadio)
            {
                var menu = this.FindName("iTunesRadioContextMenu") as ContextMenu;
                if (menu != null)
                    menu.IsOpen = true;
            }
            else if (CurrentServer != null && CurrentServer.IsCurrentlyPlayingGeniusShuffle)
                await CurrentServer.SendGeniusShuffleCommandAsync();
            else
                CurrentServer.SendPrevItemCommand();
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
                isPlaying = (CurrentServer.PlayState == PlayStates.Playing || CurrentServer.PlayState == PlayStates.FastForward || CurrentServer.PlayState == PlayStates.Rewind);
            }

            _playTransportPreviousTrackButton.IsEnabled = !isStopped;
            _playTransportPlayPauseButton.IsEnabled = !isStopped;
            _playTransportNextTrackButton.IsEnabled = !isStopped;

            if (isPlaying)
                _playTransportPlayPauseButton.IconUri = _playTransportPauseIcon;
            else
                _playTransportPlayPauseButton.IconUri = _playTransportPlayIcon;

            // iTunes Radio/Genius Shuffle
            if (CurrentServer != null && CurrentServer.IsCurrentlyPlayingiTunesRadio)
            {
                _playTransportPreviousTrackButton.Text = LocalizedStrings.iTunesRadioAppBarButton;
                _playTransportPreviousTrackButton.IconUri = _playTransportiTunesRadioIcon;
                _playTransportNextTrackButton.IsEnabled = !isStopped && CurrentServer.IsiTunesRadioNextButtonEnabled;
                _playTransportPreviousTrackButton.IsEnabled = CurrentServer.IsiTunesRadioMenuEnabled;
            }
            else if (CurrentServer != null && CurrentServer.IsCurrentlyPlayingGeniusShuffle)
            {
                _playTransportPreviousTrackButton.Text = LocalizedStrings.GeniusShuffleAppBarButton;
                _playTransportPreviousTrackButton.IconUri = _playTransportGeniusShuffleIcon;
            }
            else
            {
                _playTransportPreviousTrackButton.Text = LocalizedStrings.PreviousAppBarButton;
                _playTransportPreviousTrackButton.IconUri = _playTransportPreviousTrackIcon;
            }
        }

        #endregion

        #region "Now Playing" Items

        private ApplicationBarIconButton _nowPlayingButton;
        private ApplicationBarMenuItem _nowPlayingMenuItem;

        protected void AddAppBarNowPlayingButton()
        {
            _nowPlayingButton = AddApplicationBarIconButton(LocalizedStrings.NowPlayingAppBarButton, ResolutionUtility.GetUriWithResolutionSuffix("/Assets/Icons/App.iTunes.png"), NavigationManager.OpenNowPlayingPage);
        }

        protected void AddAppBarNowPlayingMenuItem()
        {
            _nowPlayingMenuItem = AddApplicationBarMenuItem(LocalizedStrings.NowPlayingAppBarButton, NavigationManager.OpenNowPlayingPage);
        }

        private void UpdateAppBarNowPlayingButtons()
        {
            ThreadUtility.RunOnUIThread(() =>
            {
                bool isEnabled = (CurrentServer != null && CurrentServer.IsConnected && !(CurrentServer.PlayState == PlayStates.Stopped && CurrentServer.CurrentSongName == null));

                if (_nowPlayingButton != null)
                    _nowPlayingButton.IsEnabled = isEnabled;

                if (_nowPlayingMenuItem != null)
                    _nowPlayingMenuItem.IsEnabled = isEnabled;
            });
        }

        #endregion

#if WP8
        #region Apple TV Control Button

        private ApplicationBarIconButton _appleTVControlButton;

        protected void EnableAppleTVControlButton()
        {
            var button = new ApplicationBarIconButton();
            button.Text = "Apple TV";
            button.IconUri = ResolutionUtility.GetUriWithResolutionSuffix("/Assets/Icons/AppleTVControl.png");
            button.Click += (sender, e) => OpenAppleTVControlDialog();

            _appleTVControlButton = button;
            UpdateAppleTVControlButton();
        }

        private void UpdateAppleTVControlButton()
        {
            if (_appleTVControlButton == null || ApplicationBar == null)
                return;

            var server = CurrentServer;

            bool isVisible = (server != null && server.IsConnected && server.IsAppleTV);

            if (isVisible)
            {
                if (!ApplicationBar.Buttons.Contains(_appleTVControlButton))
                    ApplicationBar.Buttons.Add(_appleTVControlButton);
            }
            else
            {
                ApplicationBar.Buttons.Remove(_appleTVControlButton);
            }
        }

        protected void OpenAppleTVControlDialog()
        {
            var dialog = new AppleTVControlDialog();
            ShowDialog(dialog);
        }

        #endregion
#endif

        #endregion
    }
}
