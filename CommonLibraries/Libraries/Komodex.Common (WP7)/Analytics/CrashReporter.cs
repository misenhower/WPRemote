using Komodex.Common;
using Microsoft.Phone.Info;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.IsolatedStorage;
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
        public static void Initialize(Application application)
        {
            if (_initialized)
                return;

            // Hook into application unhandled exceptions
            application.UnhandledException += App_UnhandledException;

            // Send previous log if it exists
            SendExceptionLog();

            _initialized = true;
        }

        #region Events

        private static void App_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            LogException(e.ExceptionObject, "Unhandled Exception");
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
                using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    using (TextWriter writer = new StreamWriter(store.OpenFile(ErrorLogFilename, FileMode.Append)))
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

                        writer.WriteLine("-> Install date: " + FirstRunNotifier.FirstRunDate);

                        // Date and time
                        writer.WriteLine("-> Date: " + DateTime.Now.ToString());

                        // Unique report ID
                        writer.WriteLine("-> Report ID: " + Guid.NewGuid().ToString());

                        writer.WriteLine(SmallDashes);

                        writer.WriteLine("-> OS Version: {0} ({1})", Environment.OSVersion, Microsoft.Devices.Environment.DeviceType);
                        writer.WriteLine("-> Framework: " + Environment.Version.ToString());
                        writer.WriteLine("-> Culture: " + CultureInfo.CurrentCulture);

                        writer.WriteLine(SmallDashes);

                        writer.WriteLine("-> Device Manufacturer: " + DeviceStatus.DeviceManufacturer);
                        writer.WriteLine("-> Device Name: " + DeviceStatus.DeviceName);
                        writer.WriteLine("-> Device Hardware Version: " + DeviceStatus.DeviceHardwareVersion);
                        writer.WriteLine("-> Device Firmware Version: " + DeviceStatus.DeviceFirmwareVersion);

                        writer.WriteLine(SmallDashes);

                        // Memory usage
                        var used = Utility.ReadableFilesize(DeviceStatus.ApplicationCurrentMemoryUsage);
                        var peak = Utility.ReadableFilesize(DeviceStatus.ApplicationPeakMemoryUsage);
                        writer.WriteLine("-> Memory Usage: {0} ({1} peak)", used, peak);
                        var available = Utility.ReadableFilesize(DeviceStatus.ApplicationMemoryUsageLimit);
                        var total = Utility.ReadableFilesize(DeviceStatus.DeviceTotalMemory);
                        writer.WriteLine("-> Memory Availability: {0} of {1}", available, total);

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
                }
            }
            catch { }

            if (sendImmediately)
                SendExceptionLog();
        }

        #endregion

        #region Log Info Hooks

        private static List<Action<TextWriter>> _additionalLogInfoCallbacks = new List<Action<TextWriter>>();
        public static List<Action<TextWriter>> AdditionalLogInfoCallbacks { get { return _additionalLogInfoCallbacks; } }

        #endregion

        #region HTTP Request and Response

        private static string _errorLogContent;

        private static void SendExceptionLog()
        {
            try
            {
                using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (!store.FileExists(ErrorLogFilename))
                        return;

                    using (TextReader reader = new StreamReader(store.OpenFile(ErrorLogFilename, FileMode.Open, FileAccess.Read, FileShare.None)))
                    {
                        _errorLogContent = reader.ReadToEnd();
                    }

                    if (_errorLogContent == null)
                        return;

                    string url = ErrorReportURL + "?p=" + Utility.ApplicationName + "&v=" + Utility.ApplicationVersion;
#if DEBUG
                    url += "&d=1";
#endif

                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                    Utility.PrepareHttpWebRequest(request);
                    request.ContentType = "application/x-www-form-urlencoded";
                    request.Method = "POST";

                    request.BeginGetRequestStream(new AsyncCallback(GetRequestStringCallback), request);
                }
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
                using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    store.DeleteFile(ErrorLogFilename);
                }
            }
            catch { }
        }

        #endregion
    }
}
