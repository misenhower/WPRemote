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
                int iTunesVersion = int.Parse(queryString["version"]);
                int iTunesDMAPVersion = int.Parse(queryString["dmap"]);
                int iTunesDAAPVersion = int.Parse(queryString["daap"]);

                tbiTunesVersion.Text = iTunesVersion.ToString("x").ToUpper();
                tbiTunesDMAPVersion.Text = iTunesDMAPVersion.ToString("x").ToUpper();
                tbiTunesDAAPVersion.Text = iTunesDAAPVersion.ToString("x").ToUpper();
            }
            else if (server != null && server.IsConnected)
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

        private void btnLogo_Click(object sender, RoutedEventArgs e)
        {
            WebBrowserTask t = new WebBrowserTask();
            t.URL = "http://komodex.com";
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

    }
}