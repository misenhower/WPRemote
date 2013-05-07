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
using Clarity.Phone.Controls;
using Clarity.Phone.Controls.Animations;
using Clarity.Phone.Extensions;
using Komodex.Remote.ServerManagement;

namespace Komodex.Remote.LibraryPages
{
    public partial class GenrePage : DACPServerBoundPhoneApplicationPage
    {
        public GenrePage()
        {
            InitializeComponent();

            //InitializeStandardAppNavApplicationBar();
            //AddChooseLibraryApplicationBarMenuItem();

#if WP7
            lbSongs.Link += lbSongs_Link;
#endif
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

            try
            {
                if (State.ContainsKey(StateUtils.SavedStateKey))
                {
                    this.RestoreState(pivotControl, 0);
                }
            }
            catch (InvalidOperationException) { }

            if (Genre == null)
                Genre = new Genre(ServerManager.CurrentServer, genreName);
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            try
            {
                this.PreserveState(pivotControl);
                State[StateUtils.SavedStateKey] = true;
            }
            catch (InvalidOperationException) { }
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

        #endregion

        #region Event Handlers

#if WP7
        private void lbSongs_Link(object sender, LinkUnlinkEventArgs e)
        {
            // Disable tilt effect on header row
            var listBoxItem = e.ContentPresenter.GetVisualAncestors().FirstOrDefault(a => a is ListBoxItem);
            if (listBoxItem != null)
            {
                bool tiltSuppressed = e.ContentPresenter.Content is Genre;
                TiltEffect.SetSuppressTilt(listBoxItem, tiltSuppressed);
            }
        }
#endif

        #endregion

        #region Actions

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
                // Shuffle songs button
                else if (ancestors.Any(a => (a is FrameworkElement) && ((FrameworkElement)a).Name == "ShuffleButton"))
                {
                    Genre.SendShuffleSongsCommand();
                    NavigationManager.OpenNowPlayingPage();
                }
                // Songs
                else if (selectedItem is MediaItem)
                {
                    MediaItem song = (MediaItem)selectedItem;
                    Genre.SendPlaySongCommand(song);
                    NavigationManager.OpenNowPlayingPage();
                }

            }
        }

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

    }
}