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
    public partial class AlbumPage : AnimatedBasePage
    {
        public AlbumPage()
        {
            InitializeComponent();

            AnimationContext = LayoutRoot;
        }

        #region Properties

        private Album Album
        {
            get { return DataContext as Album; }
            set
            {
                if (Album != null)
                {

                }

                DataContext = value;

                if (Album != null)
                {

                }
            }
        }

        #endregion

        #region Overrides

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (Album == null)
            {
                var queryString = NavigationContext.QueryString;

                string albumIDString = queryString["id"];
                string albumName = queryString["name"];
                string artistName = queryString["artist"];
                string albumPersistentIDString = queryString["perid"];

                if (string.IsNullOrEmpty(albumName) || string.IsNullOrEmpty(artistName) || string.IsNullOrEmpty(albumIDString))
                {
                    NavigationService.GoBack();
                    return;
                }

                int albumID;
                UInt64 albumPersistentID;

                if (!int.TryParse(albumIDString, out albumID) || !UInt64.TryParse(albumPersistentIDString, out albumPersistentID))
                {
                    NavigationService.GoBack();
                    return;
                }

                Album = new Album(DACPServerManager.Server, albumID, albumName, artistName, albumPersistentID);
                if (Album.Server != null && Album.Server.IsConnected)
                    Album.GetSongs();
            }
        }

        protected override AnimatorHelperBase GetAnimation(AnimationType animationType, Uri toOrFrom)
        {
            if (toOrFrom != null)
            {
                string uri = toOrFrom.OriginalString;

                if (uri.Contains("MainLibraryPage") || uri.Contains("ArtistPage"))
                    return GetContinuumAnimation(LayoutRoot, animationType);
            }

            return base.GetAnimation(animationType, toOrFrom);
        }

        #endregion

        #region Actions

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LongListSelector listBox = (LongListSelector)sender;

            Song song = listBox.SelectedItem as Song;

            if (song != null)
            {
                Album.SendPlaySongCommand(song);
                NavigationManager.OpenNowPlayingPage();
            }

            listBox.SelectedItem = null;
        }

        #endregion

        #region Methods

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

            return base.GetAnimation(animationType, toOrFrom);
        }

        #endregion
    }
}