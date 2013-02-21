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
using Komodex.WP7DACPRemote.DACPServerManagement;
using Clarity.Phone.Controls;
using Clarity.Phone.Controls.Animations;
using Microsoft.Phone.Shell;
using Komodex.WP7DACPRemote.Localization;
using Komodex.Common.Phone;

namespace Komodex.WP7DACPRemote.DACPServerInfoManagement
{
    public partial class LibraryChooserPage : PhoneApplicationBasePage
    {
        public LibraryChooserPage()
        {
            InitializeComponent();

            DataContext = DACPServerViewModel.Instance;

            AnimationContext = LayoutRoot;

            InitializeApplicationBar();
        }

        #region Overrides

        protected override void InitializeApplicationBar()
        {
            base.InitializeApplicationBar();

            // Add
            AddApplicationBarIconButton(LocalizedStrings.AddAppBarButton, "/icons/appbar.new.rest.png", () => NavigationManager.OpenAddNewServerPage());

            // About
            AddApplicationBarMenuItem(LocalizedStrings.AboutMenuItem, () => NavigationManager.OpenAboutPage());
        }

        #endregion

        #region Button/Action Event Handlers

        private void mnuDelete_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)sender;
            if (!(menuItem.Tag is Guid))
                return;

            Guid itemGuid = (Guid)menuItem.Tag;
            DACPServerInfo serverInfo = DACPServerViewModel.Instance.Items.FirstOrDefault(si => si.ID == itemGuid);
            if (serverInfo != null)
            {
                DACPServerViewModel.Instance.Items.Remove(serverInfo);
                if (DACPServerManager.Server != null && DACPServerManager.Server.ID == serverInfo.ID)
                {
                    DACPServerManager.ConnectToServer(true);
                    NavigationManager.ClearPageHistory();
                }
            }
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox listBox = (ListBox)sender;

            DACPServerInfo serverInfo = listBox.SelectedItem as DACPServerInfo;

            if (serverInfo != null)
            {
                DACPServerManager.ConnectToServer(serverInfo.ID);
                NavigationService.GoBack();
            }
        }

        #endregion

    }
}