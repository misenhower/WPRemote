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

namespace Komodex.WP7DACPRemote.LibraryPages
{
    public partial class PodcastPage : DACPServerBoundPhoneApplicationPage
    {
        public PodcastPage()
        {
            InitializeComponent();

            AnimationContext = LayoutRoot;
        }

        #region Properties

        private Podcast Podcast
        {
            get { return LayoutRoot.DataContext as Podcast; }
            set { LayoutRoot.DataContext = value; }
        }

        #endregion

        #region Overrides

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            var queryString = NavigationContext.QueryString;

            int podcastID = int.Parse(queryString["id"]);
            UInt64 podcastPersistentID = UInt64.Parse(queryString["perid"]);
            string podcastName = queryString["name"];

            if (Podcast == null)
            {
                Podcast = new Podcast(DACPServerManager.Server, podcastID, podcastName, podcastPersistentID);
                if (Podcast.Server != null && Podcast.Server.IsConnected)
                    Podcast.GetEpisodes();
            }
        }

        #endregion

        #region Actions

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        #endregion

    }

}