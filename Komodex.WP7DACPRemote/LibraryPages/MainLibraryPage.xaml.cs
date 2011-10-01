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
using Komodex.WP7DACPRemote.Localization;

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

            // "Shuffle all songs" menu item
            ApplicationBarMenuItem shuffleMenuItem = new ApplicationBarMenuItem(LocalizedStrings.ShuffleAllSongsMenuItem);
            shuffleMenuItem.Click += new EventHandler(mnuShuffle_Click);
            ApplicationBar.MenuItems.Add(shuffleMenuItem);

            AddChooseLibraryApplicationBarMenuItem();

            // "More" Appbar button
            var AppBarMoreButton = new ApplicationBarIconButton(new Uri("/icons/custom.appbar.moredots.png", UriKind.Relative));
            AppBarMoreButton.Text = LocalizedStrings.MoreAppBarButton;
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
                if (uri.Contains("GenrePage"))
                    return this.GetListSelectorAnimation(lbGenres, animationType);
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

        #region LongListSelector Tap Event

        private void LongListSelector_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            LongListSelector listBox = (LongListSelector)sender;
            var selectedItem = listBox.SelectedItem;

            DependencyObject originalSource = e.OriginalSource as DependencyObject;
            if (originalSource != null)
            {
                bool isPlayButton = originalSource.GetVisualAncestors().Any(a => (a is FrameworkElement) && ((FrameworkElement)a).Name == "PlayButton");

                // Artists
                if (selectedItem is Artist)
                {
                    Artist artist = (Artist)selectedItem;
                    if (isPlayButton)
                    {
                        artist.SendPlaySongCommand();
                        listBox.SelectedItem = null;
                        NavigationManager.OpenNowPlayingPage();
                    }
                    else
                    {
                        NavigationManager.OpenArtistPage(artist.Name);
                    }
                }
                // Albums
                else if (selectedItem is Album)
                {
                    Album album = (Album)selectedItem;
                    if (isPlayButton)
                    {
                        album.SendPlaySongCommand();
                        listBox.SelectedItem = null;
                        NavigationManager.OpenNowPlayingPage();
                    }
                    else
                    {
                        NavigationManager.OpenAlbumPage(album.ID, album.Name, album.ArtistName, album.PersistentID);
                    }
                }
                // Genres
                else if (selectedItem is Genre)
                {
                    Genre genre = (Genre)selectedItem;
                    if (isPlayButton)
                    {
                        genre.SendPlaySongCommand();
                        listBox.SelectedItem = null;
                        NavigationManager.OpenNowPlayingPage();
                    }
                    else
                    {
                        NavigationManager.OpenGenrePage(genre.Name);
                    }
                }
                // Playlists
                else if (selectedItem is Playlist)
                {
                    Playlist playlist = (Playlist)selectedItem;
                    if (playlist.ItemCount <= 0)
                        return;

                    if (isPlayButton)
                    {
                        playlist.SendPlaySongCommand();
                        listBox.SelectedItem = null;
                        NavigationManager.OpenNowPlayingPage();
                    }
                    else
                    {
                        NavigationManager.OpenPlaylistPage(playlist.ID, playlist.Name, playlist.PersistentID);
                    }
                }
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