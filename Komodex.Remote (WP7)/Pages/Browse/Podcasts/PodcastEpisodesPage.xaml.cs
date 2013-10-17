using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Komodex.DACP.Databases;
using Komodex.DACP.Containers;
using System.Threading.Tasks;
using Komodex.DACP.Library;

namespace Komodex.Remote.Pages.Browse.Podcasts
{
    public partial class PodcastEpisodesPage : BrowsePodcastBasePage
    {
        public PodcastEpisodesPage()
        {
            InitializeComponent();

            EpisodesPivotItem.DataContext = GetGroupViewSource(async g => await g.GetEpisodesAsync());
        }
    }
}