using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Komodex.DACP.Groups;
using Komodex.DACP;

namespace Komodex.Remote.Pages.Browse.TVShows
{
    public partial class TVShowsPage : BrowseTVShowsContainerBasePage
    {
        public TVShowsPage()
        {
            InitializeComponent();

            TVShowsViewSource = GetContainerViewSource(async c => await c.GetShowsAsync());
            UnwatchedTVShowsViewSource = GetContainerViewSource(async c => await c.GetUnwatchedShowsAsync());
        }

        public object TVShowsViewSource { get; private set; }
        public object UnwatchedTVShowsViewSource { get; private set; }

        protected override void OnListItemTap(DACPElement item, Common.Phone.Controls.LongListSelector list, bool isPlayButton)
        {
            if (item is TVShow)
            {
                TVShow tvShow = (TVShow)item;
                NavigationManager.OpenTVShowEpisodesPage(tvShow);
                return;
            }
        }
    }
}