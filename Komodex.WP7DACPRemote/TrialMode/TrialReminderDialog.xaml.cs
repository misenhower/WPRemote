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
using Komodex.Common.Phone;

namespace Komodex.WP7DACPRemote.TrialMode
{
    public partial class TrialReminderDialog : DialogUserControlBase
    {
        public TrialReminderDialog()
        {
            InitializeComponent();
        }

        private void BuyButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationManager.OpenMarketplaceDetailPage();

            Hide(MessageBoxResult.OK);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Hide(MessageBoxResult.Cancel);
        }
    }
}
