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

        #region Overrides

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            HidePlayControls(false);
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

        #endregion

        #region Actions

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