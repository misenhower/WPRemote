﻿using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using System.IO.IsolatedStorage;
using System.IO;
using System.Windows.Navigation;
using System.Text;
using Komodex.WP7DACPRemote.DACPServerManagement;
using Komodex.DACP;

namespace Komodex.WP7DACPRemote.Utilities
{
    // CrashReporter
    // Matt Isenhower, Komodex Systems LLC
    // http://blog.ike.to
    // Inspired by:
    // http://blogs.msdn.com/b/andypennell/archive/2010/11/01/error-reporting-on-windows-phone-7.aspx

    public static class CrashReporter
    {
        // Error Report URL parameters
        private const string ErrorReportURL = "http://sys.komodex.com/wp7/crashreporter/";
        private const string ProductName = "Remote";

        // The filename for crash reports in isolated storage 
        private const string ErrorLogFilename = "ApplicationErrorLog.log";

        private const string SeparatorDashes = "---------------------------------------------------------------------------";
        private static string ErrorLogContent = null;
        private static PhoneApplicationFrame RootFrame = null;
        private static bool IsObscured = false;
        private static bool IsLocked = false;

        public static void Initialize(PhoneApplicationFrame frame)
        {
            RootFrame = frame;

            // Hook into exception events
            App.Current.UnhandledException += new EventHandler<ApplicationUnhandledExceptionEventArgs>(App_UnhandledException);
            RootFrame.NavigationFailed += new NavigationFailedEventHandler(RootFrame_NavigationFailed);

            // Hook into obscured/unobscured events
            RootFrame.Obscured += new EventHandler<ObscuredEventArgs>(RootFrame_Obscured);
            RootFrame.Unobscured += new EventHandler(RootFrame_Unobscured);

            // Send previous log if it exists
            SendExceptionLog();
        }



        #region Events

        static void App_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            LogException(e.ExceptionObject, "Unhandled Exception");
        }

        static void RootFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            LogException(e.Exception, "Navigation Failed");
        }

        static void RootFrame_Obscured(object sender, ObscuredEventArgs e)
        {
            IsObscured = true;
            IsLocked = e.IsLocked;
        }

        static void RootFrame_Unobscured(object sender, EventArgs e)
        {
            IsObscured = false;
            IsLocked = false;
        }

        #endregion

        #region Methods

        private static void LogException(Exception e, string type = null)
        {
            try
            {
                using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    using (TextWriter writer = new StreamWriter(store.OpenFile(ErrorLogFilename, FileMode.Append)))
                    {
                        writer.WriteLine(SeparatorDashes);

                        // Error type
                        writer.WriteLine(type ?? "Application Error");

                        // Application name
                        writer.WriteLine("-> Product: " + ProductName);

                        // Application version
                        writer.Write("-> Version: " + Utility.ApplicationVersion);
#if DEBUG
                        writer.Write(" (Debug)");
#endif
                        writer.WriteLine();

                        // Date and time
                        writer.WriteLine("-> Date: " + DateTime.Now.ToString());

                        // Unique report ID
                        writer.WriteLine("-> Report ID: " + Guid.NewGuid().ToString());

                        writer.WriteLine(SeparatorDashes);

                        writer.WriteLine("-> OS Version: " + Environment.OSVersion.ToString());
                        writer.WriteLine("-> Framework: " + Environment.Version.ToString());
                        writer.WriteLine("-> Current page: " + RootFrame.CurrentSource);
                        writer.WriteLine("-> Obscured: " + ((IsObscured) ? "Yes" : "No"));
                        writer.WriteLine("-> Locked: " + ((IsLocked) ? "Yes" : "No"));

                        writer.WriteLine(SeparatorDashes);

                        // iTunes Information
                        writer.WriteLine("iTunes Information");
                        DACPServer server = DACPServerManager.Server;
                        if (server != null)
                        {
                            writer.Write("-> Connection status: ");
                            if (server.IsConnected)
                                writer.WriteLine("Connected");
                            else
                                writer.WriteLine("Disconnected");

                            writer.WriteLine("-> Version: " + server.ServerVersionString);
                            writer.WriteLine("-> Protocol: " + server.ServerVersion.ToString("x").ToUpper());
                            writer.WriteLine("-> DMAP: " + server.ServerDMAPVersion.ToString("x").ToUpper());
                            writer.WriteLine("-> DAAP: " + server.ServerDAAPVersion.ToString("x").ToUpper());
                        }
                        else
                        {
                            writer.WriteLine("-> No server connected");
                        }

                        writer.WriteLine(SeparatorDashes);

                        // Exception Info
                        writer.WriteLine("Exception Information:");
                        writer.WriteLine();
                        writer.WriteLine(e.ToString());
                    }
                }
            }
            catch { }
        }

        #endregion

        #region HTTP Request and Response

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
                        ErrorLogContent = reader.ReadToEnd();
                    }

                    if (ErrorLogContent == null)
                        return;

                    string url = ErrorReportURL + "?p=" + ProductName;
#if DEBUG
                    url += "&d=1";
#endif

                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
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

            string postData = "log=" + HttpUtility.UrlEncode(ErrorLogContent);

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
