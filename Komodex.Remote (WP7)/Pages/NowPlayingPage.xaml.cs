using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Media;

namespace Komodex.Remote.Pages
{
    public partial class NowPlayingPage : RemoteBasePage
    {
        public NowPlayingPage()
        {
            InitializeComponent();

            // Set up Application Bar
            InitializeApplicationBar();
            ApplicationBar.Mode = ApplicationBarMode.Minimized;
            ApplicationBar.BackgroundColor = (Color)Application.Current.Resources["PhoneBackgroundColor"];
        }
    }
}