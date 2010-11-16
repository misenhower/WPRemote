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
using Komodex.DACP;

namespace Komodex.WP7DACPRemote.DACPServerInfoManagement
{
    public partial class AddLibraryPage : PhoneApplicationPage
    {
        DACPServerInfo serverInfo = null;

        public AddLibraryPage()
        {
            InitializeComponent();

            serverInfo = new DACPServerInfo();
            DataContext = serverInfo;
        }

        private void tbPIN_KeyDown(object sender, KeyEventArgs e)
        {
            // Only allow numeric characters
            bool validCharacter = (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9) || (e.Key >= Key.D0 && e.Key <= Key.D9);

            if (!validCharacter)
                e.Handled = true;

        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            //DACPServer server = new DACPServer(tbHost.Text, tbp)
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            NavigationService.GoBack();
        }
    }
}