using Komodex.Common;
using Komodex.HTTP;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking.Connectivity;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Komodex.TestApp.Http
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class HttpTestPage : Page
    {
        private HttpServer _httpServer;

        public HttpTestPage()
        {
            this.InitializeComponent();
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (_httpServer != null)
                return;

            LogPanel.Children.Clear();
            LogMessage("Starting HTTP server...");

            _httpServer = new HttpServer();
            _httpServer.RequestReceived += HttpServer_RequestReceived;

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

            StartButton.IsEnabled = false;
            StopButton.IsEnabled = true;
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (_httpServer == null)
                return;

            _httpServer.Stop();
            _httpServer = null;

            LogMessage("HTTP server stopped.");

            StartButton.IsEnabled = true;
            StopButton.IsEnabled = false;
        }

        private void LogMessage(string text)
        {
            ThreadUtility.RunOnUIThread(() =>
            {
                TextBlock textBlock = new TextBlock();
                textBlock.Text = text;
                LogPanel.Children.Add(textBlock);
                LogScrollViewer.ChangeView(null, LogScrollViewer.ScrollableHeight, null);
            });
        }

        private async void HttpServer_RequestReceived(object sender, HttpRequestEventArgs e)
        {
            LogMessage("Request: " + e.Request.Uri.AbsolutePath);

            HttpResponse response = new HttpResponse();
            //string format;
            using (StreamWriter writer = new StreamWriter(response.Body))
            {
                writer.WriteLine("<html><head><title>WP8 HTTP Server</title></head><body>");
                switch (e.Request.Uri.AbsolutePath)
                {
                    case "/":
                        writer.WriteLine("<h1>Welcome!</h1>");
                        writer.WriteLine("<ul>");
                        writer.WriteLine("<li><a href='device'>Device Information</a></li>");
                        writer.WriteLine("<li><a href='memory'>Memory Information</a></li>");
                        writer.WriteLine("<li><a href='date'>Current Date &amp; Time</a></li>");
                        writer.WriteLine("</ul>");
                        break;

                    case "/device":
                        writer.WriteLine("<h1>Device Information</h1>");
                        writer.WriteLine("<ul>");
                        writer.WriteLine("<li>TODO!</li>");
                        //format = "<li><strong>{0}:</strong> {1}</li>";
                        //writer.WriteLine(format, "OS Version", Environment.OSVersion);
                        //writer.WriteLine(format, "Device Manufacturer", DeviceStatus.DeviceManufacturer);
                        //writer.WriteLine(format, "Device Name", DeviceStatus.DeviceName);
                        //writer.WriteLine(format, "Hardware Version", DeviceStatus.DeviceHardwareVersion);
                        //writer.WriteLine(format, "Firmware Version", DeviceStatus.DeviceFirmwareVersion);
                        //writer.WriteLine(format, "Current Power Source", DeviceStatus.PowerSource);
                        writer.WriteLine("</ul>");
                        writer.WriteLine("<a href='/'>Home</a>");
                        break;

                    case "/memory":
                        writer.WriteLine("<h1>Memory Information</h1>");
                        writer.WriteLine("<ul>");
                        writer.WriteLine("<li>TODO!</li>");
                        //format = "<li><strong>{0}:</strong> {1:0.00} MB</li>";
                        //writer.WriteLine(format, "App Memory Usage", DeviceStatus.ApplicationCurrentMemoryUsage / 1024d / 1024d);
                        //writer.WriteLine(format, "App Peak Usage", DeviceStatus.ApplicationPeakMemoryUsage / 1024d / 1024d);
                        //writer.WriteLine(format, "App Memory Limit", DeviceStatus.ApplicationMemoryUsageLimit / 1024d / 1024d);
                        //writer.WriteLine(format, "Device Total Memory", DeviceStatus.DeviceTotalMemory / 1024d / 1024d);
                        writer.WriteLine("</ul>");
                        writer.WriteLine("<a href='/'>Home</a>");
                        break;

                    case "/date":
                        writer.WriteLine("<h1>Current Date &amp; Time</h1>");
                        writer.WriteLine(DateTime.Now.ToString() + "<br /><br />");
                        writer.WriteLine("<a href='/'>Home</a>");
                        break;

                    default:
                        response.StatusCode = HttpStatusCode.NotFound;
                        writer.WriteLine("<h1>404: Not found</h1>");
                        writer.WriteLine("<a href='/'>Home</a>");
                        break;
                }

                writer.WriteLine("</body></html>");
                writer.Flush();
                await e.Request.SendResponse(response);
            }
        }
    }
}
