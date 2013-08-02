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
using Komodex.Remote.Localization;
using System.ComponentModel;

namespace Komodex.Remote.TrialMode
{
    public partial class TrialReminderDialog : DialogUserControlBase
    {
        public TrialReminderDialog()
        {
            InitializeComponent();

            HideOnNavigate = false;
            UpdateContent();
        }

        protected void UpdateContent()
        {
            int daysLeft = TrialManager.TrialDaysRemaining ?? TrialManager.TrialDays;
            if (daysLeft > 0)
            {
                cancelButton.Visibility = System.Windows.Visibility.Visible;
                headerTextBlock.Text = LocalizedStrings.TrialReminderHeader;
                if (daysLeft == 1)
                    content1TextBlock.Text = LocalizedStrings.TrialReminderContentSingular;
                else
                    content1TextBlock.Text = string.Format(LocalizedStrings.TrialReminderContentPlural, daysLeft);
            }
            else
            {
                cancelButton.Visibility = System.Windows.Visibility.Collapsed;
                headerTextBlock.Text = LocalizedStrings.TrialReminderExpiredHeader;
                content1TextBlock.Text = LocalizedStrings.TrialReminderExpiredContent;
                HandleBackKeyPress = false;
            }
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
