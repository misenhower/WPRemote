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
using System.Collections.ObjectModel;
using Komodex.Bonjour;
using Komodex.Common;
using Komodex.Common.Phone;

namespace Komodex.CommonLibrariesTestApp.Bonjour
{
    public partial class BonjourTests : PhoneApplicationBasePage
    {
        public BonjourTests()
        {
            InitializeComponent();

            servicesListBox.ItemsSource = _services;

            publishStopButton.IsEnabled = false;
            browseStopButton.IsEnabled = false;

            // Set up the net service
            _netService = new NetService();
            _netService.FullServiceInstanceName = "test-01._touch-remote._tcp.local.";
            _netService.Port = 1234;
            _netService.Hostname = "this-is-a-test.local.";
            _netService.IPAddresses.Add(IPAddress.Parse("127.0.0.1"));
            Dictionary<string, string> txt = new Dictionary<string, string>();
            txt.Add("DvNm", "Windows Phone 7 Device");
            txt.Add("RemV", "10000");
            txt.Add("DvTy", "iPhone");
            txt.Add("RemN", "Remote");
            txt.Add("txtvers", "1");
            txt.Add("Pair", "1111111111111111");
            _netService.TXTRecordData = txt;

        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            StopServicePublisher();
            StopServiceBrowser();
        }

        #region Service Publisher

        protected NetService _netService;

        private void publishStartButton_Click(object sender, RoutedEventArgs e)
        {
            StartServicePublisher();
        }

        private void StartServicePublisher()
        {
            _netService.Publish();

            publishStartButton.IsEnabled = false;
            publishStopButton.IsEnabled = true;
        }

        private void publishStopButton_Click(object sender, RoutedEventArgs e)
        {
            StopServicePublisher();
        }

        private void StopServicePublisher()
        {
            _netService.StopPublishing();

            publishStartButton.IsEnabled = true;
            publishStopButton.IsEnabled = false;
        }

        #endregion

        #region Service Browser

        protected ObservableCollection<NetServiceViewModel> _services = new ObservableCollection<NetServiceViewModel>();
        protected NetServiceBrowser _browser;

        private void browseStartButton_Click(object sender, RoutedEventArgs e)
        {
            StartServiceBrowser();
        }

        private void StartServiceBrowser()
        {
            _browser = new NetServiceBrowser();
            _browser.ServiceFound += _browser_ServiceFound;
            _browser.ServiceRemoved += _browser_ServiceRemoved;
            _browser.SearchForServices("_touch-able._tcp.local.");

            browseStartButton.IsEnabled = false;
            browseStopButton.IsEnabled = true;
        }

        private void browseStopButton_Click(object sender, RoutedEventArgs e)
        {
            StopServiceBrowser();
        }

        private void StopServiceBrowser()
        {
            if (_browser == null)
                return;

            _browser.ServiceFound -= _browser_ServiceFound;
            _browser.ServiceRemoved -= _browser_ServiceRemoved;
            _browser.Stop();
            _services.Clear();

            browseStartButton.IsEnabled = true;
            browseStopButton.IsEnabled = false;
        }

        void _browser_ServiceFound(object sender, NetServiceEventArgs e)
        {
            Utility.BeginInvokeOnUIThread(async () =>
            {
                if (sender != _browser || !_browser.IsRunning)
                    return; // Don't attempt to resolve the service if we're no longer looking for services (or if this is the wrong browser)

                var nsvm = new NetServiceViewModel(e.Service);
                _services.Add(nsvm);
                nsvm.Resolved = "Resolving...";
                bool resolved = await e.Service.ResolveAsync();
                nsvm.Resolved = "Resolved: " + resolved;
                nsvm.Refresh();
            });
        }

        void _browser_ServiceRemoved(object sender, NetServiceEventArgs e)
        {
            Utility.BeginInvokeOnUIThread(() =>
            {
                var nsvm = _services.FirstOrDefault(s => s.Service == e.Service);
                if (nsvm != null)
                    _services.Remove(nsvm);
            });
        }

        #endregion
    }
}