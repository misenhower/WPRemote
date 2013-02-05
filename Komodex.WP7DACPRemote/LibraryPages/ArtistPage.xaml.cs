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
using Clarity.Phone.Extensions;

namespace Komodex.WP7DACPRemote.LibraryPages
{
    public partial class ArtistPage : DACPServerBoundPhoneApplicationPage
    {
        public ArtistPage()
        {
            InitializeComponent();

            AnimationContext = LayoutRoot;

            //InitializeStandardAppNavApplicationBar();
            //AddChooseLibraryApplicationBarMenuItem();
        }

        #region Properties

        private Artist Artist
        {
            get { return LayoutRoot.DataContext as Artist; }
            set { LayoutRoot.DataContext = value; }
        }

        #endregion

        #region Overrides

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            string artistName = NavigationContext.QueryString["name"];

            if (State.ContainsKey(StateUtils.SavedStateKey))
            {
                this.RestoreState(pivotControl, 0);
            }

            if (Artist == null)
                Artist = new Artist(DACPServerManager.Server, artistName);
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

                if (uri.Contains("AlbumPage"))
                {
                    if (animationType == AnimationType.NavigateForwardOut || animationType == AnimationType.NavigateBackwardIn)
                        return this.GetListSelectorAnimation(lbAlbums, animationType);
                    else
                        return GetContinuumAnimation(LayoutRoot, animationType);
                }
                if (uri.Contains("MainLibraryPage"))
                    return GetContinuumAnimation(LayoutRoot, animationType);
                if (uri.Contains("SearchPage") || uri.Contains("GenrePage"))
                {
                    if (animationType == AnimationType.NavigateForwardIn || animationType == AnimationType.NavigateBackwardOut)
                        return GetContinuumAnimation(LayoutRoot, animationType);
                }
            }

            return base.GetAnimation(animationType, toOrFrom);
        }

        #endregion

        #region Event Handlers

        private void lbSongs_Link(object sender, LinkUnlinkEventArgs e)
        {
            // Disable tilt effect on shuffle row
            var listBoxItem = e.ContentPresenter.GetVisualAncestors().FirstOrDefault(a => a is ListBoxItem);
            if (listBoxItem != null)
            {
                bool tiltSuppressed = e.ContentPresenter.Content is Artist;
                TiltEffect.SetSuppressTilt(listBoxItem, tiltSuppressed);
            }
        }

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

                // Albums
                if (selectedItem is Album)
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
                
                // Shuffle button
                else if (ancestors.Any(a => (a is FrameworkElement) && ((FrameworkElement)a).Name == "ShuffleButton"))
                {
                    Artist.SendShuffleSongsCommand();
                    NavigationManager.OpenNowPlayingPage();
                }

                // Songs
                else if (selectedItem is MediaItem)
                {
                    MediaItem song = (MediaItem)selectedItem;
                    Artist.SendPlaySongCommand(song);
                    listBox.SelectedItem = null;
                    NavigationManager.OpenNowPlayingPage();
                }
            }
        }

        private void Pivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GetDataForPivotItem();
        }

        #endregion

        #region Methods

        private void GetDataForPivotItem()
        {
            if (Artist == null || Artist.Server == null || !Artist.Server.IsConnected)
                return;

            if (pivotControl.SelectedItem == pivotAlbums)
            {
                if (Artist.Albums == null || Artist.Albums.Count == 0)
                    Artist.GetAlbums();
            }
            else if (pivotControl.SelectedItem == pivotSongs)
            {
                if (Artist.Songs == null || Artist.Songs.Count == 0)
                    Artist.GetSongs();
            }
        }

        #endregion
    }
}