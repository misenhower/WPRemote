using Komodex.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Windows;

namespace Komodex.Analytics
{
    public static class CrashReporter
    {
        // Error Report URL parameters
        private const string ErrorReportURL = "http://sys.komodex.com/wp7/crashreporter/";

        // The filename for crash reports in isolated storage 
        private const string ErrorLogFilename = "ApplicationErrorLog.log";

        private static readonly string LargeDashes = new string('=', 80);
        private static readonly string SmallDashes = new string('-', 80);

        private static bool _initialized = false;
        /// <summary>
        /// Initializes the crash reporter. Unless this is a background agent, use
        /// PhoneAppCrashReporter.Initialize(application, rootFrame) instead.
        /// </summary>
        /// <param name="application"></param>
#if WINDOWS_PHONE
        public static void Initialize(Application application)
#else
        public static void Initialize(Windows.UI.Xaml.Application application)
#endif
        {
            if (_initialized)
                return;

            // Hook into application unhandled exceptions
#if WINDOWS_PHONE
            application.UnhandledException += (sender, e) => { LogException(e.ExceptionObject, "Unhandled Exception"); };
#else
            application.UnhandledException += (sender, e) => { LogMessage(e.Message, "Unhandled Exception"); };
#endif

            // Send previous log if it exists
            SendExceptionLog();

            _initialized = true;
        }

        #region Properties

        private static string ErrorReportPostURL
        {
            get
            {

                string url = ErrorReportURL + "?p=" + Utility.ApplicationName + "&v=" + Utility.ApplicationVersion;
#if DEBUG
                url += "&d=1";
#endif
                return url;
            }
        }

        #endregion

        #region Methods

        public static void LogException(Exception e, string type = null, bool sendImmediately = false)
        {
            // Exception Info
            string exceptionInfo = "Exception Information:\r\n";
            exceptionInfo += e.ToString();

            LogMessage(exceptionInfo, type, sendImmediately);
        }

        public static void LogMessage(string message, string type = null, bool sendImmediately = false)
        {
            try
            {
                StringBuilder stringBuilder = new StringBuilder();
                using (StringWriter writer = new StringWriter(stringBuilder))
                {
                    writer.WriteLine(LargeDashes);

                    // Error type
                    writer.WriteLine(type ?? "Application Error");

                    // Application name
                    writer.WriteLine("-> Product: " + Utility.ApplicationName);

                    // Application version
                    writer.Write("-> Version: " + Utility.ApplicationVersion);
#if DEBUG
                    writer.Write(" (Debug)");
#endif
                    writer.WriteLine();

#if WP7
                    writer.WriteLine("-> Target OS: WP7");
#endif
#if WP8
                    writer.WriteLine("-> Target OS: WP8");
#endif

                    writer.WriteLine("-> Install date: " + FirstRunNotifier.FirstRunDate);

                    // Date and time
                    writer.WriteLine("-> Date: " + DateTime.Now.ToString());

                    // Unique report ID
                    writer.WriteLine("-> Report ID: " + Guid.NewGuid().ToString());

                    writer.WriteLine(SmallDashes);

#if WINDOWS_PHONE
                    writer.WriteLine("-> OS Version: {0} ({1})", Environment.OSVersion, Microsoft.Devices.Environment.DeviceType);
                    writer.WriteLine("-> Framework: " + Environment.Version.ToString());
#endif
                    writer.WriteLine("-> Culture: " + CultureInfo.CurrentCulture);

#if WINDOWS_PHONE
                    writer.WriteLine(SmallDashes);

                    writer.WriteLine("-> Device Manufacturer: " + Microsoft.Phone.Info.DeviceStatus.DeviceManufacturer);
                    writer.WriteLine("-> Device Name: " + Microsoft.Phone.Info.DeviceStatus.DeviceName);
                    writer.WriteLine("-> Device Hardware Version: " + Microsoft.Phone.Info.DeviceStatus.DeviceHardwareVersion);
                    writer.WriteLine("-> Device Firmware Version: " + Microsoft.Phone.Info.DeviceStatus.DeviceFirmwareVersion);

                    writer.WriteLine(SmallDashes);

                    // Memory usage
                    var used = Utility.ReadableFilesize(Microsoft.Phone.Info.DeviceStatus.ApplicationCurrentMemoryUsage);
                    var peak = Utility.ReadableFilesize(Microsoft.Phone.Info.DeviceStatus.ApplicationPeakMemoryUsage);
                    writer.WriteLine("-> Memory Usage: {0} ({1} peak)", used, peak);
                    var available = Utility.ReadableFilesize(Microsoft.Phone.Info.DeviceStatus.ApplicationMemoryUsageLimit);
                    var total = Utility.ReadableFilesize(Microsoft.Phone.Info.DeviceStatus.DeviceTotalMemory);
                    writer.WriteLine("-> Memory Availability: {0} of {1}", available, total);
#endif

                    foreach (var infoCallback in AdditionalLogInfoCallbacks)
                    {
                        try
                        {
                            writer.WriteLine(SmallDashes);
                            infoCallback(writer);
                        }
                        catch { }
                    }

                    writer.WriteLine(SmallDashes);

                    // Message Body
                    writer.WriteLine(message);

                    writer.WriteLine();
                }

                WriteLog(stringBuilder.ToString());
            }


            catch { }

            if (sendImmediately)
                SendExceptionLog();
        }

#if WINDOWS_PHONE
        private static void WriteLog(string value)
        {
            using (var store = System.IO.IsolatedStorage.IsolatedStorageFile.GetUserStoreForApplication())
            using (TextWriter writer = new StreamWriter(store.OpenFile(ErrorLogFilename, FileMode.Append)))
            {
                writer.WriteLine(value);
            }
        }
#else
        private static void WriteLog(string value)
        {
            Windows.Storage.ApplicationData.Current.LocalFolder.CreateFileAsync(ErrorLogFilename, Windows.Storage.CreationCollisionOption.OpenIfExists).AsTask()
                .ContinueWith((fileTask) => Windows.Storage.FileIO.AppendTextAsync(fileTask.Result, value).AsTask().Wait()).Wait();
        }
#endif

        #endregion

        #region Log Info Hooks

        private static List<Action<TextWriter>> _additionalLogInfoCallbacks = new List<Action<TextWriter>>();
        public static List<Action<TextWriter>> AdditionalLogInfoCallbacks { get { return _additionalLogInfoCallbacks; } }

        #endregion

        #region HTTP Request and Response

#if WINDOWS_PHONE
        private static string _errorLogContent;

        private static void SendExceptionLog()
        {
            try
            {
                using (var store = System.IO.IsolatedStorage.IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (!store.FileExists(ErrorLogFilename))
                        return;

                    using (TextReader reader = new StreamReader(store.OpenFile(ErrorLogFilename, FileMode.Open, FileAccess.Read, FileShare.None)))
                    {
                        _errorLogContent = reader.ReadToEnd();
                    }
                }

                if (_errorLogContent == null)
                    return;

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(ErrorReportPostURL);
                Utility.PrepareHttpWebRequest(request);
                request.ContentType = "application/x-www-form-urlencoded";
                request.Method = "POST";

                request.BeginGetRequestStream(new AsyncCallback(GetRequestStringCallback), request);
            }
            catch { }
        }

        private static void GetRequestStringCallback(IAsyncResult asyncResult)
        {
            HttpWebRequest request = (HttpWebRequest)asyncResult.AsyncState;

            string postData = "log=" + HttpUtility.UrlEncode(_errorLogContent);

            byte[] bytes = Encoding.UTF8.GetBytes(postData);

            try
            {
                Stream postStream = request.EndGetRequestStream(asyncResult);
                postStream.Write(bytes, 0, bytes.Length);
                postStream.Close();

                request.BeginGetResponse(new AsyncCallback(GetResponseCallback), request);
            }
            catch { }
        }

        private static void GetResponseCallback(IAsyncResult asyncResult)
        {
            HttpWebRequest request = (HttpWebRequest)asyncResult.AsyncState;

            try
            {
                HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(asyncResult);
                Stream streamResponse = response.GetResponseStream();
                StreamReader reader = new StreamReader(streamResponse);
                string responseString = reader.ReadToEnd();

                streamResponse.Close();
                reader.Close();
                response.Close();

                // Delete the log file
                using (var store = System.IO.IsolatedStorage.IsolatedStorageFile.GetUserStoreForApplication())
                {
                    store.DeleteFile(ErrorLogFilename);
                }
            }
            catch { }
        }
#else
        private static async void SendExceptionLog()
        {
            try
            {
                var appFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
                var file = await appFolder.GetFileAsync(ErrorLogFilename);
                string errorLogContent = await Windows.Storage.FileIO.ReadTextAsync(file);

                if (errorLogContent == null)
                    return;

                List<KeyValuePair<string, string>> postData = new List<KeyValuePair<string, string>>();
                postData.Add(new KeyValuePair<string, string>("log", errorLogContent));


                using (var httpClient = new System.Net.Http.HttpClient())
                using (var httpContent = new System.Net.Http.FormUrlEncodedContent(postData))
                using (var response = await httpClient.PostAsync(ErrorReportPostURL, httpContent))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        await file.DeleteAsync();
                    }
                }
            }
            catch { }

        }
#endif

        #endregion
    }
}
