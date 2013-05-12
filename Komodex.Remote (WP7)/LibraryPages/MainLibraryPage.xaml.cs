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
using Komodex.Remote.Localization;
using Komodex.Common;

namespace Komodex.Remote.LibraryPages
{
    public partial class MainLibraryPage : RemoteBasePage
    {
        public MainLibraryPage()
        {
            InitializeComponent();

            // Application Bar Icons
            AddAppBarNowPlayingButton();
            AddApplicationBarIconButton(LocalizedStrings.SearchAppBarButton, "/icons/appbar.feature.search.rest.png", NavigationManager.OpenSearchPage);
            AddApplicationBarIconButton(LocalizedStrings.MoreAppBarButton, "/icons/custom.appbar.moredots.png", AppBarMoreButton_Click);

            // Shuffle All Songs
            AddApplicationBarMenuItem(LocalizedStrings.ShuffleAllSongsMenuItem, ShuffleAllSongs);

            // Choose Library
            AddApplicationBarMenuItem(LocalizedStrings.ChooseLibraryMenuItem, NavigationManager.OpenChooseLibraryPage);
        }

        #region Overrides

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            try
            {
                if (State.ContainsKey(StateUtils.SavedStateKey))
                {
                    this.RestoreState(pivotControl, 0);
                }
            }
            catch (InvalidOperationException) { }

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

            try
            {
                this.PreserveState(pivotControl);
                State[StateUtils.SavedStateKey] = true;
            }
            catch (InvalidOperationException) { }
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

        #endregion

        #region Event Handlers

        protected override void CurrentServer_ServerUpdate(object sender, ServerUpdateEventArgs e)
        {
            base.CurrentServer_ServerUpdate(sender, e);

            if (e.Type == ServerUpdateType.ServerConnected)
                Utility.BeginInvokeOnUIThread(GetDataForPivotItem);
        }

        #endregion

        #region Actions

        private void pivotControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GetDataForPivotItem();
        }

        void AppBarMoreButton_Click()
        {
            if (IsDialogOpen)
                return;

            ShowDialog(new LibraryViewDialog());
        }

        #region LongListSelector Tap Event

        private void LongListSelector_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            LongListSelector listBox = (LongListSelector)sender;
            var selectedItem = listBox.SelectedItem;

            DependencyObject originalSource = e.OriginalSource as DependencyObject;
            if (originalSource != null)
            {
                var ancestors = originalSource.GetVisualAncestors();
                bool isPlayButton = ancestors.Any(a => (a is FrameworkElement) && ((FrameworkElement)a).Name == "PlayButton");

                // Artists
                if (selectedItem is Artist)
                {
                    Artist artist = (Artist)selectedItem;
                    if (isPlayButton)
                    {
                        artist.SendPlayCommand();
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
                        album.SendPlayCommand();
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
                        genre.SendPlayCommand();
                        listBox.SelectedItem = null;
                        NavigationManager.OpenNowPlayingPage();
                    }
                    else
                    {
                        NavigationManager.OpenGenrePage(genre.Name);
                    }
                }
                // Genius Mixes
                else if (ancestors.Any(a => (a is FrameworkElement) && ((FrameworkElement)a).Name == "GeniusButton"))
                {
                    NavigationManager.OpenGeniusMixesPage();
                }
                // Playlists
                else if (selectedItem is Playlist)
                {
                    Playlist playlist = (Playlist)selectedItem;
                    if (playlist.ItemCount <= 0)
                        return;

                    if (isPlayButton)
                    {
                        playlist.SendPlayCommand();
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

        private void ShuffleAllSongs()
        {
            if (CurrentServer != null && CurrentServer.IsConnected)
            {
                CurrentServer.SendShuffleAllSongsCommand();
                NavigationManager.OpenNowPlayingPage();
            }
        }

        #endregion

        #endregion

        #region Methods

        private void GetDataForPivotItem()
        {
            if (CurrentServer == null || !CurrentServer.IsConnected)
                return;

            if (pivotControl.SelectedItem == pivotArtists)
            {
                if (CurrentServer.LibraryArtists == null || CurrentServer.LibraryArtists.Count == 0)
                    CurrentServer.GetArtists();
            }
            else if (pivotControl.SelectedItem == pivotAlbums)
            {
                if (CurrentServer.LibraryAlbums == null || CurrentServer.LibraryAlbums.Count == 0)
                    CurrentServer.GetAlbums();
            }
            else if (pivotControl.SelectedItem == pivotGenres)
            {
                if (CurrentServer.LibraryGenres == null || CurrentServer.LibraryGenres.Count == 0)
                    CurrentServer.GetGenres();
            }
        }

        #endregion

    }
}