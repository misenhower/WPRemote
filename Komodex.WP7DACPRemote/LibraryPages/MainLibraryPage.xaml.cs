﻿using System;
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
                    return GetListSelectorAnimation(lbArtists, animationType, toOrFrom);
                if (uri.Contains("AlbumPage"))
                    return GetListSelectorAnimation(lbAlbums, animationType, toOrFrom);
                if (uri.Contains("PlaylistPage"))
                    return GetListSelectorAnimation(lbPlaylists, animationType, toOrFrom);
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

            moreDialog = new DialogService();
            moreDialog.PopupContainer = MorePopup;
            moreDialog.Child = new LibraryViewDialog();
            moreDialog.AnimationType = DialogService.AnimationTypes.Slide;
            moreDialog.Show();
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

        #region Playlists

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

            return GetContinuumAnimation(LayoutRoot, animationType);
        }

        #endregion

        #region Group View Management

        private LongListSelector openedGroupViewSelector = null;

        private void LongListSelector_GroupViewOpened(object sender, GroupViewOpenedEventArgs e)
        {
            openedGroupViewSelector = (LongListSelector)sender;

            Utility.LongListSelectorGroupAnimationHelper((LongListSelector)sender, e);
        }

        private void LongListSelector_GroupViewClosing(object sender, GroupViewClosingEventArgs e)
        {
            openedGroupViewSelector = null;

            Utility.LongListSelectorGroupAnimationHelper((LongListSelector)sender, e);
        }

        #endregion

    }
}