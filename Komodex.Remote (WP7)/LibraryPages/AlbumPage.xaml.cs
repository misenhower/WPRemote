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
    public partial class AlbumPage : RemoteBasePage
    {
        public AlbumPage()
        {
            InitializeComponent();

            //InitializeStandardAppNavApplicationBar();
            //AddChooseLibraryApplicationBarMenuItem();

#if WP7
            lbSongs.Link += lbSongs_Link;
#endif
        }

        protected FrameworkElement _artistButton = null;

        #region Album Management

        protected bool _initialized;
        protected int _databaseID;
        protected int _containerID;
        protected int _albumID;

        public static readonly DependencyProperty AlbumProperty =
            DependencyProperty.Register("Album", typeof(Album), typeof(AlbumPage), new PropertyMetadata(null));

        public Album Album
        {
            get { return (Album)GetValue(AlbumProperty); }
            set { SetValue(AlbumProperty, value); }
        }

        protected async void UpdateAlbum()
        {
            if (!_initialized)
                return;

            if (CurrentServer == null || !CurrentServer.IsConnected)
                return;

            if (Album == null)
            {
                // TODO: Alternate databases
                // Get the music container
                var musicContainer = CurrentServer.MainDatabase.Music;

                // Get the album
                SetProgressIndicator(null, true);
                Album = await musicContainer.GetAlbumByID(_albumID);
                if (Album == null)
                {
                    NavigationService.GoBack();
                    return;
                }
            }

            // Load the album's songs
            if (Album.Songs == null)
            {
                SetProgressIndicator(null, true);
                await Album.RequestSongsAsync();
            }

            ClearProgressIndicator();
        }

        #endregion

        #region Overrides

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            var queryString = NavigationContext.QueryString;

            _databaseID = int.Parse(queryString["database"]);
            _containerID = int.Parse(queryString["container"]);
            _albumID = int.Parse(queryString["album"]);
            _initialized = true;

            UpdateAlbum();
        }

        protected override void CurrentServer_ServerUpdate(object sender, DACP.ServerUpdateEventArgs e)
        {
            base.CurrentServer_ServerUpdate(sender, e);

            Utility.BeginInvokeOnUIThread(UpdateAlbum);
        }

        protected override void OnServerChanged()
        {
            base.OnServerChanged();

            Utility.BeginInvokeOnUIThread(UpdateAlbum);
        }

        protected override AnimatorHelperBase GetAnimation(AnimationType animationType, Uri toOrFrom)
        {
            if (toOrFrom != null)
            {
                string uri = toOrFrom.OriginalString;

                if (animationType == AnimationType.NavigateForwardIn || animationType == AnimationType.NavigateBackwardOut)
                {
                    if (uri.Contains("MainLibraryPage") || uri.Contains("ArtistPage") || uri.Contains("SearchPage") || uri.Contains("GenrePage"))
                        return GetContinuumAnimation(LayoutRoot, animationType);
                }
                else if (animationType == AnimationType.NavigateForwardOut || animationType == AnimationType.NavigateBackwardIn)
                {
                    if (uri.Contains("ArtistPage") && _artistButton != null)
                        return GetContinuumAnimation(_artistButton, animationType);
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
                bool tiltSuppressed = e.ContentPresenter.Content is Album;
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

                // Album play button
                if (ancestors.AnyElementsWithName("AlbumPlayButton"))
                {
                    if (await Album.Play())
                        NavigationManager.OpenNowPlayingPage();
                    else
                        RemoteUtility.ShowLibraryError();
                }

                // Artist button
                else if (ancestors.AnyElementsWithName("ArtistButton"))
                {
                    _artistButton = originalSource as FrameworkElement;
                    
                    // TODO: Alternate databases
                    // Get the music container
                    var musicContainer = CurrentServer.MainDatabase.Music;

                    if (musicContainer.Artists == null)
                    {
                        SetProgressIndicator(null, true);
                        bool success = await musicContainer.RequestArtistsAsync();
                        ClearProgressIndicator();
                        if (!success)
                            return;
                    }

                    // Find the artist
                    var artist = CurrentServer.MainDatabase.Music.Artists.Values.FirstOrDefault(a => a.Name == Album.ArtistName);
                    if (artist != null)
                        NavigationManager.OpenArtistPage(artist);
                }

                // Shuffle button
                else if (ancestors.AnyElementsWithName("ShuffleButton"))
                {
                    if (await Album.Shuffle())
                        NavigationManager.OpenNowPlayingPage();
                    else
                        RemoteUtility.ShowLibraryError();
                }

                // Songs
                else if (selectedItem is Song)
                {
                    Song song = (Song)selectedItem;

                    if (await Album.PlaySong(song))
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

            // Songs
            if (menuItem.DataContext is Song)
            {
                Song song = (Song)menuItem.DataContext;

                if (await Album.PlaySong(song, mode))
                    NavigationManager.OpenNowPlayingPage();
                else
                    RemoteUtility.ShowLibraryError();
            }

            // Album
            else if (menuItem.DataContext is Album)
            {
                if (await Album.Play(mode))
                    NavigationManager.OpenNowPlayingPage();
                else
                    RemoteUtility.ShowLibraryError();
            }
        }

        #endregion
    }
}