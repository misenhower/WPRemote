using System;
using System.Collections.Generic;
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
    public static class IssueReporter
    {
        private static readonly Log _log = new Log("Issue Reporter");
        private const string ReportFolderName = "IssueReports";
        private const string ReportUri = "http://sys.komodex.com/appissuereporter/";

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
            var report = IssueReport.Create(e.Exception);
            report.Type = "Unhandled Exception";
            report.MessageBody = e.Message;

            LogIssueReportAsync(report).Wait();
        }

        private static void Frame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            var report = IssueReport.Create(e.Exception);
            report.Type = "Navigation Failed";
            if (e.SourcePageType != null)
                report.MessageBody = "Source page type: " + e.SourcePageType.ToString();
            else
                report.MessageBody = "Source page type: (null)";

            LogIssueReportAsync(report).Wait();
        }

        public static async Task LogIssueReportAsync(IssueReport report, bool sendImmediately = false)
        {
            var file = await ApplicationData.Current.LocalFolder.CreateFileAsync(string.Format(@"{0}\{1}.xml", ReportFolderName, report.ID), CreationCollisionOption.ReplaceExisting).AsTask().ConfigureAwait(false);
            using (var outputStream = await file.OpenStreamForWriteAsync().ConfigureAwait(false))
            {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(IssueReport));
                xmlSerializer.Serialize(outputStream, report);
                await outputStream.FlushAsync().ConfigureAwait(false);
            }

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
                    var stream = await file.OpenReadAsync();
                    bool success = await SendReportContentAsync(stream);
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

        private static async Task<bool> SendReportContentAsync(IInputStream stream)
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders["User-Agent"] = AppInfo.UserAgent;
            try
            {
                var response = await client.PostAsync(new Uri(ReportUri), new HttpStreamContent(stream)).AsTask().ConfigureAwait(false);
                _log.Trace("Response status code: {0} ({1})", (int)response.StatusCode, response.StatusCode);
                return response.IsSuccessStatusCode;
            }
            catch { }

            return false;
        }
    }
}
