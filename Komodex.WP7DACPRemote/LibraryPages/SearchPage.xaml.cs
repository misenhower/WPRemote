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

        protected DispatcherTimer searchTimer = new DispatcherTimer();

        #region Overrides

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            searchTimer.Stop();

            base.OnNavigatedFrom(e);
        }

        #endregion

        #region Actions

        private void tbSearchString_TextChanged(object sender, TextChangedEventArgs e)
        {
            searchTimer.Stop();
            DACPServer.ClearSearchResults();
            searchTimer.Start();
        }

        #endregion

        #region Event Handlers

        void searchTimer_Tick(object sender, EventArgs e)
        {
            searchTimer.Stop();

            DACPServer.GetSearchResults(tbSearchString.Text);
        }

        #endregion

    }
}