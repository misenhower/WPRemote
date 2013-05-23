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
using Microsoft.Phone.Shell;
using Komodex.DACP.Library;
using Komodex.Common.Phone;
using Komodex.Remote.TrialMode;
using Komodex.Remote.Localization;
using Komodex.Remote.ServerManagement;
using Komodex.Remote.Pairing;
using Komodex.Common;
using Komodex.Remote.Controls;

namespace Komodex.Remote
{
    public partial class MainPage : RemoteBasePage
    {
        public MainPage()
        {
            InitializeComponent();

            DisableConnectionStatusPopup = true;

            ApplicationBarMenuClosedOpacity = 0.9;
            ApplicationBarMenuOpenOpacity = 0.9;

            // Application Bar
            AddAppBarPlayTransportButtons();
            AddApplicationBarMenuItem(LocalizedStrings.ChooseLibraryMenuItem, NavigationManager.OpenChooseLibraryPage);
            AddApplicationBarMenuItem(LocalizedStrings.SettingsMenuItem, NavigationManager.OpenSettingsPage);
            AddApplicationBarMenuItem(LocalizedStrings.AboutMenuItem, NavigationManager.OpenAboutPage);

#if DEBUG
            UpdateDebugDataMenuItem();
            AddTrialSimulationMenuItem();
#endif

            Loaded += MainPage_Loaded;

            ServerManager.ConnectionStateChanged += (sender, e) => Utility.BeginInvokeOnUIThread(() => UpdateBindings());
        }

        protected bool _initialized;

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (_initialized)
                return;

            _initialized = true;

            if (!TrialManager.Current.IsTrial)
                return;

            TrialReminderDialog trialDialog = new TrialReminderDialog();
            trialDialog.Closed += trialDialog_Closed;

            ShowDialog(trialDialog);
        }

        private void trialDialog_Closed(object sender, DialogControlClosedEventArgs e)
        {
            if (e.Result == MessageBoxResult.OK)
                return;
        }

#if DEBUG
        ApplicationBarMenuItem debugDataMenuItem = null;

        private void UpdateDebugDataMenuItem()
        {
            if (debugDataMenuItem == null)
            {
                debugDataMenuItem = new ApplicationBarMenuItem("diagnostic data");
                debugDataMenuItem.Click += debugDataMenuItem_Click;
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
            if (TrialManager.Current.TrialExpired && IsDialogOpen)
                CurrentDialogControl.Hide();

            base.OnBackKeyPress(e);
        }

        protected override void CurrentServer_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            base.CurrentServer_PropertyChanged(sender, e);

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

        protected override void CurrentServer_ServerUpdate(object sender, ServerUpdateEventArgs e)
        {
            base.CurrentServer_ServerUpdate(sender, e);
            Utility.BeginInvokeOnUIThread(() =>
            {
                UpdateBindings();
                UpdateVisualState();
            });
        }

        protected override void OnServerChanged()
        {
            base.OnServerChanged();
            Utility.BeginInvokeOnUIThread(() =>
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
            if (CurrentServer == null || string.IsNullOrEmpty(CurrentServer.LibraryName))
                ApplicationTitle.Text = "REMOTE";
            else if (!string.IsNullOrEmpty(CurrentServer.LibraryName))
                ApplicationTitle.Text = CurrentServer.LibraryName.ToUpper();
            else
                ApplicationTitle.Text = string.Empty;

            // Panel visibility
            bool showContentPanel = (CurrentServer != null && CurrentServer.IsConnected);
            bool showFirstStartPanel = (CurrentServer == null && ServerManager.PairedServers.Count == 0);
            ContentPanel.Visibility = (showContentPanel) ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            FirstStartPanel.Visibility = (showFirstStartPanel) ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            HideApplicationBar = showFirstStartPanel;
        }

        private void UpdateVisualState(bool useTransitions = true)
        {
            if (CurrentServer == null || (CurrentServer.PlayState == DACP.PlayStates.Stopped && CurrentServer.CurrentSongName == null))
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
            if (IsDialogOpen)
                return;

#if WP7
#else
            PairingDialog pairingDialog = new PairingDialog();
            ShowDialog(pairingDialog);
#endif
        }

        private void btnTrial_Click(object sender, RoutedEventArgs e)
        {
            TrialReminderDialog trialDialog = new TrialReminderDialog();
            ShowDialog(trialDialog);
        }

        #endregion
    }
}