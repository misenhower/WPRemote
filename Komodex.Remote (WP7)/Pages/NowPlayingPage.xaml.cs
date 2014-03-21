using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Komodex.Common;
using Komodex.DACP;
using Komodex.Remote.Marketplace;
using Komodex.Remote.Settings;
using Komodex.Remote.Controls;
using Komodex.Remote.Localization;
using System.Windows.Threading;
using System.ComponentModel;
using System.Windows.Input;
using Komodex.DACP.Items;
using Komodex.DACP.Containers;

namespace Komodex.Remote.Pages
{
    public partial class NowPlayingPage : RemoteBasePage
    {
        public NowPlayingPage()
        {
            InitializeComponent();

            // Set up Application Bar
            InitializeApplicationBar();
            ApplicationBar.Mode = ApplicationBarMode.Minimized;
            //ApplicationBarMenuClosedOpacity = 0;
            ApplicationBar.BackgroundColor = (Color)Application.Current.Resources["PhoneBackgroundColor"];

            // Icon Buttons
            AddApplicationBarIconButton(LocalizedStrings.BrowseLibraryAppBarButton, ResolutionUtility.GetUriWithResolutionSuffix("/Assets/Icons/Browse.png"), () => NavigationManager.OpenLibraryPage(CurrentServer.MainDatabase));
            AddApplicationBarIconButton(LocalizedStrings.SearchAppBarButton, ResolutionUtility.GetUriWithResolutionSuffix("/Assets/Icons/Search.png"), () => NavigationManager.OpenSearchPage(CurrentServer.MainDatabase));

            RebuildApplicationBarMenuItems();

            ManipulationStarted += Page_ManipulationStarted;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            HideAlbumArtOverlay(false);

            UpdateLibraryTitleText();
            UpdateControlEnabledStates();
            UpdatePlayModeButtons();

            App.RootFrame.Obscured += RootFrame_Obscured;
            App.RootFrame.Unobscured += RootFrame_Unobscured;

            // Go Back Timer
            UpdateGoBackTimer();

            // Artist Background Image
            if (SettingsManager.Current.ShowArtistBackgroundImages)
            {
                UpdateArtistBackgroundImageName();
                ArtistBackgroundImageManager.CurrentArtistImageSourceUpdated += ArtistBackgroundImageManager_CurrentArtistImageSourceUpdated;
                SetArtistBackgroundImage(false);
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            App.RootFrame.Obscured -= RootFrame_Obscured;
            App.RootFrame.Unobscured -= RootFrame_Unobscured;

            // Go Back Timer
            if (_goBackTimer != null)
                _goBackTimer.Stop();

            // Artist Background Image
            ArtistBackgroundImageManager.CurrentArtistImageSourceUpdated -= ArtistBackgroundImageManager_CurrentArtistImageSourceUpdated;
        }

        private void RootFrame_Obscured(object sender, ObscuredEventArgs e)
        {
            if (_goBackTimer != null)
                _goBackTimer.Stop();
        }

        private void RootFrame_Unobscured(object sender, EventArgs e)
        {
            UpdateGoBackTimer();
        }

        #region Server Events

        protected override void ServerManager_ConnectionStateChanged(object sender, ServerManagement.ConnectionStateChangedEventArgs e)
        {
            base.ServerManager_ConnectionStateChanged(sender, e);

            ThreadUtility.RunOnUIThread(UpdateGoBackTimer);
        }

        protected override void CurrentServer_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.CurrentServer_PropertyChanged(sender, e);

            switch (e.PropertyName)
            {
                case "PlayState":
                case "CurrentSongName":
                    UpdateGoBackTimer();
                    break;

                case "ShuffleState":
                case "RepeatState":
                    UpdatePlayModeButtons();
                    break;

                case "CurrentArtist":
                    UpdateArtistBackgroundImageName();
                    break;

                case "CurrentContainerID":
                case "CurrentMediaKind":
                    UpdateControlEnabledStates();
                    break;

                case "VisualizerAvailable":
                case "VisualizerActive":
                    UpdateVisualizerButtons();
                    break;

                case "CurrentDatabaseID":
                case "IsCurrentlyPlayingiTunesRadio":
                case "CurrentiTunesRadioStationName":
                    UpdateLibraryTitleText();
                    break;
            }
        }

        protected override void OnServerChanged()
        {
            base.OnServerChanged();

            UpdateControlEnabledStates();
        }

        #endregion

        #region Go Back Timer

        protected DispatcherTimer _goBackTimer;

        protected void UpdateGoBackTimer()
        {
            if (_goBackTimer != null)
                _goBackTimer.Stop();

            if (CurrentServer == null || !CurrentServer.IsConnected)
                return;

            if (CurrentServer.PlayState == PlayStates.Stopped && CurrentServer.CurrentSongName == null)
            {
                if (_goBackTimer == null)
                {
                    _goBackTimer = new DispatcherTimer();
                    _goBackTimer.Interval = TimeSpan.FromSeconds(5);
                    _goBackTimer.Tick += GoBackTimer_Tick;
                }

                _goBackTimer.Start();
            }
        }

        private void GoBackTimer_Tick(object sender, EventArgs e)
        {
            NavigationService.GoBack();
        }

        #endregion

        #region Page Structure

        protected void RebuildApplicationBarMenuItems()
        {
            if (ApplicationBar == null)
                return;

            ApplicationBar.MenuItems.Clear();

            if (ShowAirPlayAppBarMenuItem)
                AddAirPlayAppBarMenuItem();

            if (ShowCurrentPlaylistMenuItem)
                AddCurrentPlaylistMenuItem();

            if (ShowPlayQueueAppBarMenuItem)
                AddPlayQueueAppBarMenuItem();

            if (ShowVisualizerAppBarMenuItem)
                AddVisualizerAppBarMenuItem();
        }

        protected void UpdateControlEnabledStates()
        {
            bool enableArtistButton = false;
            bool enableVolumeSlider = true;

            if (CurrentServer != null && CurrentServer.IsConnected)
            {
                // Artist/Album button
                enableArtistButton = (CurrentServer.CurrentMediaKind == 1);

                // Volume control
                if (CurrentServer.IsCurrentlyPlayingVideo)
                {
                    var mainSpeaker = CurrentServer.Speakers.FirstOrDefault(s => s.ID == 0);
                    if (mainSpeaker == null || !mainSpeaker.Active)
                        enableVolumeSlider = false;
                }
            }

            ArtistButton.IsEnabled = enableArtistButton;
            VolumeSlider.IsEnabled = enableVolumeSlider;

            UpdateCurrentPlaylistMenuItem();
            UpdateAirPlayButtons();
            UpdatePlayQueueButtons();
            UpdateVisualizerButtons();
        }

        #endregion

        #region Navigation Buttons

        private async void ArtistButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentServer == null || !CurrentServer.IsConnected)
                return;

            // TODO: Verify the DB version and media kind instead
            // TODO: Get the correct DB and container

            //if (CurrentServer.CurrentContainerID != CurrentServer.MainDatabase.MusicContainer.ID)
            //    return;

            switch (SettingsManager.Current.ArtistClickAction)
            {
                case ArtistClickAction.OpenArtistPage:
                    if (string.IsNullOrEmpty(CurrentServer.CurrentArtist))
                        return;

                    // Get artists
                    var artists = await CurrentServer.MainDatabase.MusicContainer.GetArtistsAsync();
                    if (artists == null)
                        return;

                    // Find artist by name
                    var artist = artists.FirstOrDefault(a => a.Name == CurrentServer.CurrentArtist);
                    if (artist == null)
                        return;

                    NavigationManager.OpenArtistPage(artist);
                    break;

                case ArtistClickAction.OpenAlbumPage:
                    if (CurrentServer.CurrentAlbumPersistentID == 0)
                        return;

                    // Get albums
                    var albums = await CurrentServer.MainDatabase.MusicContainer.GetAlbumsAsync();
                    if (albums == null)
                        return;

                    // Find album by persistent ID
                    var album = albums.FirstOrDefault(a => a.PersistentID == CurrentServer.CurrentAlbumPersistentID);
                    if (album == null)
                        return;

                    NavigationManager.OpenAlbumPage(album);
                    break;
            }
        }

        #endregion

        #region Play Mode Buttons

        protected const double _playModeButtonOpacityOff = 0.4;
        protected const double _playModeButtonOpacityOn = 0.8;

        protected readonly ControlTemplate _repeatIcon = App.Current.Resources["RepeatAllIcon"] as ControlTemplate;
        protected readonly ControlTemplate _repeatOneIcon = App.Current.Resources["RepeatOneIcon"] as ControlTemplate;

        protected void UpdatePlayModeButtons()
        {
            if (CurrentServer == null || !CurrentServer.IsConnected)
                return;

            // Shuffle button
            ShuffleImage.Opacity = (CurrentServer.ShuffleState) ? _playModeButtonOpacityOn : _playModeButtonOpacityOff;

            // Repeat button
            RepeatImage.Opacity = (CurrentServer.RepeatState != RepeatStates.None) ? _playModeButtonOpacityOn : _playModeButtonOpacityOff;
            RepeatImage.Template = (CurrentServer.RepeatState != RepeatStates.RepeatOne) ? _repeatIcon : _repeatOneIcon;
        }

        private void ShuffleButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentServer == null || !CurrentServer.IsConnected)
                return;

            CurrentServer.SendShuffleStateCommand();
        }

        private void RepeatButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentServer == null || !CurrentServer.IsConnected)
                return;

            CurrentServer.SendRepeatStateCommand();
        }

        #endregion

        #region Album Art Overlay

        protected DispatcherTimer _albumArtOverlayTimer;

        protected void ShowAlbumArtOverlay(bool useTransitions)
        {
            if (!CurrentServer.ShowUserRating)
                return;

            VisualStateManager.GoToState(this, "AlbumArtOverlayVisibleState", useTransitions);
        }

        protected void HideAlbumArtOverlay(bool useTransitions)
        {
            VisualStateManager.GoToState(this, "AlbumArtOverlayHiddenState", useTransitions);
        }

        private void AlbumArtOverlayBorder_ManipulationStarted(object sender, ManipulationStartedEventArgs e)
        {
            StopAlbumArtOverlayTimer();
            ShowAlbumArtOverlay(true);
            e.Handled = true;
        }

        private void AlbumArtOverlayBorder_ManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            StartAlbumArtOverlayTimer();
            e.Handled = true;
        }

        private void Page_ManipulationStarted(object sender, System.Windows.Input.ManipulationStartedEventArgs e)
        {
            // Hiding the overlay directly seems to skip its animation.
            // TODO: Look for a better alternative.
            if (_albumArtOverlayTimer != null)
                _albumArtOverlayTimer.Interval = TimeSpan.FromSeconds(0);
        }

        protected void StartAlbumArtOverlayTimer()
        {
            if (_albumArtOverlayTimer == null)
            {
                _albumArtOverlayTimer = new DispatcherTimer();
                _albumArtOverlayTimer.Tick += AlbumArtOverlayTimer_Tick;
            }

            _albumArtOverlayTimer.Interval = TimeSpan.FromSeconds(4);
            _albumArtOverlayTimer.Start();
        }
        
        protected void StopAlbumArtOverlayTimer()
        {
            if (_albumArtOverlayTimer == null)
                return;

            _albumArtOverlayTimer.Stop();
        }

        private void AlbumArtOverlayTimer_Tick(object sender, EventArgs e)
        {
            _albumArtOverlayTimer.Stop();

            HideAlbumArtOverlay(true);
        }

        #endregion

        #region Artist Background Images

        protected void UpdateArtistBackgroundImageName()
        {
            if (CurrentServer == null || !CurrentServer.IsConnected)
                return;

            ArtistBackgroundImageManager.SetArtistName(CurrentServer.CurrentArtist);
        }

        private void ArtistBackgroundImageManager_CurrentArtistImageSourceUpdated(object sender, ArtistBackgroundImageSourceUpdatedEventArgs e)
        {
            SetArtistBackgroundImage(true);
        }

        private string _currentArtistBackgroundImageArtistName;

        protected void SetArtistBackgroundImage(bool useTransitions)
        {
            if (CurrentServer == null || !CurrentServer.IsConnected)
                return;

            string newArtistName = ArtistBackgroundImageManager.CurrentArtistName;

            if (_currentArtistBackgroundImageArtistName == newArtistName || newArtistName != CurrentServer.CurrentArtist)
                return;

            ImageSource newImageSource = ArtistBackgroundImageManager.CurrentArtistImageSource;

            if (newImageSource != null)
                _currentArtistBackgroundImageArtistName = newArtistName;
            else
                _currentArtistBackgroundImageArtistName = null;

            ArtistBackgroundImage.SetImageSource(ArtistBackgroundImageManager.CurrentArtistImageSource, useTransitions);
        }

        #endregion

        #region Library Title Text

        public static readonly DependencyProperty LibraryTitleTextProperty =
            DependencyProperty.Register("LibraryTitleText", typeof(string), typeof(NowPlayingPage), new PropertyMetadata(null));

        public string LibraryTitleText
        {
            get { return (string)GetValue(LibraryTitleTextProperty); }
            set { SetValue(LibraryTitleTextProperty, value); }
        }

        public static readonly DependencyProperty SharedDatabaseVisibilityProperty =
            DependencyProperty.Register("SharedDatabaseVisibility", typeof(Visibility), typeof(NowPlayingPage), new PropertyMetadata(Visibility.Collapsed));

        public Visibility SharedDatabaseVisibility
        {
            get { return (Visibility)GetValue(SharedDatabaseVisibilityProperty); }
            set { SetValue(SharedDatabaseVisibilityProperty, value); }
        }

        public static readonly DependencyProperty PageTitleTextVisibilityProperty =
            DependencyProperty.Register("PageTitleTextVisibility", typeof(Visibility), typeof(NowPlayingPage), new PropertyMetadata(Visibility.Visible));

        public Visibility PageTitleTextVisibility
        {
            get { return (Visibility)GetValue(PageTitleTextVisibilityProperty); }
            set { SetValue(PageTitleTextVisibilityProperty, value); }
        }

        private void UpdateLibraryTitleText()
        {
            if (CurrentServer == null || !CurrentServer.IsConnected)
            {
                LibraryTitleText = null;
                PageTitleTextVisibility = Visibility.Visible;
                SharedDatabaseVisibility = Visibility.Collapsed;
                return;
            }

            if (CurrentServer.IsCurrentlyPlayingiTunesRadio)
            {
                LibraryTitleText = CurrentServer.CurrentiTunesRadioStationName;
                PageTitleTextVisibility = Visibility.Visible;
                SharedDatabaseVisibility = Visibility.Collapsed;
                return;
            }

            if (CurrentServer.CurrentDatabaseID != CurrentServer.MainDatabase.ID)
            {
                var sharedDB = CurrentServer.SharedDatabases.FirstOrDefault(db => db.ID == CurrentServer.CurrentDatabaseID);
                if (sharedDB != null)
                {
                    LibraryTitleText = sharedDB.Name;
                    PageTitleTextVisibility = Visibility.Collapsed;
                    SharedDatabaseVisibility = Visibility.Visible;
                    return;
                }
            }

            LibraryTitleText = CurrentServer.MainDatabase.Name;
            PageTitleTextVisibility = Visibility.Visible;
            SharedDatabaseVisibility = Visibility.Collapsed;
        }

        private void LibraryButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentServer == null || !CurrentServer.IsConnected)
                return;

            if (CurrentServer.IsCurrentlyPlayingiTunesRadio)
            {
                NavigationManager.OpeniTunesRadioStationsPage(CurrentServer.iTunesRadioDatabase);
                return;
            }

            if (CurrentServer.CurrentDatabaseID != CurrentServer.MainDatabase.ID)
            {
                var sharedDB = CurrentServer.SharedDatabases.FirstOrDefault(db => db.ID == CurrentServer.CurrentDatabaseID);
                if (sharedDB != null)
                {
                    NavigationManager.OpenLibraryPage(sharedDB);
                    return;
                }
            }

            NavigationManager.OpenLibraryPage(CurrentServer.MainDatabase);
        }

        #endregion

        #region Current Playlist

        protected void UpdateCurrentPlaylistMenuItem()
        {
            ShowCurrentPlaylistMenuItem = !(GetCurrentPlaylist() == null);
        }

        protected Playlist GetCurrentPlaylist()
        {
            if (CurrentServer == null || !CurrentServer.IsConnected || CurrentServer.CurrentContainerID == 0)
                return null;

            var db = CurrentServer.CurrentDatabase;
            if (db == null || (db != CurrentServer.MainDatabase && !CurrentServer.SharedDatabases.Contains(db)))
                return null;

            if (db.Playlists == null)
                return null;

            return db.Playlists.FirstOrDefault(pl => pl.ID == CurrentServer.CurrentContainerID);
        }

        private bool _showCurrentPlaylistMenuItem;
        protected bool ShowCurrentPlaylistMenuItem
        {
            get { return _showCurrentPlaylistMenuItem; }
            set
            {
                if (_showCurrentPlaylistMenuItem == value)
                    return;

                _showCurrentPlaylistMenuItem = value;
                RebuildApplicationBarMenuItems();
            }
        }

        protected void AddCurrentPlaylistMenuItem()
        {
            AddApplicationBarMenuItem(LocalizedStrings.ShowCurrentPlaylistMenuItem, OpenCurrentPlaylist);
        }

        private void OpenCurrentPlaylist()
        {
            var playlist = GetCurrentPlaylist();
            if (playlist == null)
                return;

            NavigationManager.OpenPlaylistPage(playlist);
        }

        #endregion

        #region AirPlay

        protected void UpdateAirPlayButtons()
        {
            if (CurrentServer == null)
                return;

            bool showAirPlayButtons = (CurrentServer.Speakers.Count > 1);

            // Rebuild the application bar if necessary
            ShowAirPlayAppBarMenuItem = showAirPlayButtons;

            // Update the AirPlay button
            if (showAirPlayButtons)
            {
                AirPlayButton.Visibility = Visibility.Visible;

                bool airPlayEnabled = CurrentServer.Speakers.Any(s => s.ID != 0 && s.Active);
                if (airPlayEnabled)
                    AirPlayButton.Opacity = _playModeButtonOpacityOn;
                else
                    AirPlayButton.Opacity = _playModeButtonOpacityOff;
            }
            else
            {
                AirPlayButton.Visibility = Visibility.Collapsed;
            }
        }

        private bool _showAirPlayAppBarMenuItem;
        protected bool ShowAirPlayAppBarMenuItem
        {
            get { return _showAirPlayAppBarMenuItem; }
            set
            {
                if (_showAirPlayAppBarMenuItem == value)
                    return;

                _showAirPlayAppBarMenuItem = value;
                RebuildApplicationBarMenuItems();
            }
        }

        protected void AddAirPlayAppBarMenuItem()
        {
            AddApplicationBarMenuItem(LocalizedStrings.AirPlaySpeakersMenuItem, ShowAirPlayDialog);
        }

        private void AirPlayButton_Click(object sender, RoutedEventArgs e)
        {
            ShowAirPlayDialog();
        }

        protected override void CurrentServer_AirPlaySpeakerUpdate(object sender, EventArgs e)
        {
            ThreadUtility.RunOnUIThread(UpdateAirPlayButtons);
        }

        protected void ShowAirPlayDialog()
        {
            if (IsDialogOpen)
                return;

            AirPlaySpeakersDialog dialog = new AirPlaySpeakersDialog();
            ShowDialog(dialog);
        }

        #endregion

        #region Play Queue

        protected void UpdatePlayQueueButtons()
        {
            if (CurrentServer == null)
                return;

            bool enablePlayQueueButtons = CurrentServer.SupportsPlayQueue;

            PlayQueueButton.IsEnabled = enablePlayQueueButtons;
            ShowPlayQueueAppBarMenuItem = enablePlayQueueButtons;
        }

        private bool _showPlayQueueAppBarMenuItem;
        protected bool ShowPlayQueueAppBarMenuItem
        {
            get { return _showPlayQueueAppBarMenuItem; }
            set
            {
                if (_showPlayQueueAppBarMenuItem == value)
                    return;

                _showPlayQueueAppBarMenuItem = value;
                RebuildApplicationBarMenuItems();
            }
        }

        protected void AddPlayQueueAppBarMenuItem()
        {
            AddApplicationBarMenuItem(LocalizedStrings.UpNextMenuItem, ShowPlayQueueDialog);
        }

        private void PlayQueueButton_Click(object sender, RoutedEventArgs e)
        {
            ShowPlayQueueDialog();
        }

        protected void ShowPlayQueueDialog()
        {
            if (IsDialogOpen)
                return;

            PlayQueueDialog dialog = new PlayQueueDialog();
            ShowDialog(dialog);
        }

        #endregion

        #region Visualizer

        protected void UpdateVisualizerButtons()
        {
            if (CurrentServer == null)
                return;

            ShowVisualizerAppBarMenuItem = CurrentServer.VisualizerAvailable;
            UpdateVisualizerMenuItem();
        }

        protected void UpdateVisualizerMenuItem()
        {
            if (CurrentServer == null || _visualizerMenuItem == null)
                return;

            _visualizerMenuItem.Text = (CurrentServer.VisualizerActive) ? LocalizedStrings.HideVisualizerMenuItem : LocalizedStrings.ShowVisualizerMenuItem;
        }

        private bool _showVisualizerAppBarMenuItem;
        protected bool ShowVisualizerAppBarMenuItem
        {
            get { return _showVisualizerAppBarMenuItem; }
            set
            {
                if (_showVisualizerAppBarMenuItem == value)
                    return;

                _showVisualizerAppBarMenuItem = value;
                RebuildApplicationBarMenuItems();
            }
        }

        ApplicationBarMenuItem _visualizerMenuItem;

        protected void AddVisualizerAppBarMenuItem()
        {
            _visualizerMenuItem = AddApplicationBarMenuItem(LocalizedStrings.ShowVisualizerMenuItem, ToggleVisualizer);
            UpdateVisualizerMenuItem();
        }

        protected void ToggleVisualizer()
        {
            if (CurrentServer == null)
                return;

            CurrentServer.SendVisualizerCommand(!CurrentServer.VisualizerActive);
        }

        #endregion

    }
}