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
                    UpdatePlayTransportButtons();
                    break;
            }
        }

        #endregion

        #region Play Transport Buttons

        protected readonly ImageSource _playIcon = new BitmapImage(new Uri("/Assets/PlayTransport/Play.png", UriKind.Relative));
        protected readonly ImageSource _pauseIcon = new BitmapImage(new Uri("/Assets/PlayTransport/Pause.png", UriKind.Relative));

        protected void UpdatePlayTransportButtons()
        {
            if (Server == null || !Server.IsConnected)
                return;

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
                PlayPauseButton.ImageSource = _pauseIcon;
            else
                PlayPauseButton.ImageSource = _playIcon;

            // iTunes Radio
            if (Server.IsCurrentlyPlayingiTunesRadio)
            {
                RewButton.Visibility = Visibility.Collapsed;
                iTunesRadioButton.Visibility = Visibility.Visible;
                FFButton.IsEnabled = !isStopped && Server.IsiTunesRadioNextButtonEnabled;
                iTunesRadioButton.IsEnabled = Server.IsiTunesRadioMenuEnabled;
            }
            else
            {
                RewButton.Visibility = Visibility.Visible;
                iTunesRadioButton.Visibility = Visibility.Collapsed;
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

        }

        #endregion

    }

}
