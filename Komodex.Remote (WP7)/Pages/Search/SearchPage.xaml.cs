using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Komodex.DACP.Search;
using System.Windows.Input;
using System.Threading;
using System.Windows.Threading;
using Komodex.Common.Phone.Controls;
using Komodex.DACP;
using Komodex.DACP.Groups;
using Komodex.DACP.Items;
using Clarity.Phone.Controls.Animations;

namespace Komodex.Remote.Pages.Search
{
    public partial class SearchPage : RemoteBasePage
    {
#if WP7
        private readonly TimeSpan SearchDelay = TimeSpan.FromMilliseconds(500);
#else
        private readonly TimeSpan SearchDelay = TimeSpan.FromMilliseconds(300);
#endif

        public SearchPage()
        {
            InitializeComponent();

            Loaded += SearchPage_Loaded;

            _beginSearchTimer = new DispatcherTimer();
            _beginSearchTimer.Interval = SearchDelay;
            _beginSearchTimer.Tick += BeginSearchTimer_Tick;
        }


        private void SearchPage_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= SearchPage_Loaded;
            SearchTextBox.Focus();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _beginSearchTimer.Stop();
            _beginSearchTimer.Start();

        }

        private void SearchTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                SearchResultsListBox.Focus();
        }

        protected override AnimatorHelperBase GetAnimation(AnimationType animationType, Uri toOrFrom)
        {
            if (toOrFrom.OriginalString.StartsWith("/Pages/Browse/"))
            {
                if (animationType == AnimationType.NavigateForwardOut || animationType == AnimationType.NavigateBackwardIn)
                {
                    if (_lastTappedItem != null)
                    {
                        var contentPresenter = SearchResultsListBox.GetContentPresenterForItem(_lastTappedItem);
                        if (contentPresenter != null)
                            return GetContinuumAnimation(contentPresenter, animationType);
                    }
                }
            }

            return base.GetAnimation(animationType, toOrFrom);
        }

        #region CurrentSearchResults Property

        public static readonly DependencyProperty CurrentSearchResultsProperty =
            DependencyProperty.Register("CurrentSearchResults", typeof(DACPSearchResults), typeof(SearchPage), new PropertyMetadata(null));

        public DACPSearchResults CurrentSearchResults
        {
            get { return (DACPSearchResults)GetValue(CurrentSearchResultsProperty); }
            set { SetValue(CurrentSearchResultsProperty, value); }
        }

        #endregion

        #region Search Results

        private readonly DispatcherTimer _beginSearchTimer;
        private CancellationTokenSource _cancellationTokenSource;

        private void BeginSearchTimer_Tick(object sender, EventArgs e)
        {
            _beginSearchTimer.Stop();
            StartNewSearch(SearchTextBox.Text.Trim());
        }

        protected async void StartNewSearch(string searchString)
        {
            // Stop the previous search requests
            if (_cancellationTokenSource != null)
                _cancellationTokenSource.Cancel();

            if (string.IsNullOrEmpty(searchString))
            {
                CurrentSearchResults = null;
                ClearProgressIndicator();
                return;
            }

            // Begin a new search
            var results = new DACPSearchResults(CurrentServer.MainDatabase, searchString);
            SetProgressIndicator(null, true);
            _cancellationTokenSource = new CancellationTokenSource();
            CurrentSearchResults = results;
            try { await CurrentSearchResults.SearchAsync(_cancellationTokenSource.Token); }
            catch { }
            if (results == CurrentSearchResults)
                ClearProgressIndicator();
        }

        #endregion

        #region List Actions

        private object _lastTappedItem;

        private void SearchResultsListBox_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            LongListSelectorEx list = (LongListSelectorEx)sender;
            var selectedItem = list.SelectedItem as DACPElement;

            // Clear out the selected item
            list.SelectedItem = null;

            // Make sure a DACPElement was selected
            if (selectedItem == null)
                return;

            _lastTappedItem = selectedItem;

            if (selectedItem is Album)
            {
                NavigationManager.OpenAlbumPage((Album)selectedItem);
                return;
            }
            if (selectedItem is Artist)
            {
                NavigationManager.OpenArtistPage((Artist)selectedItem);
                return;
            }
            if (selectedItem is Song)
            {
                // TODO
            }
        }

        #endregion

    }
}