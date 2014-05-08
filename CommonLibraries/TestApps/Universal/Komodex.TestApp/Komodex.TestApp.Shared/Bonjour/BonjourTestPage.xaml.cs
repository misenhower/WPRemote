using Komodex.Bonjour;
using Komodex.Common;
using Komodex.TestApp.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Komodex.TestApp.Bonjour
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class BonjourTestPage : Page
    {
        public BonjourTestPage()
        {
            this.InitializeComponent();

            StopPublishButton.IsEnabled = false;
            StopBrowseButton.IsEnabled = false;

            // Set up the net service
            _netService = new NetService();
            _netService.FullServiceInstanceName = "test-01._touch-remote._tcp.local.";
            _netService.Port = 1234;
            _netService.Hostname = "this-is-a-test.local.";
            _netService.IPAddresses.Add("127.0.0.1");
            Dictionary<string, string> txt = new Dictionary<string, string>();
            txt.Add("DvNm", "Windows Device");
            txt.Add("RemV", "10000");
            txt.Add("DvTy", "iPhone");
            txt.Add("RemN", "Remote");
            txt.Add("txtvers", "1");
            txt.Add("Pair", "1111111111111111");
            _netService.TXTRecordData = txt;
        }

        private readonly ObservableDictionary _pageViewSource = new ObservableDictionary();
        public ObservableDictionary PageViewSource
        {
            get { return _pageViewSource; }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            StopServicePublisher();
            StopServiceBrowser();
        }

        #region Service Publisher

        private NetService _netService;

        private void StartPublishButton_Click(object sender, RoutedEventArgs e)
        {
            StartServicePublisher();
        }

        private void StopPublishButton_Click(object sender, RoutedEventArgs e)
        {
            StopServicePublisher();
        }

        private void StartServicePublisher()
        {
            StartPublishButton.IsEnabled = false;
            _netService.Publish();
            StopPublishButton.IsEnabled = true;
        }

        private void StopServicePublisher()
        {
            StopPublishButton.IsEnabled = false;
            _netService.StopPublishing();
            StartPublishButton.IsEnabled = true;
        }

        #endregion

        #region Service Browser

        private NetServiceBrowser _browser;
        private ObservableCollectionEx<string> _discoveredServices;

        private void StartBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            StartServiceBrowser();
        }

        private void StopBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            StopServiceBrowser();
        }

        private void StartServiceBrowser()
        {
            StartBrowseButton.IsEnabled = false;

            _discoveredServices = new ObservableCollectionEx<string>();
            PageViewSource["DiscoveredServices"] = _discoveredServices;

            _browser = new NetServiceBrowser();
            _browser.ServiceFound += _browser_ServiceFound;
            _browser.ServiceRemoved += _browser_ServiceRemoved;
            _browser.SearchForServices("_touch-able._tcp.local");

            StopBrowseButton.IsEnabled = true;
        }

        private void StopServiceBrowser()
        {
            StopBrowseButton.IsEnabled = false;

            if (_browser != null)
            {
                _browser.ServiceFound -= _browser_ServiceFound;
                _browser.ServiceRemoved -= _browser_ServiceRemoved;
                _browser.Stop();
            }

            _discoveredServices = null;
            PageViewSource["DiscoveredServices"] = null;

            StartBrowseButton.IsEnabled = true;
        }

        private async void _browser_ServiceFound(object sender, NetServiceEventArgs e)
        {
            ThreadUtility.RunOnUIThread(() =>
            {
                _discoveredServices.Add(e.Service.Name);
            });
            await e.Service.ResolveAsync();
        }

        private void _browser_ServiceRemoved(object sender, NetServiceEventArgs e)
        {
            _discoveredServices.Remove(e.Service.Name);
        }

        #endregion
    }
}
