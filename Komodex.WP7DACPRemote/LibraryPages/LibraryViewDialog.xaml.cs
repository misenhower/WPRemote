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

namespace Komodex.WP7DACPRemote.LibraryPages
{
    public partial class LibraryViewDialog : UserControl
    {
        public LibraryViewDialog()
        {
            InitializeComponent();
        }

        private void btnVideos_Click(object sender, RoutedEventArgs e)
        {
            NavigationManager.OpenVideosPage();
        }

    }
}
