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
using Komodex.WP7DACPRemote.Utilities;
using Komodex.WP7DACPRemote.DACPServerManagement;
using Komodex.DACP;
using Microsoft.Phone.Tasks;
using Clarity.Phone.Controls;

namespace Komodex.WP7DACPRemote
{
    public partial class AboutPage : AnimatedBasePage
    {
        public AboutPage()
        {
            InitializeComponent();

            AnimationContext = LayoutRoot;
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Application information
            tbAppVersion.Text = "Version: " + GetApplicationVersion();
#if DEBUG
            tbAppVersion.Text += " (Debug)";
#endif

            // iTunes information
            DACPServer server = DACPServerManager.Server;
            if (server != null && server.IsConnected)
            {
                tbiTunesNotConnected.Visibility = System.Windows.Visibility.Collapsed;
                gridiTunesInfo.Visibility = System.Windows.Visibility.Visible;

                tbiTunesVersion.Text = server.ServerVersion.ToString("x").ToUpper();
                tbiTunesDMAPVersion.Text = server.ServerDMAPVersion.ToString("x").ToUpper();
                tbiTunesDAAPVersion.Text = server.ServerDAAPVersion.ToString("x").ToUpper();
            }
            else
            {
                tbiTunesNotConnected.Visibility = System.Windows.Visibility.Visible;
                gridiTunesInfo.Visibility = System.Windows.Visibility.Collapsed;
            }

#if DEBUG
            // Device information
            tbManufacturer.Text = DeviceInfo.DeviceManufacturer;
            tbDevice.Text = DeviceInfo.DeviceName;
            tbFirmware.Text = DeviceInfo.DeviceFirmwareVersion;
            tbHardwareVersion.Text = DeviceInfo.DeviceHardwareVersion;

            DeviceInfoPanel.Visibility = System.Windows.Visibility.Visible;
#endif

        }

        private string GetApplicationVersion()
        {
            string assemblyInfo = Assembly.GetExecutingAssembly().FullName;
            if (string.IsNullOrEmpty(assemblyInfo))
                return string.Empty;

            var splitString = assemblyInfo.Split(',');
            foreach (var stringPart in splitString)
            {
                var stringPartTrimmed = stringPart.Trim();
                if (stringPartTrimmed.StartsWith("Version="))
                    return stringPartTrimmed.Substring(8).Trim();
            }

            return string.Empty;
        }

        private void btnLogo_Click(object sender, RoutedEventArgs e)
        {
            WebBrowserTask t = new WebBrowserTask();
            t.URL = "http://komodex.com";
            t.Show();
        }

    }
}