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

            InitializeStandardPlayTransportApplicationBar();

            AnimationContext = LayoutRoot;

            repeatShuffleControlDisplayTimer.Interval = TimeSpan.FromSeconds(5);
            repeatShuffleControlDisplayTimer.Tick += new EventHandler(repeatShuffleControlDisplayTimer_Tick);

            goBackTimer.Interval = TimeSpan.FromMilliseconds(500);
            goBackTimer.Tick += new EventHandler(goBackTimer_Tick);
        }

        private readonly BitmapImage iconRepeat = new BitmapImage(new Uri("/icons/custom.appbar.repeat.png", UriKind.Relative));
        private readonly BitmapImage iconRepeatOne = new BitmapImage(new Uri("/icons/custom.appbar.repeatone.png", UriKind.Relative));

        private DispatcherTimer goBackTimer = new DispatcherTimer();
        private bool closing = false;

        #region Overrides

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            HideRepeatShuffleControls(false);
            UpdateRepeatShuffleButtons();

            GestureListener.IgnoreTouchFrameReported = true;

            if (State.ContainsKey(StateUtils.SavedStateKey))
            {
                if (ShouldGoBack())
                {
                    closing = true;
                    AnimationContext = null;
                }
            }
        }

        protected override void AnimationsComplete(Clarity.Phone.Controls.Animations.AnimationType animationType)
        {
            base.AnimationsComplete(animationType);

            if (closing)
                NavigationService.GoBack();
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            goBackTimer.Stop();

            base.OnNavigatedFrom(e);

            GestureListener.IgnoreTouchFrameReported = false;

            State[StateUtils.SavedStateKey] = true;
        }

        protected override void DACPServer_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            base.DACPServer_PropertyChanged(sender, e);

            if (e.PropertyName == "ShuffleState" || e.PropertyName == "RepeatState")
                UpdateRepeatShuffleButtons();

            if (e.PropertyName == "PlayState" || e.PropertyName == "CurrentSongName")
            {
                goBackTimer.Stop();
                if (ShouldGoBack())
                    goBackTimer.Start();
            }
        }

        #endregion

        #region Play Control Management

        protected DispatcherTimer repeatShuffleControlDisplayTimer = new DispatcherTimer();

        void repeatShuffleControlDisplayTimer_Tick(object sender, EventArgs e)
        {
            repeatShuffleControlDisplayTimer.Stop();
            HideRepeatShuffleControls();
        }

        private void ShowRepeatShuffleControls(bool useTransitions = true)
        {
            repeatShuffleControlDisplayTimer.Stop();
            VisualStateManager.GoToState(this, "RepeatShuffleControlsVisibleState", useTransitions);
            repeatShuffleControlDisplayTimer.Start();
        }

        private void HideRepeatShuffleControls(bool useTransitions = true)
        {
            repeatShuffleControlDisplayTimer.Stop();
            VisualStateManager.GoToState(this, "RepeatShuffleControlsHiddenState", useTransitions);
        }

        private void btnRepeat_Click(object sender, RoutedEventArgs e)
        {
            ShowRepeatShuffleControls();
            if (DACPServer != null && DACPServer.IsConnected)
                DACPServer.SendRepeatStateCommand();
        }

        private void btnShuffle_Click(object sender, RoutedEventArgs e)
        {
            ShowRepeatShuffleControls();
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

        private void bdrRepeatShuffleControls_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ShowRepeatShuffleControls();
            e.Handled = true;
        }

        private void Page_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            HideRepeatShuffleControls();
        }

        #endregion

        #region Methods

        protected bool ShouldGoBack()
        {
            return (DACPServer != null && DACPServer.IsConnected && DACPServer.PlayState == DACP.PlayStates.Stopped && DACPServer.CurrentSongName == null);
        }

        #endregion

        #region Event Handlers

        void goBackTimer_Tick(object sender, EventArgs e)
        {
            NavigationService.GoBack();
        }

        #endregion
    }
}