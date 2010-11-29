﻿using System;
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
using Komodex.DACP;
using Komodex.WP7DACPRemote.DACPServerInfoManagement;
using Microsoft.Phone.Shell;
using Komodex.WP7DACPRemote.DACPServerManagement;
using Komodex.DACP.Library;

namespace Komodex.WP7DACPRemote
{
    public partial class MainPage : DACPServerBoundPhoneApplicationPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private void btnNowPlaying_Click(object sender, RoutedEventArgs e)
        {
            NavigationManager.OpenNowPlayingPage();
        }

        private void btnLibrary_Click(object sender, RoutedEventArgs e)
        {
            NavigationManager.OpenMainLibraryPage();
        }
    }
}