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
using Komodex.DACP.Library;
using Komodex.DACP;
using Clarity.Phone.Controls.Animations;

namespace Komodex.WP7DACPRemote.LibraryPages
{
    public partial class MainLibraryPage : DACPServerBoundPhoneApplicationPage
    {
        public MainLibraryPage()
        {
            InitializeComponent();

            AnimationContext = LayoutRoot;
        }

        #region Overrides

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            GetDataForPivotItem();
        }

        protected override AnimatorHelperBase GetAnimation(AnimationType animationType, Uri toOrFrom)
        {
            if (toOrFrom != null)
            {
                string uri = toOrFrom.OriginalString;

                if (uri.Contains("ArtistPage"))
                    return GetListSelectorAnimation(lbArtists, animationType, toOrFrom);
                if (uri.Contains("AlbumPage"))
                    return GetListSelectorAnimation(lbAlbums, animationType, toOrFrom);
            }

            return base.GetAnimation(animationType, toOrFrom);
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            if (openedGroupViewSelector != null)
            {
                openedGroupViewSelector.CloseGroupView();
                openedGroupViewSelector = null;
                e.Cancel = true;
                return;
            }

            base.OnBackKeyPress(e);
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

        private void btnSearch_Click(object sender, EventArgs e)
        {
            NavigationManager.OpenSearchPage();
        }

        private void mnuSettings_Click(object sender, EventArgs e)
        {
            NavigationManager.OpenLibraryChooserPage();
        }

        private void pivotControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GetDataForPivotItem();
        }

        #region Artists

        private bool ignoreNextArtistSelection = false;

        private void lbArtists_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ignoreNextArtistSelection)
            {
                ignoreNextArtistSelection = false;
                lbArtists.SelectedItem = null;
                return;
            }

            Artist artist = lbArtists.SelectedItem as Artist;

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
                NavigationManager.OpenNowPlayingPage();
            }
        }

        #endregion

        #region Albums

        private bool ignoreNextAlbumSelection = false;

        private void lbAlbums_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ignoreNextAlbumSelection)
            {
                ignoreNextAlbumSelection = false;
                lbAlbums.SelectedItem = null;
                return;
            }

            Album album = lbAlbums.SelectedItem as Album;

            if (album != null)
            {
                NavigationManager.OpenAlbumPage(album.ID, album.Name, album.ArtistName, album.PersistentID);
            }
        }

        private void AlbumPlayButton_Click(object sender, RoutedEventArgs e)
        {
            ignoreNextAlbumSelection = true;

            Button button = (Button)sender;

            Album album = button.Tag as Album;

            if (album != null)
            {
                album.SendPlaySongCommand();
                NavigationManager.OpenNowPlayingPage();
            }
        }

        #endregion

        #region Playlists

        private bool ignoreNextPlaylistSelection = false;

        private void lbPlaylists_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ignoreNextPlaylistSelection)
            {
                ignoreNextPlaylistSelection = false;
                lbPlaylists.SelectedItem = null;
                return;
            }

            Playlist playlist = lbPlaylists.SelectedItem as Playlist;

            if (playlist != null)
                NavigationManager.OpenPlaylistPage(playlist.ID, playlist.Name, playlist.PersistentID);
        }

        private void PlaylistPlayButton_Click(object sender, RoutedEventArgs e)
        {
            ignoreNextPlaylistSelection = true;

            Button button = (Button)sender;

            Playlist playlist = button.Tag as Playlist;

            if (playlist != null)
            {
                playlist.SendPlaySongCommand();
                NavigationManager.OpenNowPlayingPage();
            }
        }


        #endregion

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

        private AnimatorHelperBase GetListSelectorAnimation(LongListSelector listSelector, AnimationType animationType, Uri toOrFrom)
        {
            if (listSelector.SelectedItem != null)
            {
                var contentPresenters = listSelector.GetItemsWithContainers(true, true).Cast<ContentPresenter>();
                var contentPresenter = contentPresenters.FirstOrDefault(cp => cp.Content == listSelector.SelectedItem);

                if (animationType == AnimationType.NavigateBackwardIn)
                    listSelector.SelectedItem = null;

                if (contentPresenter != null)
                {
                    return GetContinuumAnimation(contentPresenter, animationType);
                }
            }

            return base.GetAnimation(animationType, toOrFrom);
        }
        
        #endregion

        #region Group View Management

        private LongListSelector openedGroupViewSelector = null;

        private void LongListSelector_GroupViewOpened(object sender, GroupViewOpenedEventArgs e)
        {
            openedGroupViewSelector = (LongListSelector)sender;
        }

        private void LongListSelector_GroupViewClosing(object sender, GroupViewClosingEventArgs e)
        {
            openedGroupViewSelector = null;
        }

        #endregion

    }
}