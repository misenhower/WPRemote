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
using Komodex.WP7DACPRemote.DACPServerManagement;
using Clarity.Phone.Controls;
using Clarity.Phone.Controls.Animations;

namespace Komodex.WP7DACPRemote.LibraryPages
{
    public partial class GenrePage : DACPServerBoundPhoneApplicationPage
    {
        public GenrePage()
        {
            InitializeComponent();

            AnimationContext = LayoutRoot;

            //InitializeStandardAppNavApplicationBar();
            //AddChooseLibraryApplicationBarMenuItem();
        }

        #region Properties

        private Genre Genre
        {
            get { return LayoutRoot.DataContext as Genre; }
            set { LayoutRoot.DataContext = value; }
        }

        #endregion

        #region Overrides

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            string genreName = NavigationContext.QueryString["name"];

            if (State.ContainsKey(StateUtils.SavedStateKey))
            {
                this.RestoreState(pivotControl, 0);
            }

            if (Genre == null)
                Genre = new Genre(DACPServerManager.Server, genreName);
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            this.PreserveState(pivotControl);
            State[StateUtils.SavedStateKey] = true;
        }

        protected override void DACPServer_ServerUpdate(object sender, DACP.ServerUpdateEventArgs e)
        {
            base.DACPServer_ServerUpdate(sender, e);

            if (e.Type == DACP.ServerUpdateType.ServerConnected)
                Deployment.Current.Dispatcher.BeginInvoke(() => { GetDataForPivotItem(); });
        }

        protected override AnimatorHelperBase GetAnimation(AnimationType animationType, Uri toOrFrom)
        {
            if (toOrFrom != null)
            {
                string uri = toOrFrom.OriginalString;

                if (uri.Contains("ArtistPage"))
                {
                    if (animationType == AnimationType.NavigateForwardOut || animationType == AnimationType.NavigateBackwardIn)
                        return this.GetListSelectorAnimation(lbArtists, animationType);
                    else
                        return GetContinuumAnimation(LayoutRoot, animationType);
                }
                if (uri.Contains("AlbumPage"))
                {
                    if (animationType == AnimationType.NavigateForwardOut || animationType == AnimationType.NavigateBackwardIn)
                        return this.GetListSelectorAnimation(lbAlbums, animationType);
                    else
                        return GetContinuumAnimation(LayoutRoot, animationType);
                }
                if (uri.Contains("MainLibraryPage"))
                    return GetContinuumAnimation(LayoutRoot, animationType);
                if (uri.Contains("SearchPage"))
                {
                    if (animationType == AnimationType.NavigateForwardIn || animationType == AnimationType.NavigateBackwardOut)
                        return GetContinuumAnimation(LayoutRoot, animationType);
                }
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

            base.OnBackKeyPress(e);
        }

        #endregion

        #region Actions

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

        #region Songs

        private void SongPlayButton_Click(object sender, RoutedEventArgs e)
        {
            MediaItem song = ((Button)sender).Tag as MediaItem;

            if (song != null)
            {
                Genre.SendPlaySongCommand(song);
                NavigationManager.OpenNowPlayingPage();
            }
        }

        #endregion

        private void Pivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GetDataForPivotItem();
        }

        private void ShuffleButton_Click(object sender, RoutedEventArgs e)
        {
            Genre.SendShuffleSongsCommand();
            NavigationManager.OpenNowPlayingPage();
        }

        #endregion

        #region Methods

        private void GetDataForPivotItem()
        {
            if (Genre == null || Genre.Server == null || !Genre.Server.IsConnected)
                return;

            if (pivotControl.SelectedItem == pivotArtists)
            {
                if (Genre.Artists == null || Genre.Artists.Count == 0)
                    Genre.GetArtists();
            }
            else if (pivotControl.SelectedItem == pivotAlbums)
            {
                if (Genre.Albums == null || Genre.Albums.Count == 0)
                    Genre.GetAlbums();
            }
            else if (pivotControl.SelectedItem == pivotSongs)
            {
                if (Genre.Songs == null || Genre.Songs.Count == 0)
                    Genre.GetSongs();
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