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
using Microsoft.Unsupported;

namespace Komodex.WP7DACPRemote.DACPServerInfoManagement
{
    public partial class LibraryChooserPage : PhoneApplicationPage
    {
        public LibraryChooserPage()
        {
            InitializeComponent();

            TiltEffect.SetIsTiltEnabled(this, true);
            DataContext = DACPServerViewModel.Instance;
        }

        private void btnNew_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/DACPServerInfoManagement/AddLibraryPage.xaml", UriKind.Relative));
        }

        private void mnuDelete_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)sender;
            if (!(menuItem.Tag is Guid))
                return;

            Guid itemGuid = (Guid)menuItem.Tag;
            DACPServerInfo serverInfo = DACPServerViewModel.Instance.Items.FirstOrDefault(si => si.ID == itemGuid);
            if (serverInfo != null)
                DACPServerViewModel.Instance.Items.Remove(serverInfo);
        }

    }
}