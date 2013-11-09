﻿using System;
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

            // Localized contact and twitter links
            string contactString = LocalizedStrings.ContactUs;
            string[] contactStringParts = contactString.Split('[', ']');
            contact1.Text = contactStringParts[0];
            contact2.Content = contactStringParts[1];
            contact3.Text = contactStringParts[2];

            string twitterString = LocalizedStrings.FollowOnTwitter;
            string[] twitterStringParts = twitterString.Split('[', ']');
            twitter1.Text = twitterStringParts[0];
            twitter2.Content = twitterStringParts[1];
            twitter3.Text = twitterStringParts[2];
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

        protected override void CurrentServer_ServerUpdate(object sender, ServerUpdateEventArgs e)
        {
            base.CurrentServer_ServerUpdate(sender, e);

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
            twitter1.Visibility = twitter2.Visibility = twitter3.Visibility = twitterVisibility;
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

        private void btnSupport_Click(object sender, RoutedEventArgs e)
        {
            EmailComposeTask t = new EmailComposeTask();
            t.To = "Komodex Support <info@komodex.com>";
            t.Subject = "[Komodex] Remote v" + Utility.ApplicationVersion + " Feedback";
            t.Show();
        }

        private void btnTwitter_Click(object sender, RoutedEventArgs e)
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

    }
}