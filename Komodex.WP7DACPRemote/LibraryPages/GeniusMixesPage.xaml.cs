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

namespace Komodex.WP7DACPRemote.LibraryPages
{
    public partial class GeniusMixesPage : DACPServerBoundPhoneApplicationPage
    {
        public GeniusMixesPage()
        {
            InitializeComponent();
        }

        private void GeniusMixButton_Click(object sender, RoutedEventArgs e)
        {
            Playlist playlist = ((Button)sender).Tag as Playlist;
            if (playlist == null)
                return;

            playlist.SendShuffleSongsCommand();
            NavigationManager.OpenNowPlayingPage();
        }
    }
}