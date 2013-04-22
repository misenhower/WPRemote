using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Komodex.HTTP;
using Windows.Networking.Connectivity;

namespace Komodex.CommonLibrariesTestApp.HTTP
{
    public partial class HttpTestPage : PhoneApplicationPage
    {
        protected HttpServer _httpServer;

        public HttpTestPage()
        {
            InitializeComponent();
        }

        protected void LogMessage(string text)
        {
            TextBlock textBlock = new TextBlock();
            textBlock.Text = text;
            logPanel.Children.Add(textBlock);
            logScrollViewer.ScrollToVerticalOffset(logScrollViewer.ScrollableHeight);
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (_httpServer != null)
                return;

            LogMessage("Starting HTTP server...");

            _httpServer = new HttpServer();

            try
            {
                await _httpServer.Start();
            }
            catch (Exception ex)
            {
                LogMessage("Error: " + ex.ToString());
                _httpServer = null;
                return;
            }

            var hostList = NetworkInformation.GetHostNames();
            string hosts = string.Join(", ", hostList.Select(h => h.DisplayName));
            LogMessage("Listening on hosts: " + hosts);

            startButton.IsEnabled = false;
            stopButton.IsEnabled = true;
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (_httpServer == null)
                return;

            _httpServer.Stop();
            _httpServer = null;

            LogMessage("HTTP server stopped.");

            startButton.IsEnabled = true;
            stopButton.IsEnabled = false;
        }
    }
}