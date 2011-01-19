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

namespace Komodex.WP7DACPRemote.LibraryPages
{
    public partial class SearchPage : DACPServerBoundPhoneApplicationPage
    {
        public SearchPage()
        {
            InitializeComponent();

            AnimationContext = LayoutRoot;

            searchTimer.Interval = TimeSpan.FromMilliseconds(250);
            searchTimer.Tick += new EventHandler(searchTimer_Tick);
        }

        private void DACPServerBoundPhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            tbSearchString.Focus();
        }

        protected DispatcherTimer searchTimer = new DispatcherTimer();

        #region Overrides

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            searchTimer.Stop();

            base.OnNavigatedFrom(e);

            this.PreserveState(tbSearchString);
            State[StateUtils.SavedStateKey] = true;
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (State.ContainsKey(StateUtils.SavedStateKey))
            {
                this.RestoreState(tbSearchString, string.Empty);
            }
        }

        protected override AnimatorHelperBase GetAnimation(AnimationType animationType, Uri toOrFrom)
        {
            if (toOrFrom != null)
            {
                string uri = toOrFrom.OriginalString;

                if (animationType == AnimationType.NavigateForwardOut || animationType == AnimationType.NavigateBackwardIn)
                {
                    if (uri.Contains("AlbumPage") || uri.Contains("ArtistPage") || uri.Contains("PodcastPage"))
                        return GetListSelectorAnimation(lbSearchResults, animationType, toOrFrom);
                }
            }

            return base.GetAnimation(animationType, toOrFrom);
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            if (openedGroupViewSelector != null)
            {
                openedGroupViewSelector.CloseGroupView();
                openedGroupViewSelector = null;
                e.Cancel = true;
                return;
            }

            base.OnBackKeyPress(e);
        }

        protected override void DACPServer_ServerUpdate(object sender, ServerUpdateEventArgs e)
        {
            base.DACPServer_ServerUpdate(sender, e);

            if (e.Type == DACP.ServerUpdateType.ServerConnected)
                Deployment.Current.Dispatcher.BeginInvoke(() => { StartSearch(); });
        }

        #endregion

        #region Actions

        private void tbSearchString_TextChanged(object sender, TextChangedEventArgs e)
        {
            searchTimer.Stop();
            if (DACPServer != null && DACPServer.IsConnected)
            {
                DACPServer.StopSearch();
                lbSearchResults.ItemsSource = null;
                searchTimer.Start();
            }
        }

        private void tbSearchString_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                lbSearchResults.Focus();
        }

        private void lbSearchResults_SelectionChanged(object sender, SelectionChangedEventArgs e)
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
                    resultSet.SendPlayItemCommand(mediaItem);
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
            if (DACPServer != null && DACPServer.IsConnected)
                lbSearchResults.ItemsSource = DACPServer.GetSearchResults(tbSearchString.Text);
        }

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

            return GetContinuumAnimation(LayoutRoot, animationType);
        }

        #endregion

        #region Group View Management

        private LongListSelector openedGroupViewSelector = null;

        private void LongListSelector_GroupViewOpened(object sender, GroupViewOpenedEventArgs e)
        {
            openedGroupViewSelector = (LongListSelector)sender;

            Utility.LongListSelectorGroupAnimationHelper((LongListSelector)sender, e);
        }

        private void LongListSelector_GroupViewClosing(object sender, GroupViewClosingEventArgs e)
        {
            openedGroupViewSelector = null;

            Utility.LongListSelectorGroupAnimationHelper((LongListSelector)sender, e);
        }

        #endregion

    }
}