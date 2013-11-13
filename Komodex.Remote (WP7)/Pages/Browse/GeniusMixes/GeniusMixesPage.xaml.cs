using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Komodex.DACP.Containers;

namespace Komodex.Remote.Pages.Browse.GeniusMixes
{
    public partial class GeniusMixesPage : BrowseDatabaseBasePage
    {
        public GeniusMixesPage()
        {
            InitializeComponent();
        }

        private void GeniusMixButton_Click(object sender, RoutedEventArgs e)
        {
            GeniusMix geniusMix = ((Button)sender).Tag as GeniusMix;
            if (geniusMix == null)
                return;

            RemoteUtility.HandleLibraryPlayTask(geniusMix.Shuffle());
        }
    }
}