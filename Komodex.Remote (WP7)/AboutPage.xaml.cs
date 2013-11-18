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
using System.Reflection;
using Komodex.Remote.Utilities;
using Komodex.DACP;
using Microsoft.Phone.Tasks;
using Clarity.Phone.Controls;
using Komodex.Remote.Localization;
using Komodex.Common;

namespace Komodex.Remote
{
    public partial class AboutPage : RemoteBasePage
    {
        public AboutPage()
        {
            InitializeComponent();

            DisableConnectionStatusPopup = true;

            SetUpRichTextBox(ContactUsRichTextBox, LocalizedStrings.ContactUs, OpenSupportEmail);
            SetUpRichTextBox(RateAndReviewRichTextBox, LocalizedStrings.RateAndReview, OpenRateAndReview);
            SetUpRichTextBox(TwitterRichTextBox, LocalizedStrings.FollowOnTwitter, OpenTwitterLink);
        }

        #region Overrides

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Application information
            string version = Utility.ApplicationVersion;
            // Clean up the version number by removing up two two trailing ".0"s
            if (version.EndsWith(".0"))
            {
                version = version.Substring(0, version.Length - 2);
                if (version.EndsWith(".0"))
                    version = version.Substring(0, version.Length - 2);
            }
            tbAppVersion.Text = LocalizedStrings.Version + " " + version;
#if DEBUG
            tbAppVersion.Text += " (Debug)";

            // Device information
            tbManufacturer.Text = DeviceInfo.DeviceManufacturer;
            tbDevice.Text = DeviceInfo.DeviceName;
            tbFirmware.Text = DeviceInfo.DeviceFirmwareVersion;
            tbHardwareVersion.Text = DeviceInfo.DeviceHardwareVersion;
#endif

            UpdateServerInfo();
        }

        protected override void ServerManager_ConnectionStateChanged(object sender, ServerManagement.ConnectionStateChangedEventArgs e)
        {
            base.ServerManager_ConnectionStateChanged(sender, e);

            Utility.BeginInvokeOnUIThread(UpdateServerInfo);
        }

        protected override void CurrentServer_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            base.CurrentServer_PropertyChanged(sender, e);

            UpdateServerInfo();
        }

        #endregion

        #region Methods

        private void UpdateServerInfo()
        {
            if (NavigationContext.QueryString.ContainsKey("version"))
            {
                tbiTunesNotConnected.Visibility = System.Windows.Visibility.Visible;
                gridiTunesInfo.Visibility = System.Windows.Visibility.Visible;

                tbiTunesNotConnected.Text = LocalizedStrings.FromLastConnectionAttempt;

                var queryString = NavigationContext.QueryString;
                string iTunesVersion = queryString["version"];
                int iTunesProtocolVersion = int.Parse(queryString["protocol"]);
                int iTunesDMAPVersion = int.Parse(queryString["dmap"]);
                int iTunesDAAPVersion = int.Parse(queryString["daap"]);

                tbiTunesVersion.Text = iTunesVersion;
                tbiTunesProtocolVersion.Text = iTunesProtocolVersion.ToString("x").ToUpper();
                tbiTunesDMAPVersion.Text = iTunesDMAPVersion.ToString("x").ToUpper();
                tbiTunesDAAPVersion.Text = iTunesDAAPVersion.ToString("x").ToUpper();
            }
            else if (CurrentServer != null && CurrentServer.IsConnected)
            {
                tbiTunesNotConnected.Visibility = System.Windows.Visibility.Collapsed;
                gridiTunesInfo.Visibility = System.Windows.Visibility.Visible;

                tbiTunesVersion.Text = CurrentServer.ServerVersionString;
                tbiTunesProtocolVersion.Text = CurrentServer.ServerVersion.ToString("x").ToUpper();
                tbiTunesDMAPVersion.Text = CurrentServer.ServerDMAPVersion.ToString("x").ToUpper();
                tbiTunesDAAPVersion.Text = CurrentServer.ServerDAAPVersion.ToString("x").ToUpper();
            }
            else
            {
                tbiTunesNotConnected.Visibility = System.Windows.Visibility.Visible;
                gridiTunesInfo.Visibility = System.Windows.Visibility.Collapsed;
            }

            if (string.IsNullOrEmpty(tbiTunesVersion.Text))
                tbiTunesVersion.Text = "N/A";

            var twitterVisibility = System.Windows.Visibility.Visible;
            if (tbiTunesNotConnected.Visibility == System.Windows.Visibility.Visible)
                twitterVisibility = System.Windows.Visibility.Collapsed;
            RateAndReviewRichTextBox.Visibility = twitterVisibility;
            TwitterRichTextBox.Visibility = twitterVisibility;
        }

        #endregion

        #region Button/Link Actions

        int assemblyInfoCount = 25;
        private void btnAssemblyInfo_Click(object sender, RoutedEventArgs e)
        {
            if (assemblyInfoCount-- == 0)
            {
                Komodex.Analytics.CrashReporter.LogMessage(Komodex.Common.Phone.PhoneUtility.GetFormattedIsolatedStorageContents(), "Isolated Storage Report", true);
                MessageBox.Show(Assembly.GetExecutingAssembly().FullName);
                MessageBox.Show(DACPServer.GetAssemblyName());
                MessageBox.Show(Assembly.GetCallingAssembly().FullName);
            }
        }

        private void btniTunesInfo_Click(object sender, RoutedEventArgs e)
        {
#if DEBUG
            if (DeviceInfoPanel.Visibility == System.Windows.Visibility.Visible)
            {
                DeviceInfoPanel.Visibility = System.Windows.Visibility.Collapsed;
                iTunesInfoPanel.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                DeviceInfoPanel.Visibility = System.Windows.Visibility.Visible;
                iTunesInfoPanel.Visibility = System.Windows.Visibility.Collapsed;
            }
#endif
        }

        private void SetUpRichTextBox(RichTextBox richTextBox, string content, Action action)
        {
            richTextBox.Blocks.Clear();
            var parts = content.Split('[', ']');

            Paragraph p = new Paragraph();
            p.Inlines.Add(new Run() { Text = parts[0] });
            Hyperlink h = new Hyperlink();
            h.Inlines.Add(new Run() { Text = parts[1] });
            h.Command = new HyperlinkCommand(this, action);
            p.Inlines.Add(h);
            p.Inlines.Add(new Run() { Text = parts[2] });

            richTextBox.Blocks.Add(p);
            richTextBox.IsReadOnly = true;
        }

        private void OpenSupportEmail()
        {
            EmailComposeTask t = new EmailComposeTask();
            t.To = "Komodex Support <info@komodex.com>";
            t.Subject = "[Komodex] Remote v" + Utility.ApplicationVersion + " Feedback";
            if (CurrentServer != null && CurrentServer.IsConnected)
                t.Body = "\n\n\nRemote Diagnostic Info:\nConnected Server Version: " + CurrentServer.ServerVersionString;
            t.Show();
        }

        private void OpenRateAndReview()
        {
            MarketplaceReviewTask t = new MarketplaceReviewTask();
            t.Show();
        }

        private void OpenTwitterLink()
        {
            WebBrowserTask t = new WebBrowserTask();
            t.Uri = new Uri("http://twitter.com/WP7remote");
            t.Show();
        }

        private void btnLogo_Click(object sender, RoutedEventArgs e)
        {
            WebBrowserTask t = new WebBrowserTask();
            t.Uri = new Uri("http://komodex.com/");
            t.Show();
        }

        #endregion

        // Hyperlinks in RichTextBox under WP8 don't seem to send the Click event reliably. Using a Command instead seems to work.
        private class HyperlinkCommand : ICommand
        {
            private AboutPage _page;
            private Action _action;

            public HyperlinkCommand(AboutPage page, Action a)
            {
                _page = page;
                _action = a;
            }

            public bool CanExecute(object parameter)
            {
                return true;
            }

            public event EventHandler CanExecuteChanged;

            public void Execute(object parameter)
            {
                _page.Focus();
                _action();
            }
        }
    }
}