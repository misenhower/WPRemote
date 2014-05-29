using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;

namespace Komodex.Common.Analytics
{
    public static class CrashReporter
    {
        private static readonly Log _log = new Log("Crash Reporter");
        private const string ReportFolderName = "CrashReports";
        private const string ReportUri = "http://sys.komodex.com/appcrashreporter/";

        private static readonly string LargeDashes = new string('=', 80);
        private static readonly string SmallDashes = new string('-', 80);

        private static bool _initialized;

        public static async void Initialize()
        {
            if (_initialized)
                return;
            _initialized = true;

            _log.Debug("Initializing...");

            Application.Current.UnhandledException += Application_UnhandledException;
            ((Frame)Window.Current.Content).NavigationFailed += Frame_NavigationFailed;

            await SendReportsAsync().ConfigureAwait(false);
        }

        private static void Application_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            LogMessageAsync(e.Message, "Unhandled Exception").Wait();
        }

        private static void Frame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            string message = "Source page type: ";
            if (e.SourcePageType != null)
                message += e.SourcePageType.ToString();
            else
                message += "(null)";

            message += Environment.NewLine + e.Exception.ToString();

            LogMessageAsync(message, "Navigation Failed").Wait();
        }

        public static Task LogExceptionAsync(Exception e, string type = null, bool sendImmediately = false)
        {
            string message = "Exception type: " + e.GetType().Name + Environment.NewLine + e.Message;

            return LogMessageAsync(message, type, sendImmediately);
        }

        public static async Task LogMessageAsync(string message, string type = null, bool sendImmediately = false)
        {
            Guid guid = Guid.NewGuid();

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(LargeDashes);

            // Error type
            sb.AppendLine(type ?? "Application Error");

            // Application name
            sb.AppendLine("-> Product: " + AppInfo.DisplayName);

            // Application version
            sb.Append("-> Version: " + AppInfo.Version);
#if DEBUG
            sb.Append(" (Debug)");
#endif
            sb.AppendLine();

            // Installation date
            sb.AppendLine("-> Installation Date: " + AppInfo.InstallationDate);

            // Current date and time
            sb.AppendLine("-> Date: " + DateTimeOffset.Now);

            // Current culture
            sb.AppendLine("-> Culture: " + CultureInfo.CurrentCulture.EnglishName);

            // Unique report ID
            sb.AppendLine("-> Report ID: " + guid);

            sb.AppendLine(SmallDashes);

            // Device info
            sb.AppendLine("-> Device Type: " + DeviceInfo.Type);
            sb.AppendLine("-> Device Manufacturer: " + DeviceInfo.SystemManufacturer);
            sb.AppendLine("-> Device Model: " + DeviceInfo.SystemProductName);
            sb.AppendLine("-> Device SKU: " + DeviceInfo.SystemSku);
            sb.AppendLine("-> Device Has Touch: " + (DeviceInfo.HasTouch ? "Yes" : "No"));
            sb.AppendLine("-> Device Friendly Name: " + DeviceInfo.FriendlyName);

            sb.AppendLine(SmallDashes);

            sb.AppendLine(message);

            // Write to file
            var file = await ApplicationData.Current.LocalFolder.CreateFileAsync(string.Format(@"{0}\{1}.txt", ReportFolderName, guid), CreationCollisionOption.ReplaceExisting).AsTask().ConfigureAwait(false);
            await FileIO.WriteTextAsync(file, sb.ToString()).AsTask().ConfigureAwait(false);

            // Send if necessary
            if (sendImmediately)
                await SendReportsAsync();
        }

        public static async Task SendReportsAsync()
        {
            // Look for reports
            try
            {
                var folder = await ApplicationData.Current.LocalFolder.GetFolderAsync(ReportFolderName).AsTask().ConfigureAwait(false);
                var files = await folder.GetFilesAsync().AsTask().ConfigureAwait(false);
                foreach (var file in files)
                {
                    _log.Trace("Sending issue report: " + file.Name);
                    string content = await FileIO.ReadTextAsync(file).AsTask().ConfigureAwait(false);
                    bool success = await SendReportContentAsync(content).ConfigureAwait(false);
                    if (success)
                    {
                        _log.Trace("Successfully sent issue report, deleting file: " + file.Name);
                        await file.DeleteAsync().AsTask().ConfigureAwait(false);
                    }
                    else
                    {
                        _log.Error("Couldn't send issue report: " + file.Name);
                    }
                }
            }
            catch (FileNotFoundException)
            {
                // No issue reports, nothing to do
            }
            catch (Exception e)
            {
                _log.Error("Caught exception: " + e.ToString());
            }
        }

        private static async Task<bool> SendReportContentAsync(string content)
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders["User-Agent"] = AppInfo.UserAgent;
            var postData = new List<KeyValuePair<string, string>>();
            postData.Add("log", content);

            string uri = string.Format("{0}?p={1}&v={2}", ReportUri, AppInfo.DisplayName, AppInfo.Version);
#if DEBUG
            uri += "&d=1";
#endif

            try
            {
                var response = await client.PostAsync(new Uri(uri), new HttpFormUrlEncodedContent(postData)).AsTask().ConfigureAwait(false);
                _log.Trace("Response status code: {0} ({1})", (int)response.StatusCode, response.StatusCode);
                return response.IsSuccessStatusCode;
            }
            catch { }

            return false;
        }
    }
}
