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

namespace Komodex.WP7DACPRemote.DACPServerInfoManagement
{
    public partial class LibraryChooserPage : AnimatedBasePage
    {
        public LibraryChooserPage()
        {
            InitializeComponent();

            DataContext = DACPServerViewModel.Instance;

            AnimationContext = LayoutRoot;

            // "Add" application bar button
            ApplicationBarIconButton addButton = new ApplicationBarIconButton(new Uri("/icons/appbar.new.rest.png", UriKind.Relative));
            addButton.Text = LocalizedStrings.AddAppBarButton;
            addButton.Click += new EventHandler(btnNew_Click);
            ApplicationBar.Buttons.Add(addButton);

            // "About" application bar menu item
            ApplicationBarMenuItem aboutMenuItem = new ApplicationBarMenuItem(LocalizedStrings.AboutMenuItem);
            aboutMenuItem.Click += new EventHandler(mnuAbout_Click);
            ApplicationBar.MenuItems.Add(aboutMenuItem);
        }

        private bool _deletedConnectedServer = false;

        #region Static Properties

        private static bool _SuppressAutoOpenAddNewServerPage = false;
        public static bool SuppressAutoOpenAddNewServerPage
        {
            get { return _SuppressAutoOpenAddNewServerPage; }
            set { _SuppressAutoOpenAddNewServerPage = value; }
        }

        #endregion

        #region Overrides

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            if (!SuppressAutoOpenAddNewServerPage && DACPServerViewModel.Instance.Items.Count == 0)
            {
                SuppressAutoOpenAddNewServerPage = true; // This needs to be set to false at some point
                NavigationManager.OpenAddNewServerPage();
                return;
            }

            base.OnNavigatedTo(e);
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            base.OnBackKeyPress(e);

            if (_deletedConnectedServer)
            {
                _deletedConnectedServer = false;
                NavigationManager.GoToFirstPage();
            }
        }

        #endregion

        #region Button/Action Event Handlers

        private void btnNew_Click(object sender, EventArgs e)
        {
            NavigationManager.OpenAddNewServerPage();
        }

        private void mnuAbout_Click(object sender, EventArgs e)
        {
            NavigationManager.OpenAboutPage();
        }

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
                    _deletedConnectedServer = true;
                    DACPServerManager.ConnectToServer(true);
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