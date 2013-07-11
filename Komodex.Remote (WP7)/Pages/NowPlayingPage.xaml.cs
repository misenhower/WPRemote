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
            ApplicationBarMenuClosedOpacity = 0;
            ApplicationBar.BackgroundColor = (Color)Application.Current.Resources["PhoneBackgroundColor"];
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            UpdatePlayTransportButtons();
            UpdatePlayModeButtons();

            UpdateArtistName();
            ArtistBackgroundImageManager.CurrentArtistImageSourceUpdated += ArtistBackgroundImageManager_CurrentArtistImageSourceUpdated;
            SetArtistBackgroundImage(false);
        }
        
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            ArtistBackgroundImageManager.CurrentArtistImageSourceUpdated -= ArtistBackgroundImageManager_CurrentArtistImageSourceUpdated;
        }

        protected override void CurrentServer_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            base.CurrentServer_PropertyChanged(sender, e);

            switch (e.PropertyName)
            {
                case "PlayState":
                case "CurrentSongName":
                    UpdatePlayTransportButtons();
                    break;

                case "ShuffleState":
                case "RepeatState":
                    UpdatePlayModeButtons();
                    break;

                case "CurrentArtist":
                    UpdateArtistName();
                    break;
            }
        }

        #region Play Transport Buttons

        protected readonly ImageSource _playIcon = new BitmapImage(new Uri("/Assets/PlayTransport/Play.png", UriKind.Relative));
        protected readonly ImageSource _pauseIcon = new BitmapImage(new Uri("/Assets/PlayTransport/Pause.png", UriKind.Relative));

        protected void UpdatePlayTransportButtons()
        {
            if (CurrentServer == null || !CurrentServer.IsConnected)
                return;

            bool isStopped = true;
            bool isPlaying = false;

            if (CurrentServer != null)
            {
                isStopped = (CurrentServer.PlayState == PlayStates.Stopped && CurrentServer.CurrentSongName == null);
                isPlaying = (CurrentServer.PlayState == PlayStates.Playing || CurrentServer.PlayState == PlayStates.FastForward || CurrentServer.PlayState == PlayStates.Rewind);
            }

            RewButton.IsEnabled = !isStopped;
            PlayPauseButton.IsEnabled = !isStopped;
            FFButton.IsEnabled = !isStopped;

            if (isPlaying)
                PlayPauseButton.ImageSource = _pauseIcon;
            else
                PlayPauseButton.ImageSource = _playIcon;
        }

        private void RewButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentServer == null || !CurrentServer.IsConnected)
                return;

            CurrentServer.SendPrevItemCommand();
        }

        private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentServer == null || !CurrentServer.IsConnected)
                return;

            CurrentServer.SendPlayPauseCommand();
        }

        private void FFButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentServer == null || !CurrentServer.IsConnected)
                return;

            CurrentServer.SendNextItemCommand();
        }

        private void RewButton_RepeatBegin(object sender, EventArgs e)
        {
            if (CurrentServer == null || !CurrentServer.IsConnected)
                return;

            CurrentServer.SendBeginRewCommand();
        }

        private void RewButton_RepeatEnd(object sender, EventArgs e)
        {
            if (CurrentServer == null || !CurrentServer.IsConnected)
                return;

            CurrentServer.SendPlayResumeCommand();
        }

        private void FFButton_RepeatBegin(object sender, EventArgs e)
        {
            if (CurrentServer == null || !CurrentServer.IsConnected)
                return;

            CurrentServer.SendBeginFFCommand();
        }

        private void FFButton_RepeatEnd(object sender, EventArgs e)
        {
            if (CurrentServer == null || !CurrentServer.IsConnected)
                return;

            CurrentServer.SendPlayResumeCommand();
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

        protected void UpdateArtistName()
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

    }
}