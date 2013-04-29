using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Komodex.Remote.ServerManagement;
using Komodex.Remote.Localization;

namespace Komodex.Remote.Pages
{
    public partial class ChooseLibraryPage : RemoteBasePage
    {
        public ChooseLibraryPage()
        {
            InitializeComponent();

            DisableConnectionStatusPopup = true;

            AnimationContext = LayoutRoot;

            LayoutRoot.DataContext = ServerManager.PairedServers;

            ApplicationBar = new ApplicationBar();
#if WP7
            AddApplicationBarIconButton(LocalizedStrings.AddAppBarButton, "/icons/appbar.new.rest.png", () => NavigationManager.OpenManualPairingPage());
#else
            AddApplicationBarIconButton(LocalizedStrings.AddAppBarButton, "/icons/appbar.new.rest.png", () => NavigationManager.OpenPairingPage());
#endif
        }

        private void LibraryList_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ServerConnectionInfo info = LibraryList.SelectedItem as ServerConnectionInfo;
            if (info == null)
                return;

            ServerManager.ChooseServer(info);
            NavigationManager.GoToFirstPage();
        }

        private void DeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)sender;
            ServerConnectionInfo info = menuItem.Tag as ServerConnectionInfo;
            if (info == null)
                return;

            ServerManager.RemoveServerInfo(info);
        }
    }
}