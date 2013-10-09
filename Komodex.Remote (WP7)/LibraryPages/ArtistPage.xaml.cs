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
using Clarity.Phone.Controls;
using Clarity.Phone.Controls.Animations;
using Clarity.Phone.Extensions;
using Komodex.Remote.ServerManagement;
using Komodex.Common;
using Komodex.DACP;
using Komodex.DACP.Groups;
using Komodex.DACP.Items;

namespace Komodex.Remote.LibraryPages
{
    public partial class ArtistPage : RemoteBasePage
    {
        public ArtistPage()
        {
            InitializeComponent();

            //InitializeStandardAppNavApplicationBar();
            //AddChooseLibraryApplicationBarMenuItem();

#if WP7
            lbSongs.Link += lbSongs_Link;
#endif
        }

        #region Artist Management

        protected bool _initialized;
        protected int _databaseID;
        protected int _containerID;
        protected int _artistID;

        public static readonly DependencyProperty ArtistProperty =
            DependencyProperty.Register("Artist", typeof(Artist), typeof(ArtistPage), new PropertyMetadata(null));

        public Artist Artist
        {
            get { return (Artist)GetValue(ArtistProperty); }
            set { SetValue(ArtistProperty, value); }
        }

        protected async void UpdateArtist()
        {
            if (!_initialized)
                return;

            if (CurrentServer == null || !CurrentServer.IsConnected)
                return;

            if (Artist == null)
            {
                // TODO: Alternate databases
                // Get the music container
                var musicContainer = CurrentServer.MainDatabase.MusicContainer;

                // Get the artist
                SetProgressIndicator(null, true);
                Artist = await musicContainer.GetArtistByID(_artistID);
                ClearProgressIndicator();
                if (Artist == null)
                {
                    NavigationService.GoBack();
                    return;
                }
            }

            // Load the artist's albums or songs
            GetDataForPivotItem();
        }

        #endregion

        #region Overrides

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            var queryString = NavigationContext.QueryString;

            _databaseID = int.Parse(queryString["database"]);
            _containerID = int.Parse(queryString["container"]);
            _artistID = int.Parse(queryString["artist"]);
            _initialized = true;

            try
            {
                if (State.ContainsKey(StateUtils.SavedStateKey))
                {
                    this.RestoreState(pivotControl, 0);
                }
            }
            catch (InvalidOperationException) { }

            UpdateArtist();
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

        protected override void CurrentServer_ServerUpdate(object sender, DACP.ServerUpdateEventArgs e)
        {
            base.CurrentServer_ServerUpdate(sender, e);

            Utility.BeginInvokeOnUIThread(UpdateArtist);
        }

        protected override void OnServerChanged()
        {
            base.OnServerChanged();

            Utility.BeginInvokeOnUIThread(UpdateArtist);
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

#if WP7
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
#endif

        #endregion

        #region Actions

        private async void LongListSelector_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            LongListSelector listBox = (LongListSelector)sender;
            var selectedItem = listBox.SelectedItem;

            DependencyObject originalSource = e.OriginalSource as DependencyObject;
            if (originalSource != null)
            {
                var ancestors = originalSource.GetVisualAncestors().ToList();
                bool isPlayButton = ancestors.AnyElementsWithName("PlayButton");

                // Albums
                if (selectedItem is Album)
                {
                    Album album = (Album)selectedItem;
                    if (isPlayButton)
                    {
                        listBox.SelectedItem = null;
                        if (await album.Play())
                            NavigationManager.OpenNowPlayingPage();
                        else
                            RemoteUtility.ShowLibraryError();
                    }
                    else
                    {
                        NavigationManager.OpenAlbumPage(album);
                    }
                }

                // Shuffle button
                else if (ancestors.AnyElementsWithName("ShuffleButton"))
                {
                    if (await Artist.Shuffle())
                        NavigationManager.OpenNowPlayingPage();
                    else
                        RemoteUtility.ShowLibraryError();
                }

                // Songs
                else if (selectedItem is Song)
                {
                    Song song = (Song)selectedItem;
                    listBox.SelectedItem = null;
                    if (await Artist.PlaySong(song))
                        NavigationManager.OpenNowPlayingPage();
                    else
                        RemoteUtility.ShowLibraryError();
                }
            }
        }

        private async void PlayQueueButton_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)sender;

            PlayQueueMode mode;
            switch (menuItem.Name)
            {
                case "PlayNextButton": mode = PlayQueueMode.PlayNext; break;
                case "AddToUpNextButton": mode = PlayQueueMode.AddToQueue; break;
                default: return;
            }

            // Albums
            if (menuItem.DataContext is Album)
            {
                Album album = (Album)menuItem.DataContext;

                if (await album.Play(mode))
                    NavigationManager.OpenNowPlayingPage();
                else
                    RemoteUtility.ShowLibraryError();
            }

            // Songs
            else if (menuItem.DataContext is Song)
            {
                Song song = (Song)menuItem.DataContext;

                if (await Artist.PlaySong(song, mode))
                    NavigationManager.OpenNowPlayingPage();
                else
                    RemoteUtility.ShowLibraryError();
            }
        }

        private void Pivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GetDataForPivotItem();
        }

        #endregion

        #region Methods

        private async void GetDataForPivotItem()
        {
            if (CurrentServer == null || !CurrentServer.IsConnected)
                return;

            if (Artist == null)
                return;

            if (pivotControl.SelectedItem == pivotAlbums)
            {
                if (Artist.Albums == null)
                {
                    SetProgressIndicator(null, true);
                    await Artist.RequestAlbumsAsync();
                    ClearProgressIndicator();
                }
            }
            else if (pivotControl.SelectedItem == pivotSongs)
            {
                if (Artist.Songs == null)
                {
                    SetProgressIndicator(null, true);
                    await Artist.RequestSongsAsync();
                    ClearProgressIndicator();
                }
            }
        }

        #endregion
    }
}