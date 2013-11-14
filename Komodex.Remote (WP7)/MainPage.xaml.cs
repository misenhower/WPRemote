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
        private IApplicationBar _standardAppBar;
        private IApplicationBar _firstRunAppBar;

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
#endif

            _standardAppBar = ApplicationBar;

            UpdateTrialModeText();
            Loaded += ShowTrialDialogOnFirstLoad;

            DialogClosed += MainPage_DialogClosed;
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

            AttachEvents();

            UpdateBindings();
            UpdateVisualState(false);
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

        private void MainPage_DialogClosed(object sender, DialogControlClosedEventArgs e)
        {
            UpdateBindings();
        }

        #endregion

        #region Methods

        protected void AttachEvents()
        {
            ServerManager.PairedServers.CollectionChanged += PairedServers_CollectionChanged;
        }

        protected void DetachEvents()
        {
            ServerManager.PairedServers.CollectionChanged -= PairedServers_CollectionChanged;
        }

        private void PairedServers_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            UpdateBindings();
        }

        private void UpdateBindings()
        {
            // Page title
            if (CurrentServer == null || string.IsNullOrEmpty(CurrentServer.LibraryName))
                ApplicationTitle.Text = "REMOTE";
            else
                ApplicationTitle.Text = CurrentServer.LibraryName.ToUpper();

            // Panel visibility
            bool showContentPanel = (CurrentServer != null && CurrentServer.IsConnected);
            bool showFirstStartPanel = (CurrentServer == null && ServerManager.PairedServers.Count == 0);
            ContentPanel.Visibility = (showContentPanel) ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            FirstStartPanel.Visibility = (showFirstStartPanel) ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;

            DisableConnectionStatusPopup = showFirstStartPanel || IsDialogOpen;

            if (IsDialogOpen)
            {
                ApplicationBar = null;
            }
            else if (showFirstStartPanel)
            {
                ShowFirstRunAppBar();
                ApplicationBarMenuClosedOpacity = 0;
            }
            else
            {
                ApplicationBar = _standardAppBar;
                ApplicationBarMenuClosedOpacity = 0.9;
            }

            UpdateApplicationBarVisibility();
        }

        protected void ShowFirstRunAppBar()
        {
            if (_firstRunAppBar != null)
            {
                ApplicationBar = _firstRunAppBar;
                return;
            }

            _firstRunAppBar = new ApplicationBar();
            _firstRunAppBar.Mode = ApplicationBarMode.Minimized;
            _firstRunAppBar.BackgroundColor = (Color)Application.Current.Resources["PhoneBackgroundColor"];
            ApplicationBar = _firstRunAppBar;
            AddApplicationBarMenuItem(LocalizedStrings.ManualPairingMenuItem, OpenManualPairingDialog);
            AddApplicationBarMenuItem(LocalizedStrings.AboutMenuItem, NavigationManager.OpenAboutPage);
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
            NavigationManager.OpenLibraryPage(CurrentServer.MainDatabase);
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
            UtilityPairingDialog pairingDialog = new UtilityPairingDialog();
            ShowDialog(pairingDialog);
#else
            PairingDialog pairingDialog = new PairingDialog();
            ShowDialog(pairingDialog);
#endif
        }

        private void OpenManualPairingDialog()
        {
            if (IsDialogOpen)
                return;

            ManualPairingDialog pairingDialog = new ManualPairingDialog();
            ShowDialog(pairingDialog);
        }

        #endregion

        #region Trial Mode

        protected void UpdateTrialModeText()
        {
            if (!TrialManager.IsTrial)
                return;

            var days = TrialManager.TrialDaysRemaining ?? TrialManager.TrialDays;

            if (days == 1)
                trialBannerContent.Text = LocalizedStrings.TrialBannerContentSingular;
            else
                trialBannerContent.Text = string.Format(LocalizedStrings.TrialBannerContentPlural, days);

            // Adjust content panel margin to give the trial button some more room
            if (ResolutionUtility.ScreenResolution != ScreenResolution.HD720p)
            {
                var margin = MainButtonGrid.Margin;
                margin.Top = -36;
                MainButtonGrid.Margin = margin;
            }
        }

        private void ShowTrialDialogOnFirstLoad(object sender, RoutedEventArgs e)
        {
            Loaded -= ShowTrialDialogOnFirstLoad;

            if (!TrialManager.IsTrial)
                return;

            // Don't show the reminder dialog before the trial period has begun
            if (TrialManager.TrialState != TrialState.Expired && TrialManager.TrialDaysRemaining == null)
                return;

            ShowTrialDialog();
        }


        private void TrialButton_Click(object sender, RoutedEventArgs e)
        {
            ShowTrialDialog();
        }

        protected void ShowTrialDialog()
        {
            TrialReminderDialog dialog = new TrialReminderDialog();
            ShowDialog(dialog);
            UpdateBindings();
        }

        #endregion
    }
}