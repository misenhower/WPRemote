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
using Komodex.Common.Phone;

namespace Komodex.Remote.LibraryPages
{
    public partial class LibraryViewDialog : DialogUserControlBase
    {
        public LibraryViewDialog()
        {
            InitializeComponent();
        }

        private void btnVideos_Click(object sender, RoutedEventArgs e)
        {
            NavigationManager.OpenVideosPage();
        }

        private void btnPodcasts_Click(object sender, RoutedEventArgs e)
        {
            NavigationManager.OpenPodcastsPage();
        }

    }
}
