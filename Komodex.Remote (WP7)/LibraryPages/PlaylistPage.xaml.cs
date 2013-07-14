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
using Komodex.DACP.Library;
using Clarity.Phone.Controls.Animations;
using Clarity.Phone.Extensions;
using Komodex.Remote.ServerManagement;
using Komodex.Common;
using Komodex.DACP;

namespace Komodex.Remote.LibraryPages
{
    public partial class PlaylistPage : RemoteBasePage
    {
        public PlaylistPage()
        {
            InitializeComponent();

            //InitializeStandardAppNavApplicationBar();
            //AddChooseLibraryApplicationBarMenuItem();

#if WP7
            lbSongs.Link += lbSongs_Link;
#endif
        }

        #region Properties

        private Playlist Playlist
        {
            get { return LayoutRoot.DataContext as Playlist; }
            set { LayoutRoot.DataContext = value; }
        }

        #endregion

        #region Overrides

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (Playlist == null)
            {
                var queryString = NavigationContext.QueryString;

                string playlistIDString = queryString["id"];
                string playlistName = queryString["name"];
                string playlistPersistentIDString = queryString["perid"];

                if (string.IsNullOrEmpty(playlistIDString) || string.IsNullOrEmpty(playlistName) || string.IsNullOrEmpty(playlistPersistentIDString))
                {
                    NavigationService.GoBack();
                    return;
                }

                int playlistID;
                UInt64 playlistPersistentID;

                if (!int.TryParse(playlistIDString, out playlistID) || !UInt64.TryParse(playlistPersistentIDString, out playlistPersistentID))
                {
                    NavigationService.GoBack();
                    return;
                }

                Playlist = new Playlist(ServerManager.CurrentServer, playlistID, playlistName, playlistPersistentID);
                if (Playlist.Server != null && Playlist.Server.IsConnected)
                    Playlist.GetSongs();
            }
        }

        protected override AnimatorHelperBase GetAnimation(AnimationType animationType, Uri toOrFrom)
        {
            if (toOrFrom != null)
            {
                string uri = toOrFrom.OriginalString;

                if (uri.Contains("MainLibraryPage"))
                    return GetContinuumAnimation(PageTitle, animationType);
            }

            return base.GetAnimation(animationType, toOrFrom);
        }

        protected override void CurrentServer_ServerUpdate(object sender, DACP.ServerUpdateEventArgs e)
        {
            base.CurrentServer_ServerUpdate(sender, e);

            if (e.Type == DACP.ServerUpdateType.ServerConnected)
                Utility.BeginInvokeOnUIThread(Playlist.GetSongs);
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
                bool tiltSuppressed = e.ContentPresenter.Content is Playlist;
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

                if (ancestors.Any(a => (a is FrameworkElement) && ((FrameworkElement)a).Name == "ShuffleButton"))
                {
                    Playlist.SendShuffleSongsCommand();
                    NavigationManager.OpenNowPlayingPage();
                }
                else if (selectedItem is MediaItem)
                {
                    MediaItem song = (MediaItem)selectedItem;
                    Playlist.SendPlaySongCommand(song);
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

                Playlist.SendPlaySongCommand(song, mode);
                NavigationManager.OpenNowPlayingPage();
            }
        }

        #endregion
    }
}