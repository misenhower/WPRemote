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
using System.Windows.Threading;
using Komodex.DACP.Library;
using Clarity.Phone.Controls.Animations;
using Komodex.DACP;
using System.Collections.ObjectModel;
using Komodex.Common;

namespace Komodex.Remote.LibraryPages
{
    public partial class SearchPage : RemoteBasePage
    {
        public SearchPage()
        {
            InitializeComponent();

            searchTimer.Interval = TimeSpan.FromMilliseconds(500);
            searchTimer.Tick += searchTimer_Tick;

        }

        private void SearchPage_Loaded(object sender, RoutedEventArgs e)
        {
            tbSearchString.Focus();
        }

        protected DispatcherTimer searchTimer = new DispatcherTimer();

        #region Overrides

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            searchTimer.Stop();

            base.OnNavigatedFrom(e);

            try
            {
                this.PreserveState(tbSearchString);
                State[StateUtils.SavedStateKey] = true;
            }
            catch (InvalidOperationException) { }
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            try
            {
                if (State.ContainsKey(StateUtils.SavedStateKey))
                {
                    this.RestoreState(tbSearchString, string.Empty);
                }
            }
            catch (InvalidOperationException) { }
        }

        protected override AnimatorHelperBase GetAnimation(AnimationType animationType, Uri toOrFrom)
        {
            if (toOrFrom != null)
            {
                string uri = toOrFrom.OriginalString;

                if (animationType == AnimationType.NavigateForwardOut || animationType == AnimationType.NavigateBackwardIn)
                {
                    if (uri.Contains("AlbumPage") || uri.Contains("ArtistPage") || uri.Contains("PodcastPage"))
                        return this.GetListSelectorAnimation(lbSearchResults, animationType);
                }
            }

            return base.GetAnimation(animationType, toOrFrom);
        }

        protected override void CurrentServer_ServerUpdate(object sender, ServerUpdateEventArgs e)
        {
            base.CurrentServer_ServerUpdate(sender, e);

            if (e.Type == DACP.ServerUpdateType.ServerConnected)
                Utility.BeginInvokeOnUIThread(StartSearch);
        }

        #endregion

        #region Actions

        private void tbSearchString_TextChanged(object sender, TextChangedEventArgs e)
        {
            searchTimer.Stop();
            if (CurrentServer != null && CurrentServer.IsConnected)
            {
                CurrentServer.StopSearch();
                lbSearchResults.ItemsSource = null;
                searchTimer.Start();
            }
        }

        private void tbSearchString_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                lbSearchResults.Focus();
        }

        private void LongListSelector_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            LongListSelector listBox = (LongListSelector)sender;

            object selectedItem = listBox.SelectedItem;

            if (selectedItem is Album)
            {
                Album album = (Album)selectedItem;
                NavigationManager.OpenAlbumPage(album.ID, album.Name, album.ArtistName, album.PersistentID);
            }
            else if (selectedItem is Artist)
            {
                Artist artist = (Artist)selectedItem;
                NavigationManager.OpenArtistPage(artist.Name);
            }
            else if (selectedItem is Podcast)
            {
                Podcast podcast = (Podcast)selectedItem;
                NavigationManager.OpenPodcastPage(podcast.ID, podcast.Name, podcast.PersistentID);
            }
            else if (selectedItem is MediaItem)
            {
                MediaItem mediaItem = (MediaItem)selectedItem;

                // Find the SearchResultSet object that contains this MediaItem
                var searchResults = (ObservableCollection<SearchResultSet>)listBox.ItemsSource;
                SearchResultSet resultSet = searchResults.FirstOrDefault(rs => rs.Contains(mediaItem));
                if (resultSet != null)
                {
                    if (resultSet.Type == SearchResultsType.Songs)
                    {
                        if (CurrentServer.SupportsPlayQueue)
                            mediaItem.SendPlayQueueCommand();
                        else
                            resultSet.SendPlayItemCommand(mediaItem);
                    }
                    else
                        mediaItem.SendPlayMediaItemCommand();
                    
                    listBox.SelectedItem = null;
                    NavigationManager.OpenNowPlayingPage();
                }
            }
        }

        #endregion

        #region Event Handlers

        void searchTimer_Tick(object sender, EventArgs e)
        {
            searchTimer.Stop();

            StartSearch();
        }

        #endregion

        #region Methods

        private void StartSearch()
        {
            if (CurrentServer != null && CurrentServer.IsConnected)
                lbSearchResults.ItemsSource = CurrentServer.GetSearchResults(tbSearchString.Text);
        }

        #endregion

    }
}