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

namespace Komodex.Remote.Pages
{
    public partial class NowPlayingPage : RemoteBasePage
    {
        public NowPlayingPage()
        {
            InitializeComponent();

#if WP7
            PlayPositionProgressBar.Background = Resources["PhoneForegroundBrush"] as Brush;
#endif

            // Set up Application Bar
            InitializeApplicationBar();
            ApplicationBar.Mode = ApplicationBarMode.Minimized;
            //ApplicationBarMenuClosedOpacity = 0;
            ApplicationBar.BackgroundColor = (Color)Application.Current.Resources["PhoneBackgroundColor"];

            // Icon Buttons
            AddApplicationBarIconButton(LocalizedStrings.BrowseLibraryAppBarButton, ResolutionUtility.GetUriWithResolutionSuffix("/Assets/Icons/Browse.png"), NavigationManager.OpenMainLibraryPage);
            AddApplicationBarIconButton(LocalizedStrings.SearchAppBarButton, ResolutionUtility.GetUriWithResolutionSuffix("/Assets/Icons/Search.png"), NavigationManager.OpenSearchPage);

            RebuildApplicationBarMenuItems();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

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

        protected override void CurrentServer_ServerUpdate(object sender, ServerUpdateEventArgs e)
        {
            base.CurrentServer_ServerUpdate(sender, e);

            Utility.BeginInvokeOnUIThread(UpdateGoBackTimer);
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

                case "CurrentMediaKind":
                    UpdateControlEnabledStates();
                    break;

                case "VisualizerAvailable":
                case "VisualizerActive":
                    UpdateVisualizerButtons();
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

            UpdateAirPlayButtons();
            UpdatePlayQueueButtons();
            UpdateVisualizerButtons();
        }

        #endregion

        #region Navigation Buttons

        private void LibraryButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationManager.OpenMainLibraryPage();
        }

        private void ArtistButton_Click(object sender, RoutedEventArgs e)
        {
            switch (SettingsManager.Current.ArtistClickAction)
            {
                case ArtistClickAction.OpenArtistPage:
                    if (CurrentServer != null && CurrentServer.CurrentArtist != null)
                        NavigationManager.OpenArtistPage(CurrentServer.CurrentArtist);
                    break;

                case ArtistClickAction.OpenAlbumPage:
                    if (CurrentServer != null && CurrentServer.CurrentArtist != null && CurrentServer.CurrentAlbum != null && CurrentServer.CurrentAlbumPersistentID != 0)
                        NavigationManager.OpenAlbumPage(0, CurrentServer.CurrentAlbum, CurrentServer.CurrentArtist, CurrentServer.CurrentAlbumPersistentID);
                    break;
            }
        }

        #endregion

        #region Play Mode Buttons

        protected const double _playModeButtonOpacityOff = 0.4;
        protected const double _playModeButtonOpacityOn = 0.8;

        protected readonly ImageSource _repeatIcon = new BitmapImage(new Uri("/Assets/Icons/Repeat.png", UriKind.Relative));
        protected readonly ImageSource _repeatOneIcon = new BitmapImage(new Uri("/Assets/Icons/RepeatOne.png", UriKind.Relative));

        protected void UpdatePlayModeButtons()
        {
            if (CurrentServer == null || !CurrentServer.IsConnected)
                return;

            // Shuffle button
            ShuffleImage.Opacity = (CurrentServer.ShuffleState) ? _playModeButtonOpacityOn : _playModeButtonOpacityOff;

            // Repeat button
            RepeatImage.Opacity = (CurrentServer.RepeatState != RepeatStates.None) ? _playModeButtonOpacityOn : _playModeButtonOpacityOff;
            RepeatImage.Source = (CurrentServer.RepeatState != RepeatStates.RepeatOne) ? _repeatIcon : _repeatOneIcon;
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
            Utility.BeginInvokeOnUIThread(UpdateAirPlayButtons);
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