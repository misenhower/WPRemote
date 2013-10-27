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

namespace Komodex.Remote.Pages.Search
{
    public partial class SearchPage : RemoteBasePage
    {
        public SearchPage()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty CurrentSearchResultsProperty =
            DependencyProperty.Register("CurrentSearchResults", typeof(DACPSearchResults), typeof(SearchPage), new PropertyMetadata(null));

        public DACPSearchResults CurrentSearchResults
        {
            get { return (DACPSearchResults)GetValue(CurrentSearchResultsProperty); }
            set { SetValue(CurrentSearchResultsProperty, value); }
        }

        private async void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            CurrentSearchResults = new DACPSearchResults(CurrentServer.MainDatabase, SearchTextBox.Text.Trim());
            SetProgressIndicator(null, true);
            await CurrentSearchResults.SearchAsync(CancellationToken.None);
            ClearProgressIndicator();
        }

        private void SearchTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                SearchResultsListBox.Focus();
        }
    }
}