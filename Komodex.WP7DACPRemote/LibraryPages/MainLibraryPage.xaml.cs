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
using Microsoft.Phone.Shell;
using Clarity.Phone.Extensions;

namespace Komodex.WP7DACPRemote.LibraryPages
{
    public partial class MainLibraryPage : DACPServerBoundPhoneApplicationPage
    {
        public MainLibraryPage()
        {
            InitializeComponent();

            AnimationContext = LayoutRoot;

            // Application Bar
            InitializeAppBar();
            InitializeStandardAppNavApplicationBar(true, false, true);
            AddChooseLibraryApplicationBarMenuItem();

            // "More" Appbar button
            var AppBarMoreButton = new ApplicationBarIconButton(new Uri("/icons/custom.appbar.moredots.png", UriKind.Relative));
            AppBarMoreButton.Text = "more";
            AppBarMoreButton.Click += new EventHandler(AppBarMoreButton_Click);
            ApplicationBar.Buttons.Add(AppBarMoreButton);
        }

        DialogService moreDialog = null;

        #region Overrides

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (State.ContainsKey(StateUtils.SavedStateKey))
            {
                this.RestoreState(pivotControl, 0);
            }

            GetDataForPivotItem();
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            // Album scroll retention
            //object lbAlbumsFirstItem = lbAlbums.GetItemsInView().FirstOrDefault();
            //if (lbAlbumsFirstItem is Album)
            //{
            //    Album lbAlbumsAlbum = (Album)lbAlbumsFirstItem;
            //    State[lbAlbums.Name + "_FirstItem"] = lbAlbumsAlbum.PersistentID;
            //}
            //else if (lbAlbumsFirstItem is GroupItems<Album>)
            //{
            //    GroupItems<Album> lbAlbumsGroup = (GroupItems<Album>)lbAlbumsFirstItem;
            //    State[lbAlbums.Name + "_FirstItem"] = lbAlbumsGroup.Key;
            //}

            this.PreserveState(pivotControl);
            State[StateUtils.SavedStateKey] = true;
        }

        protected override AnimatorHelperBase GetAnimation(AnimationType animationType, Uri toOrFrom)
        {
            if (toOrFrom != null)
            {
                string uri = toOrFrom.OriginalString;

                if (uri.Contains("ArtistPage"))
                    return this.GetListSelectorAnimation(lbArtists, animationType);
                if (uri.Contains("AlbumPage"))
                    return this.GetListSelectorAnimation(lbAlbums, animationType);
                if (uri.Contains("PlaylistPage"))
                    return this.GetListSelectorAnimation(lbPlaylists, animationType);
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
                pivotControl.IsEnabled = true;
                return;
            }

            if (moreDialog != null && moreDialog.IsOpen)
            {
                moreDialog.Hide();
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

        private void pivotControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GetDataForPivotItem();
        }

        #region Artists

        private void ArtistButton_Click(object sender, RoutedEventArgs e)
        {
            Artist artist = ((Button)sender).Tag as Artist;

            if (artist != null)
            {
                lbArtists.SelectedItem = artist;
                NavigationManager.OpenArtistPage(artist.Name);
            }
        }

        private void ArtistPlayButton_Click(object sender, RoutedEventArgs e)
        {
            Artist artist = ((Button)sender).Tag as Artist;

            if (artist != null)
            {
                artist.SendPlaySongCommand();
                NavigationManager.OpenNowPlayingPage();
            }
        }

        void AppBarMoreButton_Click(object sender, EventArgs e)
        {
            if (moreDialog != null)
                moreDialog.Hide();

            // Disable all the listboxes because of a z-order issue with the group headers
            lbArtists.IsEnabled = false;
            lbAlbums.IsEnabled = false;
            lbPlaylists.IsEnabled = false;

            moreDialog = new DialogService();
            moreDialog.PopupContainer = MorePopup;
            moreDialog.Child = new LibraryViewDialog();
            moreDialog.AnimationType = DialogService.AnimationTypes.Slide;
            moreDialog.Closed += new EventHandler(moreDialog_Closed);
            moreDialog.Show();
        }

        void moreDialog_Closed(object sender, EventArgs e)
        {
            lbArtists.IsEnabled = true;
            lbAlbums.IsEnabled = true;
            lbPlaylists.IsEnabled = true;
        }

        #endregion

        #region Albums

        private void AlbumButton_Click(object sender, RoutedEventArgs e)
        {
            Album album = ((Button)sender).Tag as Album;

            if (album != null)
            {
                lbAlbums.SelectedItem = album;
                NavigationManager.OpenAlbumPage(album.ID, album.Name, album.ArtistName, album.PersistentID);
            }
        }

        private void AlbumPlayButton_Click(object sender, RoutedEventArgs e)
        {
            Album album = ((Button)sender).Tag as Album;

            if (album != null)
            {
                album.SendPlaySongCommand();
                NavigationManager.OpenNowPlayingPage();
            }
        }

        #endregion

        #region Genres

        private void GenreButton_Click(object sender, RoutedEventArgs e)
        {
            Genre genre = ((Button)sender).Tag as Genre;

            if (genre != null)
            {
                lbGenres.SelectedItem = genre;
                NavigationManager.OpenGenrePage(genre.Name);
            }
        }

        private void GenrePlayButton_Click(object sender, RoutedEventArgs e)
        {
            Genre genre = ((Button)sender).Tag as Genre;

            if (genre != null)
            {
                genre.SendPlaySongCommand();
                NavigationManager.OpenNowPlayingPage();
            }
        }

        #endregion

        #region Playlists

        private void GeniusMixesButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationManager.OpenGeniusMixesPage();
        }

        private void PlaylistButton_Click(object sender, RoutedEventArgs e)
        {
            Playlist playlist = ((Button)sender).Tag as Playlist;

            if (playlist != null)
            {
                if (playlist.ItemCount <= 0)
                    return;

                lbPlaylists.SelectedItem = playlist;
                NavigationManager.OpenPlaylistPage(playlist.ID, playlist.Name, playlist.PersistentID);
            }
        }

        private void PlaylistPlayButton_Click(object sender, RoutedEventArgs e)
        {
            Playlist playlist = ((Button)sender).Tag as Playlist;

            if (playlist != null)
            {
                if (playlist.ItemCount <= 0)
                    return;

                playlist.SendPlaySongCommand();
                NavigationManager.OpenNowPlayingPage();
            }
        }

        #endregion

        #region Shuffle All Songs

        private void mnuShuffle_Click(object sender, EventArgs e)
        {
            if (DACPServer != null && DACPServer.IsConnected)
            {
                DACPServer.SendShuffleAllSongsCommand();
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
            else if (pivotControl.SelectedItem == pivotGenres)
            {
                if (DACPServer.LibraryGenres == null || DACPServer.LibraryGenres.Count == 0)
                    DACPServer.GetGenres();
            }
        }

        #endregion

        #region Group View Management

        private LongListSelector openedGroupViewSelector = null;

        private void LongListSelector_GroupViewOpened(object sender, GroupViewOpenedEventArgs e)
        {
            pivotControl.IsEnabled = false;
            openedGroupViewSelector = (LongListSelector)sender;
        }

        private void LongListSelector_GroupViewClosing(object sender, GroupViewClosingEventArgs e)
        {
            openedGroupViewSelector = null;
            pivotControl.IsEnabled = true;
        }

        #endregion

    }
}