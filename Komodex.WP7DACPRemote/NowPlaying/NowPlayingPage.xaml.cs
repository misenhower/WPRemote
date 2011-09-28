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
using Clarity.Phone.Extensions;
using System.Collections.Specialized;
using Komodex.WP7DACPRemote.Settings;
using Komodex.WP7DACPRemote.Localization;

namespace Komodex.WP7DACPRemote.NowPlaying
{
    public partial class NowPlayingPage : DACPServerBoundPhoneApplicationPage
    {
        protected DialogService AirPlayDialog = null;
        protected AirPlaySpeakersControl AirPlaySpeakersControl = null;
        protected ApplicationBarMenuItem AirPlayMenuItem = null;

        // GoBackOnNextLoad is used for situations where a play command fails
        public static bool GoBackOnNextLoad { get; set; }

        public NowPlayingPage()
        {
            InitializeComponent();

            AnimationContext = LayoutRoot;

            // Application bar
            InitializeAppBar();
            InitializeStandardPlayTransportApplicationBar();

            // "Browse library" menu item
            ApplicationBarMenuItem browseMenuItem = new ApplicationBarMenuItem(LocalizedStrings.BrowseLibraryMenuItem);
            browseMenuItem.Click += new EventHandler(mnuBrowse_Click);
            ApplicationBar.MenuItems.Add(browseMenuItem);

            // "Search" menu item
            ApplicationBarMenuItem searchMenuItem = new ApplicationBarMenuItem(LocalizedStrings.SearchMenuItem);
            searchMenuItem.Click += new EventHandler(mnuSearch_Click);
            ApplicationBar.MenuItems.Add(searchMenuItem);

            AddChooseLibraryApplicationBarMenuItem();

            repeatShuffleControlDisplayTimer.Interval = TimeSpan.FromSeconds(5);
            repeatShuffleControlDisplayTimer.Tick += new EventHandler(repeatShuffleControlDisplayTimer_Tick);

            goBackTimer.Interval = TimeSpan.FromSeconds(5);
            goBackTimer.Tick += new EventHandler(goBackTimer_Tick);

            AirPlayMenuItem = new ApplicationBarMenuItem(LocalizedStrings.AirPlaySpeakersMenuItem);
            AirPlayMenuItem.Click += new EventHandler(AirPlayMenuItem_Click);
        }

        private readonly BitmapImage iconRepeat = new BitmapImage(new Uri("/icons/custom.appbar.repeat.png", UriKind.Relative));
        private readonly BitmapImage iconRepeatOne = new BitmapImage(new Uri("/icons/custom.appbar.repeatone.png", UriKind.Relative));

        private DispatcherTimer goBackTimer = new DispatcherTimer();
        private bool closing = false;

        #region Overrides

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            if (GoBackOnNextLoad)
            {
                GoBackOnNextLoad = false;
                NavigationService.GoBack();
            }

            base.OnNavigatedTo(e);
            HideRepeatShuffleControls(false);
            UpdateRepeatShuffleButtons();
            UpdateMediaKind();

            if (State.ContainsKey(StateUtils.SavedStateKey))
            {
                if (ShouldGoBack())
                {
                    closing = true;
                    AnimationContext = null;
                }
            }

            UpdateAirPlayButtons();

            ((PhoneApplicationFrame)App.Current.RootVisual).Obscured += new EventHandler<ObscuredEventArgs>(RootVisual_Obscured);
            ((PhoneApplicationFrame)App.Current.RootVisual).Unobscured += new EventHandler(RootVisual_Unobscured);
        }

        protected override void AnimationsComplete(Clarity.Phone.Controls.Animations.AnimationType animationType)
        {
            base.AnimationsComplete(animationType);

            if (closing)
                NavigationService.GoBack();
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            ((PhoneApplicationFrame)App.Current.RootVisual).Obscured -= new EventHandler<ObscuredEventArgs>(RootVisual_Obscured);
            ((PhoneApplicationFrame)App.Current.RootVisual).Unobscured -= new EventHandler(RootVisual_Unobscured);

            goBackTimer.Stop();

            base.OnNavigatedFrom(e);

            State[StateUtils.SavedStateKey] = true;
        }

        protected override void DACPServer_ServerUpdate(object sender, ServerUpdateEventArgs e)
        {
            base.DACPServer_ServerUpdate(sender, e);

            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                switch (e.Type)
                {
                    case ServerUpdateType.ServerConnected:
                    case ServerUpdateType.Error:
                        if (AirPlayDialog != null && AirPlayDialog.IsOpen)
                            AirPlayDialog.Hide();
                        AirPlayDialog = null;
                        AirPlaySpeakersControl = null;
                        break;
                    case ServerUpdateType.AirPlaySpeakerPassword:
                        MessageBox.Show("The remote speaker you selected requires a password.  Please enter it in iTunes.");
                        break;
                    case ServerUpdateType.LibraryError:
                        GoBackOnNextLoad = false;
                        if (NavigationService.CanGoBack)
                            NavigationService.GoBack();
                        break;
                    default:
                        break;
                }
            });
        }

        protected override void DACPServer_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            base.DACPServer_PropertyChanged(sender, e);

            switch (e.PropertyName)
            {
                case "ShuffleState":
                case "RepeatState":
                    UpdateRepeatShuffleButtons();
                    break;
                case "PlayState":
                case "CurrentSongName":
                    goBackTimer.Stop();
                    if (ShouldGoBack())
                        goBackTimer.Start();
                    break;
                case "CurrentMediaKind":
                    UpdateMediaKind();
                    break;
                default:
                    break;
            }

        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            if (AirPlayDialog != null && AirPlayDialog.IsOpen)
            {
                AirPlayDialog.Hide();
                e.Cancel = true;
                return;
            }

            base.OnBackKeyPress(e);
        }

        protected override void AttachServerEvents()
        {
            base.AttachServerEvents();

            if (DACPServer != null)
                DACPServer.AirPlaySpeakerUpdate += new EventHandler(DACPServer_AirPlaySpeakerUpdate);
        }

        protected override void DetachServerEvents()
        {
            base.DetachServerEvents();

            if (DACPServer != null)
                DACPServer.AirPlaySpeakerUpdate -= new EventHandler(DACPServer_AirPlaySpeakerUpdate);
        }

        #endregion

        #region Obscured/Unobscured

        protected bool _isObscured = false;

        void RootVisual_Obscured(object sender, ObscuredEventArgs e)
        {
            _isObscured = true;
        }

        void RootVisual_Unobscured(object sender, EventArgs e)
        {
            _isObscured = false;
            if (ShouldGoBack())
                goBackTimer.Start();
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
        }

        private void StartHideRepeatShuffleControlsTimer()
        {
            repeatShuffleControlDisplayTimer.Stop();
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

        private void btnArtist_Click(object sender, RoutedEventArgs e)
        {
            switch (SettingsManager.Current.ArtistClickAction)
            {
                case SettingsManager.ArtistClickActions.OpenArtistPage:
                    if (DACPServer.CurrentArtist == null)
                        return;
                    NavigationManager.OpenArtistPage(DACPServer.CurrentArtist);
                    break;

                case SettingsManager.ArtistClickActions.OpenAlbumPage:
                    if (DACPServer.CurrentArtist == null || DACPServer.CurrentAlbum == null || DACPServer.CurrentAlbumPersistentID == 0)
                        return;
                    NavigationManager.OpenAlbumPage(0, DACPServer.CurrentAlbum, DACPServer.CurrentArtist, DACPServer.CurrentAlbumPersistentID);
                    break;

                default:
                    break;
            }
        }

        private void bdrRepeatShuffleControls_ManipulationStarted(object sender, ManipulationStartedEventArgs e)
        {
            ShowRepeatShuffleControls();
        }

        private void bdrRepeatShuffleControls_ManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            StartHideRepeatShuffleControlsTimer();
        }

        private void Page_ManipulationStarted(object sender, ManipulationStartedEventArgs e)
        {
            // If the timer is running, the manipulation within the border has completed
            if (repeatShuffleControlDisplayTimer.IsEnabled)
                HideRepeatShuffleControls();
        }

        void AirPlayMenuItem_Click(object sender, EventArgs e)
        {
            ShowAirPlayDialog();
        }

        private void btnAirPlay_Click(object sender, RoutedEventArgs e)
        {
            ShowAirPlayDialog();
        }

        #endregion

        #region Methods

        protected bool ShouldGoBack()
        {
            return (DACPServer != null && DACPServer.IsConnected && DACPServer.PlayState == DACP.PlayStates.Stopped && DACPServer.CurrentSongName == null);
        }

        protected void ShowAirPlayDialog()
        {
            if (DACPServer == null || DACPServer.Speakers.Count <= 1)
                return;

            if (AirPlayDialog != null && AirPlayDialog.IsOpen)
                return;

            // Refresh the speaker data
            DACPServer.GetSpeakers();

            if (AirPlaySpeakersControl == null)
            {
                AirPlaySpeakersControl = new NowPlaying.AirPlaySpeakersControl(DACPServer);
                AirPlaySpeakersControl.SingleSpeakerClicked += new EventHandler(AirPlaySpeakersControl_SingleSpeakerClicked);
            }

            AirPlayDialog = new DialogService();
            AirPlayDialog.PopupContainer = MorePopup;
            AirPlayDialog.Child = AirPlaySpeakersControl;
            AirPlayDialog.AnimationType = DialogService.AnimationTypes.Slide;
            AirPlayDialog.Show(false);
        }

        protected void UpdateAirPlayButtons()
        {
            if (DACPServer == null)
                return;

            if (DACPServer.Speakers.Count > 1)
            {
                btnAirPlay.Visibility = System.Windows.Visibility.Visible;
                if (!ApplicationBar.MenuItems.Contains(AirPlayMenuItem))
                    ApplicationBar.MenuItems.Add(AirPlayMenuItem);
                    //ApplicationBar.MenuItems.Insert(ApplicationBar.MenuItems.Count - 2, AirPlayMenuItem);

                // It looks like the application bar doesn't recognize the Insert method so I'm just going to use the Add method for now.

                // Set button opacity
                bool airPlayEnabled = DACPServer.Speakers.Any(s => s.ID != 0 && s.Active);
                btnAirPlay.Opacity = (airPlayEnabled) ? 1.0 : 0.5;
            }
            else
            {
                btnAirPlay.Visibility = System.Windows.Visibility.Collapsed;
                if (ApplicationBar.MenuItems.Contains(AirPlayMenuItem))
                    ApplicationBar.MenuItems.Remove(AirPlayMenuItem);
            }

            UpdateMasterVolumeSlider();
        }

        protected void UpdateMasterVolumeSlider()
        {
            bool enableMasterVolume = true;

            if (DACPServer.IsCurrentlyPlayingVideo)
            {
                var mainSpeaker = DACPServer.Speakers.FirstOrDefault(s => s.ID == 0);
                if (mainSpeaker != null)
                    enableMasterVolume = mainSpeaker.Active;
            }

            MasterVolumeSlider.IsEnabled = enableMasterVolume;
        }

        protected void UpdateMediaKind()
        {
            if (DACPServer == null)
                return;

            btnArtist.IsEnabled = (DACPServer.CurrentMediaKind == 1);
            UpdateMasterVolumeSlider();
        }

        #endregion

        #region Event Handlers

        void goBackTimer_Tick(object sender, EventArgs e)
        {
            if (!_isObscured)
                NavigationService.GoBack();
        }

        void DACPServer_AirPlaySpeakerUpdate(object sender, EventArgs e)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                UpdateAirPlayButtons();
            });
        }
        
        void AirPlaySpeakersControl_SingleSpeakerClicked(object sender, EventArgs e)
        {
            if (AirPlayDialog != null && AirPlayDialog.IsOpen)
                AirPlayDialog.Hide();
        }

        #endregion
    }
}