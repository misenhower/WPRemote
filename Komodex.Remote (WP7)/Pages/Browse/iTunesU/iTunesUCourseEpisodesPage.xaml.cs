using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace Komodex.Remote.Pages.Browse.iTunesU
{
    public partial class iTunesUCourseEpisodesPage : BrowseiTunesUCourseBasePage
    {
        public iTunesUCourseEpisodesPage()
        {
            InitializeComponent();

            EpisodesViewSource = GetGroupViewSource(async g => await g.GetEpisodesAsync());
            UnplayedEpisodesViewSource = GetGroupViewSource(async g => await g.GetUnplayedEpisodesAsync());
        }

        public object EpisodesViewSource { get; private set; }
        public object UnplayedEpisodesViewSource { get; private set; }
    }
}