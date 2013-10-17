using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Shell;
using Komodex.DACP.Containers;
using Komodex.DACP.Databases;
using Komodex.DACP;
using Komodex.Common.Phone.Controls;
using Komodex.DACP.Groups;

namespace Komodex.Remote.Pages.Browse.Podcasts
{
    public partial class PodcastsPage : BrowsePodcastsContainerBasePage
    {
        public PodcastsPage()
        {
            InitializeComponent();

            SetPivotItemViewSource(PodcastsPivotItem, async c => await c.GetShowsAsync());
            SetPivotItemViewSource(UnplayedPodcastsPivotItem, async c => await c.GetUnplayedShowsAsync());
        }

        protected override PodcastsContainer GetContainer(DACPDatabase database)
        {
            return database.PodcastsContainer;
        }

        protected override void OnListItemTap(DACPElement item, LongListSelector list)
        {
            if (item is Podcast)
            {
                Podcast podcast = (Podcast)item;
                NavigationManager.OpenPodcastEpisodesPage(podcast);
                return;
            }

            base.OnListItemTap(item, list);
        }
    }
}