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
using Komodex.DACP;
using Komodex.WP7DACPRemote.DACPServerInfoManagement;
using Microsoft.Phone.Shell;
using Komodex.WP7DACPRemote.DACPServerManagement;
using Komodex.DACP.Library;

namespace Komodex.WP7DACPRemote
{
    public partial class MainPage : DACPServerBoundPhoneApplicationPage
    {
        public MainPage()
        {
            InitializeComponent();

            // ApplicationBar buttons and menu items must be referenced at run time
            btnPlayPause = (ApplicationBarIconButton)ApplicationBar.Buttons[1];
        }

        private ApplicationBarIconButton btnPlayPause = null;
        private readonly Uri iconPlay = new Uri("/icons/appbar.transport.play.rest.png", UriKind.Relative);
        private readonly Uri iconPause = new Uri("/icons/appbar.transport.pause.rest.png", UriKind.Relative);

        #region Overrides

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            UpdatePlayPauseButton();
        }

        protected override void DACPServer_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            base.DACPServer_PropertyChanged(sender, e);

            if (e.PropertyName == "PlayState")
                UpdatePlayPauseButton();
        }

        #endregion

        #region Methods

        private void UpdatePlayPauseButton()
        {
            if (DACPServer.PlayState == PlayStates.Playing)
                btnPlayPause.IconUri = iconPause;
            else
                btnPlayPause.IconUri = iconPlay;
        }

        #endregion

        #region Actions

        private void btnNowPlaying_Click(object sender, RoutedEventArgs e)
        {
            NavigationManager.OpenNowPlayingPage();
        }

        private void btnLibrary_Click(object sender, RoutedEventArgs e)
        {
            NavigationManager.OpenMainLibraryPage();
        }

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            NavigationManager.OpenLibraryChooserPage();
        }

        private void mnuSettings_Click(object sender, EventArgs e)
        {
            NavigationManager.OpenLibraryChooserPage();
        }

        private void btnPrevious_Click(object sender, EventArgs e)
        {
            DACPServer.SendPrevItemCommand();
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            DACPServer.SendNextItemCommand();
        }

        private void btnPlayPause_Click(object sender, EventArgs e)
        {
            DACPServer.SendPlayPauseCommand();
        }

        #endregion

    }
}