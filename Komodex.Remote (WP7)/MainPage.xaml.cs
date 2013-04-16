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
using Komodex.Remote.DACPServerInfoManagement;
using Microsoft.Phone.Shell;
using Komodex.Remote.DACPServerManagement;
using Komodex.DACP.Library;
using Komodex.Common.Phone;
using Komodex.Remote.TrialMode;
using Komodex.Remote.Localization;
using Komodex.Remote.ServerManagement;

namespace Komodex.Remote
{
    public partial class MainPage : DACPServerBoundPhoneApplicationPage
    {
        public MainPage()
        {
            InitializeComponent();

            DisableConnectionStatusPopup = true;

            ApplicationBarClosedOpacity = 0.9;
            ApplicationBarOpenOpacity = 0.9;

            AnimationContext = LayoutRoot;
            DialogContainer = DialogPopupContainer;

            // Application Bar
            InitializeApplicationBar();
            InitializeStandardPlayTransportApplicationBar();
            AddChooseLibraryApplicationBarMenuItem();
            AddSettingsApplicationBarMenuItem();
            AddAboutApplicationBarMenuItem();

#if DEBUG
            UpdateDebugDataMenuItem();
            AddTrialSimulationMenuItem();
#endif

            Loaded += new RoutedEventHandler(MainPage_Loaded);
        }

        protected bool _initialized;

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (_initialized)
                return;

            _initialized = true;

            if (!TrialManager.Current.IsTrial)
                return;

            if (DACPServerViewModel.Instance.CurrentDACPServer == null)
                return;

            TrialReminderDialog trialDialog = new TrialReminderDialog();
            trialDialog.Closed += new EventHandler<DialogControlClosedEventArgs>(trialDialog_Closed);

            DACPServerManager.ShowPopups = false;
            ShowDialog(trialDialog);
        }

        private void trialDialog_Closed(object sender, DialogControlClosedEventArgs e)
        {
            DACPServerManager.ShowPopups = true;

            if (e.Result == MessageBoxResult.OK)
                return;

            DACPServerManager.ConnectToServer();
        }

#if DEBUG
        ApplicationBarMenuItem debugDataMenuItem = null;

        private void UpdateDebugDataMenuItem()
        {
            if (debugDataMenuItem == null)
            {
                debugDataMenuItem = new ApplicationBarMenuItem("diagnostic data");
                debugDataMenuItem.Click += new EventHandler(debugDataMenuItem_Click);
                ApplicationBar.MenuItems.Add(debugDataMenuItem);
            }

            if (((App)App.Current).EnableDiagnosticData == true)
                debugDataMenuItem.Text = "hide diagnostic data";
            else
                debugDataMenuItem.Text = "show diagnostic data";
        }

        void debugDataMenuItem_Click(object sender, EventArgs e)
        {
            App app = (App)App.Current;
            app.EnableDiagnosticData = !app.EnableDiagnosticData;
            UpdateDebugDataMenuItem();
        }
#endif

        #region Overrides

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            UpdateBindings();
            UpdateVisualState(false);

            if (ServerManager.PairedServers.Count > 0)
                DisableConnectionStatusPopup = false;
            else
                DisableConnectionStatusPopup = true;

            if (TrialManager.Current.IsTrial)
            {
                if (TrialManager.Current.TrialDaysLeft == 1)
                    trialBannerContent.Text = LocalizedStrings.TrialBannerContentSingular;
                else
                    trialBannerContent.Text = string.Format(LocalizedStrings.TrialBannerContentPlural, TrialManager.Current.TrialDaysLeft);

                // Adjust the content panel margin to allow some more space around the trial banner
                var margin = ContentPanel.Margin;
                margin.Top = -36;
                ContentPanel.Margin = margin;
            }
            else
            {
                btnTrial.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            if (TrialManager.Current.TrialExpired && CurrentDialog != null && CurrentDialog.IsOpen)
                CurrentDialog.Hide();

            base.OnBackKeyPress(e);
        }

        protected override void DACPServer_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            base.DACPServer_PropertyChanged(sender, e);

            switch (e.PropertyName)
            {
                case "LibraryName":
                case "IsConnected":
                case "PlayState":
                case "CurrentSongName":
                    UpdateBindings();
                    UpdateVisualState();
                    break;
                default:
                    break;
            }
        }

        protected override void DACPServer_ServerUpdate(object sender, ServerUpdateEventArgs e)
        {
            base.DACPServer_ServerUpdate(sender, e);
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                UpdateBindings();
                UpdateVisualState();
            });
        }

        protected override void DACPServerManager_ServerChanged(object sender, EventArgs e)
        {
            base.DACPServerManager_ServerChanged(sender, e);
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                UpdateBindings();
                UpdateVisualState();
            });
        }

        #endregion

        #region Methods

        private void UpdateBindings()
        {
            // Page title
            if (DACPServer == null || string.IsNullOrEmpty(DACPServer.LibraryName))
                ApplicationTitle.Text = "REMOTE";
            else if (!string.IsNullOrEmpty(DACPServer.LibraryName))
                ApplicationTitle.Text = DACPServer.LibraryName.ToUpper();
            else
                ApplicationTitle.Text = string.Empty;

            // Panel visibility
            ContentPanel.Visibility = (DACPServer != null && DACPServer.IsConnected) ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            FirstStartPanel.Visibility = (DACPServer == null && DACPServerViewModel.Instance.Items.Count == 0) ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        }

        private void UpdateVisualState(bool useTransitions = true)
        {
            if (DACPServer == null || (DACPServer.PlayState == DACP.PlayStates.Stopped && DACPServer.CurrentSongName == null))
            {
                VisualStateManager.GoToState(this, "StoppedState", useTransitions);
                btnNowPlaying.IsEnabled = false;
            }
            else
            {
                VisualStateManager.GoToState(this, "PlayingState", useTransitions);
                btnNowPlaying.IsEnabled = true;
            }
        }

        #endregion

        #region Actions

        private void btnNowPlaying_Click(object sender, RoutedEventArgs e)
        {
            NavigationManager.OpenNowPlayingPage();
        }

        private void btnLibrary_Click(object sender, RoutedEventArgs e)
        {
            NavigationManager.OpenMainLibraryPage();
        }

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            NavigationManager.OpenSearchPage();
        }

        private void btnAddLibrary_Click(object sender, RoutedEventArgs e)
        {
            NavigationManager.OpenManualPairingPage();
        }

        private void btnTrial_Click(object sender, RoutedEventArgs e)
        {
            TrialReminderDialog trialDialog = new TrialReminderDialog();
            ShowDialog(trialDialog);
        }

        #endregion
    }
}