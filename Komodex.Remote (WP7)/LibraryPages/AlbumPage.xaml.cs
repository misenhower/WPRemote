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
using Clarity.Phone.Controls;
using Clarity.Phone.Controls.Animations;
using Clarity.Phone.Extensions;
using Komodex.Remote.ServerManagement;
using Komodex.Common;
using Komodex.DACP;

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

        protected FrameworkElement btnArtist = null;

        #region Properties

        private Album Album
        {
            get { return LayoutRoot.DataContext as Album; }
            set { LayoutRoot.DataContext = value; }
        }

        #endregion

        #region Overrides

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            var queryString = NavigationContext.QueryString;

            int albumID = int.Parse(queryString["id"]);
            UInt64 albumPersistentID = UInt64.Parse(queryString["perid"]);
            string albumName = queryString["name"];
            string artistName = queryString["artist"];

            if (Album == null)
            {
                Album = new Album(ServerManager.CurrentServer, albumID, albumName, artistName, albumPersistentID);
                if (Album.Server != null && Album.Server.IsConnected)
                    Album.GetSongs();
            }
        }

        protected override void CurrentServer_ServerUpdate(object sender, DACP.ServerUpdateEventArgs e)
        {
            base.CurrentServer_ServerUpdate(sender, e);

            if (e.Type == DACP.ServerUpdateType.ServerConnected)
                Utility.BeginInvokeOnUIThread(() => { Album.GetSongs(); });
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
                    if (uri.Contains("ArtistPage") && btnArtist != null)
                        return GetContinuumAnimation(btnArtist, animationType);
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

        private void LongListSelector_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            LongListSelector listBox = (LongListSelector)sender;
            var selectedItem = listBox.SelectedItem;

            DependencyObject originalSource = e.OriginalSource as DependencyObject;
            if (originalSource != null)
            {
                var ancestors = originalSource.GetVisualAncestors();
                bool isPlayButton = ancestors.Any(a => (a is FrameworkElement) && ((FrameworkElement)a).Name == "PlayButton");

                // Album play button
                if (ancestors.Any(a => (a is FrameworkElement) && ((FrameworkElement)a).Name == "AlbumPlayButton"))
                {
                    Album.SendPlayCommand();
                    NavigationManager.OpenNowPlayingPage();
                }

                // Artist button
                else if (ancestors.Any(a => (a is FrameworkElement) && ((FrameworkElement)a).Name == "ArtistButton"))
                {
                    btnArtist = originalSource as FrameworkElement;
                    NavigationManager.OpenArtistPage(Album.ArtistName);
                }

                // Shuffle button
                else if (ancestors.Any(a => (a is FrameworkElement) && ((FrameworkElement)a).Name == "ShuffleButton"))
                {
                    Album.SendShuffleSongsCommand();
                    NavigationManager.OpenNowPlayingPage();
                }

                // Songs
                else if (selectedItem is MediaItem)
                {
                    MediaItem song = (MediaItem)selectedItem;

                    Album.SendPlaySongCommand(song);
                    NavigationManager.OpenNowPlayingPage();
                }

            }
        }

        private void PlayQueueButton_Click(object sender, RoutedEventArgs e)
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
            if (menuItem.DataContext is MediaItem)
            {
                MediaItem song = (MediaItem)menuItem.DataContext;

                Album.SendPlaySongCommand(song, mode);
                NavigationManager.OpenNowPlayingPage();
            }

            // Album
            else if (menuItem.DataContext is Album)
            {
                Album.SendPlayCommand(mode);
                NavigationManager.OpenNowPlayingPage();
            }
        }

        #endregion

        #region Methods

        #endregion
    }
}