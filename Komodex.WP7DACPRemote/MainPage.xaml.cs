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

namespace Komodex.WP7DACPRemote
{
    public partial class MainPage : PhoneApplicationPage
    {
        public MainPage()
        {
            InitializeComponent();

            SetVisibility(false);

        }

        private void SetVisibility(bool serverConnected)
        {
            if (serverConnected)
            {
                pivotControl.Visibility = System.Windows.Visibility.Visible;
                ApplicationBar.IsVisible = true;
                connectingStatusControl.ShowProgress = false;
            }
            else
            {
                pivotControl.Visibility = System.Windows.Visibility.Collapsed;
                ApplicationBar.IsVisible = false;
                connectingStatusControl.ShowProgress = true;
            }
        }
    }
}