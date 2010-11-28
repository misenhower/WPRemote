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
        }

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

        #endregion

        #region Actions

        private void btnNowPlaying_Click(object sender, EventArgs e)
        {
            NavigationManager.OpenNowPlayingPage();
        }

        private void mnuSettings_Click(object sender, EventArgs e)
        {
            NavigationManager.OpenLibraryChooserPage();
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