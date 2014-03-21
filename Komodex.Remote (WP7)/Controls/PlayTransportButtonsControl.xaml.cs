using Komodex.DACP;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace Komodex.Remote.Controls
{
    public partial class PlayTransportButtonsControl : UserControl
    {
        public PlayTransportButtonsControl()
        {
            InitializeComponent();

            Loaded += PlayTransportButtonsControl_Loaded;
            Unloaded += PlayTransportButtonsControl_Unloaded;
        }

        private void PlayTransportButtonsControl_Loaded(object sender, RoutedEventArgs e)
        {
            AttachServerEvents(Server);
            UpdatePlayTransportButtons();
        }

        private void PlayTransportButtonsControl_Unloaded(object sender, RoutedEventArgs e)
        {
            DetachServerEvents(Server);
        }

        #region Server Property

        public static readonly DependencyProperty ServerProperty =
            DependencyProperty.Register("Server", typeof(DACPServer), typeof(PlayTransportButtonsControl), new PropertyMetadata(ServerPropertyChanged));

        public DACPServer Server
        {
            get { return (DACPServer)GetValue(ServerProperty); }
            set { SetValue(ServerProperty, value); }
        }

        private static void ServerPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PlayTransportButtonsControl control = (PlayTransportButtonsControl)d;

            control.AttachServerEvents(e.NewValue as DACPServer);
        }

        #endregion

        #region Server Events

        private DACPServer _attachedServer;

        protected void AttachServerEvents(DACPServer server)
        {
            if (server == _attachedServer)
                return;
            DetachServerEvents(_attachedServer);

            if (server == null)
                return;

            server.PropertyChanged += Server_PropertyChanged;

            _attachedServer = server;
        }

        protected void DetachServerEvents(DACPServer server)
        {
            if (server == null)
                return;

            server.PropertyChanged -= Server_PropertyChanged;

            if (server == _attachedServer)
                _attachedServer = null;
        }

        private void Server_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
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
                    UpdatePlayTransportButtons();
                    break;
            }
        }

        #endregion

        #region Play Transport Buttons

        protected readonly ControlTemplate _playIcon = App.Current.Resources["PlayTransportPlayIcon"] as ControlTemplate;
        protected readonly ControlTemplate _playIconPressed = App.Current.Resources["PlayTransportPlayPressedIcon"] as ControlTemplate;
        protected readonly ControlTemplate _pauseIcon = App.Current.Resources["PlayTransportPauseIcon"] as ControlTemplate;
        protected readonly ControlTemplate _pauseIconPressed = App.Current.Resources["PlayTransportPausePressedIcon"] as ControlTemplate;

        protected void UpdatePlayTransportButtons()
        {
            bool isStopped = true;
            bool isPlaying = false;

            if (Server != null)
            {
                isStopped = (Server.PlayState == PlayStates.Stopped && Server.CurrentSongName == null);
                isPlaying = (Server.PlayState == PlayStates.Playing || Server.PlayState == PlayStates.FastForward || Server.PlayState == PlayStates.Rewind);
            }

            RewButton.IsEnabled = !isStopped;
            PlayPauseButton.IsEnabled = !isStopped;
            FFButton.IsEnabled = !isStopped;

            if (isPlaying)
            {
                PlayPauseButton.IconTemplate = _pauseIcon;
                PlayPauseButton.PressedIconTemplate = _pauseIconPressed;
            }
            else
            {
                PlayPauseButton.IconTemplate = _playIcon;
                PlayPauseButton.PressedIconTemplate = _playIconPressed;
            }

            // iTunes Radio/Genius Shuffle
            if (Server != null && Server.IsCurrentlyPlayingiTunesRadio)
            {
                RewButton.Visibility = Visibility.Collapsed;
                iTunesRadioButton.Visibility = Visibility.Visible;
                GeniusShuffleButton.Visibility = Visibility.Collapsed;
                FFButton.IsEnabled = !isStopped && Server.IsiTunesRadioNextButtonEnabled;
                iTunesRadioButton.IsEnabled = Server.IsiTunesRadioMenuEnabled;
            }
            else if (Server != null && Server.IsCurrentlyPlayingGeniusShuffle)
            {
                RewButton.Visibility = Visibility.Collapsed;
                iTunesRadioButton.Visibility = Visibility.Collapsed;
                GeniusShuffleButton.Visibility = Visibility.Visible;
            }
            else
            {
                RewButton.Visibility = Visibility.Visible;
                iTunesRadioButton.Visibility = Visibility.Collapsed;
                GeniusShuffleButton.Visibility = Visibility.Collapsed;
            }
        }

        private void RewButton_Click(object sender, RoutedEventArgs e)
        {
            if (Server == null || !Server.IsConnected)
                return;

            Server.SendPrevItemCommand();
        }

        private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (Server == null || !Server.IsConnected)
                return;

            Server.SendPlayPauseCommand();
        }

        private void FFButton_Click(object sender, RoutedEventArgs e)
        {
            if (Server == null || !Server.IsConnected)
                return;

            Server.SendNextItemCommand();
        }

        private void RewButton_RepeatBegin(object sender, EventArgs e)
        {
            if (Server == null || !Server.IsConnected)
                return;

            Server.SendBeginRewCommand();
        }

        private void RewButton_RepeatEnd(object sender, EventArgs e)
        {
            if (Server == null || !Server.IsConnected)
                return;

            Server.SendPlayResumeCommand();
        }

        private void FFButton_RepeatBegin(object sender, EventArgs e)
        {
            if (Server == null || !Server.IsConnected)
                return;

            Server.SendBeginFFCommand();
        }

        private void FFButton_RepeatEnd(object sender, EventArgs e)
        {
            if (Server == null || !Server.IsConnected)
                return;

            Server.SendPlayResumeCommand();
        }

        #endregion

        #region iTunes Radio

        private void iTunesRadioButton_Click(object sender, RoutedEventArgs e)
        {
            iTunesRadioContextMenu.IsOpen = true;
        }

        private async void iTunesRadioPlayMoreLikeThisMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (Server == null || !Server.IsConnected)
                return;

            await Server.SendiTunesRadioPlayMoreLikeThisAsync();
        }

        private async void iTunesRadioNeverPlayThisSongMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (Server == null || !Server.IsConnected)
                return;

            await Server.SendiTunesRadioNeverPlayThisSongAsync();
        }

        #endregion

        #region Genius Shuffle

        private async void GeniusShuffleButton_Click(object sender, RoutedEventArgs e)
        {
            if (Server == null || !Server.IsConnected)
                return;

            await Server.SendGeniusShuffleCommandAsync();
        }

        #endregion
    }

}
