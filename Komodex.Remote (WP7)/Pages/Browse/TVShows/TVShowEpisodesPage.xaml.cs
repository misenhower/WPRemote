using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace Komodex.Remote.Pages.Browse.TVShows
{
    public partial class TVShowEpisodesPage : BrowseTVShowBasePage
    {
        public TVShowEpisodesPage()
        {
            InitializeComponent();

            EpisodesViewSource = GetGroupViewSource(async g => await g.GetEpisodesAsync());
            UnwatchedEpisodesViewSource = GetGroupViewSource(async g => await g.GetUnwatchedEpisodesAsync());
        }

        public object EpisodesViewSource { get; private set; }
        public object UnwatchedEpisodesViewSource { get; private set; }
    }
}