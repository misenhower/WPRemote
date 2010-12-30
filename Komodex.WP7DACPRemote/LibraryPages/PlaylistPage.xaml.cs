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
using Komodex.WP7DACPRemote.DACPServerManagement;
using Clarity.Phone.Controls.Animations;

namespace Komodex.WP7DACPRemote.LibraryPages
{
    public partial class PlaylistPage : DACPServerBoundPhoneApplicationPage
    {
        public PlaylistPage()
        {
            InitializeComponent();

            AnimationContext = LayoutRoot;

            //InitializeStandardAppNavApplicationBar();
            //AddChooseLibraryApplicationBarMenuItem();
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

                Playlist = new Playlist(DACPServerManager.Server, playlistID, playlistName, playlistPersistentID);
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

        #endregion

        #region Actions

        private void lbSongs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MediaItem song = lbSongs.SelectedItem as MediaItem;

            if (song != null)
            {
                Playlist.SendPlaySongCommand(song);
                NavigationManager.OpenNowPlayingPage();
            }

            lbSongs.SelectedItem = null;
        }

        #endregion
    }
}