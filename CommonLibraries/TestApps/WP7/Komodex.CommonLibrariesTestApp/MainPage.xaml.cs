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
using Komodex.Common.Phone;

namespace Komodex.CommonLibrariesTestApp
{
    public partial class MainPage : PhoneApplicationBasePage
    {
        // Constructor
        public MainPage()
        {
            InitializeComponent();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (mytb.IsEnabled)
                mytb.IsEnabled = false;
            else
                mytb.IsEnabled = true;
        }
    }
}