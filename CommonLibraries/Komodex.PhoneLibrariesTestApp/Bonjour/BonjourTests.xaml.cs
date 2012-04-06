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
using System.Collections.ObjectModel;
using Komodex.Bonjour;
using Komodex.Common;

namespace Komodex.PhoneLibrariesTestApp.Bonjour
{
    public partial class BonjourTests : PhoneApplicationPage
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

        #region Service Publisher

        protected NetService _netService;

        private void publishStartButton_Click(object sender, RoutedEventArgs e)
        {
            _netService.Publish();

            publishStartButton.IsEnabled = false;
            publishStopButton.IsEnabled = true;
        }

        private void publishStopButton_Click(object sender, RoutedEventArgs e)
        {
            _netService.Stop();

            publishStartButton.IsEnabled = true;
            publishStopButton.IsEnabled = false;
        }

        #endregion

        #region Service Browser

        protected ObservableCollection<NetServiceViewModel> _services = new ObservableCollection<NetServiceViewModel>();
        protected NetServiceBrowser _browser;

        private void browseStartButton_Click(object sender, RoutedEventArgs e)
        {
            _browser = new NetServiceBrowser();
            _browser.ServiceFound += new EventHandler<NetServiceEventArgs>(_browser_ServiceFound);
            _browser.ServiceRemoved += new EventHandler<NetServiceEventArgs>(_browser_ServiceRemoved);
            _browser.SearchForServices("_touch-able._tcp.local.");

            browseStartButton.IsEnabled = false;
            browseStopButton.IsEnabled = true;
        }

        private void browseStopButton_Click(object sender, RoutedEventArgs e)
        {
            _browser.Stop();
            _services.Clear();

            browseStartButton.IsEnabled = true;
            browseStopButton.IsEnabled = false;
        }

        void _browser_ServiceFound(object sender, NetServiceEventArgs e)
        {
            Utility.BeginInvokeOnUIThread(() =>
            {
                var nsvm = new NetServiceViewModel(e.Service);
                _services.Add(nsvm);
                e.Service.Resolve();
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