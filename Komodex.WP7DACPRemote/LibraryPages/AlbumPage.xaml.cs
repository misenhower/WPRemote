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
using Komodex.WP7DACPRemote.DACPServerManagement;
using Clarity.Phone.Controls;
using Clarity.Phone.Controls.Animations;

namespace Komodex.WP7DACPRemote.LibraryPages
{
    public partial class AlbumPage : DACPServerBoundPhoneApplicationPage
    {
        public AlbumPage()
        {
            InitializeComponent();

            AnimationContext = LayoutRoot;

            //InitializeStandardAppNavApplicationBar();
            //AddChooseLibraryApplicationBarMenuItem();
        }

        protected Button btnArtist = null;

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
                Album = new Album(DACPServerManager.Server, albumID, albumName, artistName, albumPersistentID);
                if (Album.Server != null && Album.Server.IsConnected)
                    Album.GetSongs();
            }
        }

        protected override void DACPServer_ServerUpdate(object sender, DACP.ServerUpdateEventArgs e)
        {
            base.DACPServer_ServerUpdate(sender, e);

            if (e.Type == DACP.ServerUpdateType.ServerConnected)
                Deployment.Current.Dispatcher.BeginInvoke(() => { Album.GetSongs(); });
        }

        protected override AnimatorHelperBase GetAnimation(AnimationType animationType, Uri toOrFrom)
        {
            if (toOrFrom != null)
            {
                string uri = toOrFrom.OriginalString;

                if (animationType == AnimationType.NavigateForwardIn || animationType == AnimationType.NavigateBackwardOut)
                {
                    if (uri.Contains("MainLibraryPage") || uri.Contains("ArtistPage") || uri.Contains("SearchPage"))
                        return GetContinuumAnimation(LayoutRoot, animationType);
                }
                else if (animationType == AnimationType.NavigateForwardOut || animationType == AnimationType.NavigateBackwardIn)
                {
                    if (uri.Contains("ArtistPage"))
                        return GetContinuumAnimation(btnArtist, animationType);
                }
                
            }

            return base.GetAnimation(animationType, toOrFrom);
        }

        #endregion

        #region Actions

        private void btnArtist_Click(object sender, RoutedEventArgs e)
        {
            btnArtist = sender as Button;
            NavigationManager.OpenArtistPage(Album.ArtistName);
        }

        private void SongPlayButton_Click(object sender, RoutedEventArgs e)
        {
            MediaItem song = ((Button)sender).Tag as MediaItem;

            if (song != null)
            {
                Album.SendPlaySongCommand(song);
                NavigationManager.OpenNowPlayingPage();
            }
        }

        private void AlbumPlayButton_Click(object sender, RoutedEventArgs e)
        {
            Album.SendPlaySongCommand();
            NavigationManager.OpenNowPlayingPage();
        }

        private void ShuffleButton_Click(object sender, RoutedEventArgs e)
        {
            Album.SendShuffleSongsCommand();
            NavigationManager.OpenNowPlayingPage();
        }

        #endregion

        #region Methods

        #endregion
    }
}