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
            tbAppVersion.Text = "Version: " + Utility.GetApplicationVersion();
#if DEBUG
            tbAppVersion.Text += " (Debug)";
#endif

            // iTunes information
            DACPServer server = DACPServerManager.Server;

            if (NavigationContext.QueryString.ContainsKey("version"))
            {
                tbiTunesNotConnected.Visibility = System.Windows.Visibility.Visible;
                gridiTunesInfo.Visibility = System.Windows.Visibility.Visible;

                tbiTunesNotConnected.Text = "From last connection attempt";

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
            else if (server != null && server.IsConnected)
            {
                tbiTunesNotConnected.Visibility = System.Windows.Visibility.Collapsed;
                gridiTunesInfo.Visibility = System.Windows.Visibility.Visible;

                tbiTunesVersion.Text = server.ServerVersionString;
                tbiTunesProtocolVersion.Text = server.ServerVersion.ToString("x").ToUpper();
                tbiTunesDMAPVersion.Text = server.ServerDMAPVersion.ToString("x").ToUpper();
                tbiTunesDAAPVersion.Text = server.ServerDAAPVersion.ToString("x").ToUpper();
            }
            else
            {
                tbiTunesNotConnected.Visibility = System.Windows.Visibility.Visible;
                gridiTunesInfo.Visibility = System.Windows.Visibility.Collapsed;
            }

            if (string.IsNullOrEmpty(tbiTunesVersion.Text))
                tbiTunesVersion.Text = "N/A";

#if DEBUG
            // Device information
            tbManufacturer.Text = DeviceInfo.DeviceManufacturer;
            tbDevice.Text = DeviceInfo.DeviceName;
            tbFirmware.Text = DeviceInfo.DeviceFirmwareVersion;
            tbHardwareVersion.Text = DeviceInfo.DeviceHardwareVersion;
#endif

        }

        private void btnLogo_Click(object sender, RoutedEventArgs e)
        {
            WebBrowserTask t = new WebBrowserTask();
            t.URL = "http://komodex.com/wp7remote";
            t.Show();
        }

        int assemblyInfoCount = 25;
        private void btnAssemblyInfo_Click(object sender, RoutedEventArgs e)
        {
            if (assemblyInfoCount-- == 0)
            {
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
            t.Subject = "[Komodex] Remote v" + Utility.GetApplicationVersion() + " Feedback";
            t.Show();
        }

        private void btnTwitter_Click(object sender, RoutedEventArgs e)
        {
            WebBrowserTask t = new WebBrowserTask();
            t.URL = "http://twitter.com/WP7remote";
            t.Show();
        }

    }
}