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

            // ApplicationBar button and menu item references must be referenced at run time
            btnPlayPause = (ApplicationBarIconButton)ApplicationBar.Buttons[1];
        }

        DACPServerViewModel viewModel = DACPServerViewModel.Instance;
        private ApplicationBarIconButton btnPlayPause = null;
        private readonly Uri iconPlay = new Uri("/icons/appbar.transport.play.rest.png", UriKind.Relative);
        private readonly Uri iconPause = new Uri("/icons/appbar.transport.pause.rest.png", UriKind.Relative);


        #region Overrides

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            GetDataForPivotItem();
        }

        #endregion

        #region Event Handlers

        protected override void DACPServer_ServerUpdate(object sender, ServerUpdateEventArgs e)
        {
            base.DACPServer_ServerUpdate(sender, e);

            if (e.Type == ServerUpdateType.ServerConnected)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    GetDataForPivotItem();
                });
            }
        }

        protected override void DACPServer_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            base.DACPServer_PropertyChanged(sender, e);

            switch (e.PropertyName)
            {
                case "PlayState":
                    UpdatePlayPauseButton();
                    break;
                default:
                    break;
            }
        }

        private void UpdatePlayPauseButton()
        {
            if (DACPServer.PlayState == PlayStates.Playing)
                btnPlayPause.IconUri = iconPause;
            else
                btnPlayPause.IconUri = iconPlay;
        }

        #endregion

        #region Actions

        private void connectingStatusControl_ButtonClick(object sender, RoutedEventArgs e)
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

        private void pivotControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GetDataForPivotItem();
        }

        private bool ignoreNextArtistSelection = false;

        private void lbArtists_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ignoreNextArtistSelection)
            {
                ignoreNextArtistSelection = false;
                return;
            }

            LongListSelector listBox = (LongListSelector)sender;

            Artist artist = listBox.SelectedItem as Artist;

            if (artist != null)
            {
                NavigationManager.OpenArtistPage(artist.Name);
            }
        }

        private void ArtistPlayButton_Click(object sender, RoutedEventArgs e)
        {
            ignoreNextArtistSelection = true;

            Button button = (Button)sender;

            Artist artist = button.Tag as Artist;

            if (artist != null)
            {
                artist.SendPlaySongCommand();
                pivotControl.SelectedIndex = 0;
            }
        }

        private void lbAlbums_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LongListSelector listBox = (LongListSelector)sender;

            Album album = listBox.SelectedItem as Album;

            if (album != null)
            {
                NavigationManager.OpenAlbumPage(album.ID, album.Name, album.ArtistName, album.PersistentID);
            }
        }


        #endregion

        #region Methods

        private void GetDataForPivotItem()
        {
            if (DACPServer == null || !DACPServer.IsConnected)
                return;

            if (pivotControl.SelectedItem == pivotArtists)
            {
                if (DACPServer.LibraryArtists == null || DACPServer.LibraryArtists.Count == 0)
                    DACPServer.GetArtists();
            }
            else if (pivotControl.SelectedItem == pivotAlbums)
            {
                if (DACPServer.LibraryAlbums == null || DACPServer.LibraryAlbums.Count == 0)
                    DACPServer.GetAlbums();
            }
        }

        #endregion

    }
}