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
using Microsoft.Phone.Shell;
using Komodex.DACP;
using System.Windows.Threading;
using System.Windows.Media.Imaging;

namespace Komodex.WP7DACPRemote
{
    public partial class NowPlayingPage : DACPServerBoundPhoneApplicationPage
    {
        public NowPlayingPage()
        {
            InitializeComponent();

            InitializeStandardTransportApplicationBar();

            AnimationContext = LayoutRoot;

            playControlDisplayTimer.Interval = TimeSpan.FromSeconds(5);
            playControlDisplayTimer.Tick += new EventHandler(playControlDisplayTimer_Tick);
        }

        private readonly BitmapImage iconRepeat = new BitmapImage(new Uri("/icons/custom.appbar.repeat.png", UriKind.Relative));
        private readonly BitmapImage iconRepeatOne = new BitmapImage(new Uri("/icons/custom.appbar.repeatone.png", UriKind.Relative));

        #region Overrides

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            HidePlayControls(false);
            UpdateRepeatShuffleButtons();

            GestureListener.IgnoreTouchFrameReported = true;
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            GestureListener.IgnoreTouchFrameReported = false;
        }

        protected override void DACPServer_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            base.DACPServer_PropertyChanged(sender, e);

            if (e.PropertyName == "ShuffleState" || e.PropertyName == "RepeatState")
                UpdateRepeatShuffleButtons();
        }

        #endregion

        #region Play Control Management

        protected DispatcherTimer playControlDisplayTimer = new DispatcherTimer();

        void playControlDisplayTimer_Tick(object sender, EventArgs e)
        {
            playControlDisplayTimer.Stop();
            HidePlayControls();
        }

        private void ShowPlayControls(bool useTransitions = true)
        {
            playControlDisplayTimer.Stop();
            VisualStateManager.GoToState(this, "PlayControlsVisibleState", useTransitions);
            playControlDisplayTimer.Start();
        }

        private void HidePlayControls(bool useTransitions = true)
        {
            playControlDisplayTimer.Stop();
            VisualStateManager.GoToState(this, "PlayControlsHiddenState", useTransitions);
        }

        private void btnRepeat_Click(object sender, RoutedEventArgs e)
        {
            ShowPlayControls();
            if (DACPServer != null && DACPServer.IsConnected)
                DACPServer.SendRepeatStateCommand();
        }

        private void btnShuffle_Click(object sender, RoutedEventArgs e)
        {
            ShowPlayControls();
            if (DACPServer != null && DACPServer.IsConnected)
                DACPServer.SendShuffleStateCommand();
        }

        private void UpdateRepeatShuffleButtons()
        {
            if (DACPServer == null || !DACPServer.IsConnected)
                return;

            // Repeat button
            btnRepeat.Opacity = (DACPServer.RepeatState != RepeatStates.None) ? 1.0 : 0.5;
            imgRepeat.Source = (DACPServer.RepeatState != RepeatStates.RepeatOne) ? iconRepeat : iconRepeatOne;

            // Shuffle button
            btnShuffle.Opacity = (DACPServer.ShuffleState) ? 1.0 : 0.5;
        }

        #endregion

        #region Actions

        private void btnLibrary_Click(object sender, RoutedEventArgs e)
        {
            NavigationManager.OpenMainLibraryPage();
        }

        private void mnuBrowse_Click(object sender, EventArgs e)
        {
            NavigationManager.OpenMainLibraryPage();
        }

        private void mnuSearch_Click(object sender, EventArgs e)
        {
            NavigationManager.OpenSearchPage();
        }

        private void mnuSettings_Click(object sender, EventArgs e)
        {
            NavigationManager.OpenLibraryChooserPage();
        }

        private void btnArtist_Click(object sender, RoutedEventArgs e)
        {
            NavigationManager.OpenArtistPage(DACPServer.CurrentArtist);
        }

        private void bdrPlayControls_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ShowPlayControls();
            e.Handled = true;
        }

        private void Page_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            HidePlayControls();
        }

        #endregion

    }
}