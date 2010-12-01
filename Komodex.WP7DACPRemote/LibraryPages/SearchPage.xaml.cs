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

namespace Komodex.WP7DACPRemote.LibraryPages
{
    public partial class SearchPage : DACPServerBoundPhoneApplicationPage
    {
        public SearchPage()
        {
            InitializeComponent();

            AnimationContext = LayoutRoot;

            searchTimer.Interval = TimeSpan.FromMilliseconds(100);
            searchTimer.Tick += new EventHandler(searchTimer_Tick);
        }

        protected DispatcherTimer searchTimer = new DispatcherTimer();

        #region Overrides

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            searchTimer.Stop();

            base.OnNavigatedFrom(e);
        }

        protected override AnimatorHelperBase GetAnimation(AnimationType animationType, Uri toOrFrom)
        {
            if (toOrFrom != null)
            {
                string uri = toOrFrom.OriginalString;

                if (animationType == AnimationType.NavigateForwardOut || animationType == AnimationType.NavigateBackwardIn)
                {
                    if (uri.Contains("AlbumPage"))
                        return GetListSelectorAnimation(lbSearchResults, animationType, toOrFrom);
                    if (uri.Contains("ArtistPage"))
                        return GetListSelectorAnimation(lbSearchResults, animationType, toOrFrom);
                }
            }

            return base.GetAnimation(animationType, toOrFrom);
        }

        #endregion

        #region Actions

        private void tbSearchString_TextChanged(object sender, TextChangedEventArgs e)
        {
            searchTimer.Stop();
            lbSearchResults.ItemsSource = null;
            searchTimer.Start();
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
            else if (selectedItem is Song)
            {

            }
        }

        #endregion

        #region Event Handlers

        void searchTimer_Tick(object sender, EventArgs e)
        {
            searchTimer.Stop();

            lbSearchResults.ItemsSource = DACPServer.GetSearchResults(tbSearchString.Text); ;
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