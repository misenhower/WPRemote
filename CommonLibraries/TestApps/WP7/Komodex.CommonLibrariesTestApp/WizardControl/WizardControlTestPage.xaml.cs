using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace Komodex.CommonLibrariesTestApp.WizardControl
{
    public partial class WizardControlTestPage : PhoneApplicationPage
    {
        public WizardControlTestPage()
        {
            InitializeComponent();
        }

        private void AnimatedNavigationButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo((string)((Button)sender).Tag, true);
        }

        private void InstantNavigationButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo((string)((Button)sender).Tag, false);
        }

        private void NavigateTo(string page, bool animated)
        {
            switch (page)
            {
                case "1":
                    wizard.SetSelectedIndex(0, animated);
                    break;

                case "2":
                    wizard.SetSelectedIndex(1, animated);
                    break;

                case "3":
                    wizard.SetSelectedIndex(2, animated);
                    break;

                case "4":
                    wizard.SetSelectedIndex(3, animated);
                    break;
            }
        }
    }
}