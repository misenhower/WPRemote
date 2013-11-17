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

namespace Komodex.Remote.Pages.Browse.iTunesU
{
    public partial class iTunesUCoursesPage : BrowseiTunesUContainerBasePage
    {
        public iTunesUCoursesPage()
        {
            InitializeComponent();

            CoursesViewSource = GetContainerViewSource(async c => await c.GetCoursesAsync());
            UnplayedCoursesViewSource = GetContainerViewSource(async c => await c.GetUnplayedCoursesAsync());
        }

        public object CoursesViewSource { get; private set; }
        public object UnplayedCoursesViewSource { get; private set; }

        protected override void OnListItemTap(DACPElement item, Common.Phone.Controls.LongListSelector list, bool isPlayButton)
        {
            if (item is iTunesUCourse)
            {
                NavigationManager.OpeniTunesUCourseEpisodesPage((iTunesUCourse)item);
                return;
            }
        }
    }
}