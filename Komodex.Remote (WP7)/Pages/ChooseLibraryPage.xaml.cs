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
using Komodex.Remote.Controls;
using Komodex.Common;

namespace Komodex.Remote.Pages
{
    public partial class ChooseLibraryPage : RemoteBasePage
    {
        public ChooseLibraryPage()
        {
            InitializeComponent();

            DisableConnectionStatusPopup = true;

            LayoutRoot.DataContext = ServerManager.PairedServers;

            ApplicationBar = new ApplicationBar();

            AddApplicationBarIconButton(LocalizedStrings.AddAppBarButton, ResolutionUtility.GetUriWithResolutionSuffix("/Assets/Icons/Add.png"), ShowPairingDialog);
            AddApplicationBarMenuItem(LocalizedStrings.ManualPairingMenuItem, ShowManualPairingDialog);
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

        protected void ShowPairingDialog()
        {
            if (IsDialogOpen)
                return;

#if WP7
            UtilityPairingDialog pairingDialog = new UtilityPairingDialog();
            ShowDialog(pairingDialog);
#else
            PairingDialog pairingDialog = new PairingDialog();
            ShowDialog(pairingDialog);
#endif
        }

        protected void ShowManualPairingDialog()
        {
            if (IsDialogOpen)
                return;

            ManualPairingDialog pairingDialog = new ManualPairingDialog();
            ShowDialog(pairingDialog);
        }
    }
}