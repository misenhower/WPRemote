using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Komodex.Remote.Pairing;
using Komodex.Common.Phone;

namespace Komodex.Remote.Pages.Pairing
{
    public partial class ManualPairingPage : PhoneApplicationBasePage
    {
        public ManualPairingPage()
        {
            InitializeComponent();

            libraryList.ItemsSource = ManualPairingManager.DiscoveredPairingUtilities;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            ManualPairingManager.SearchForPairingUtility();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            ManualPairingManager.StopSearchingForPairingUtility();
        }
    }
}