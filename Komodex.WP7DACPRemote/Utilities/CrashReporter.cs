using System;
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
    // Inspired by:
    // http://blogs.msdn.com/b/andypennell/archive/2010/11/01/error-reporting-on-windows-phone-7.aspx

    public static class CrashReporter
    {
        private static readonly string ErrorLogFilename = "ApplicationErrorLog.log";
        private static readonly string SeparatorDashes = "---------------------------------------------------------------------------";

        private static string ErrorLogContent = null;

        public static void Initialize(PhoneApplicationFrame frame)
        {
            // Hook into exception events
            App.Current.UnhandledException += new EventHandler<ApplicationUnhandledExceptionEventArgs>(App_UnhandledException);
            frame.NavigationFailed += new NavigationFailedEventHandler(RootFrame_NavigationFailed);

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
                        writer.WriteLine("Application Exception");

                        // Application version
                        writer.Write("-> Version: " + Utility.GetApplicationVersion());
#if DEBUG
                        writer.Write(" (Debug)");
#endif
                        writer.WriteLine();

                        // Date and time
                        writer.WriteLine("-> " + DateTime.Now.ToString());

                        // Exception type
                        if (type != null)
                            writer.WriteLine("-> Type: " + type);

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

                            writer.WriteLine("-> Version: " + server.ServerVersion);
                            writer.WriteLine("-> DMAP: " + server.ServerDMAPVersion);
                            writer.WriteLine("-> DAAP: " + server.ServerDAAPVersion);
                        }
                        else
                        {
                            writer.WriteLine("-> No server connected");
                        }

                        writer.WriteLine(SeparatorDashes);

                        // Exception Info
                        writer.WriteLine("Exception Information:");
                        writer.WriteLine();
                        writer.WriteLine(e.Message);
                        writer.WriteLine(e.StackTrace);
                    }
                }
            }
            catch { }
        }

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

                    string url = "http://sys.komodex.com/wp7/troubleshooting/";

                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                    request.ContentType = "application/x-www-form-urlencoded";
                    request.Method = "POST";

                    request.BeginGetRequestStream(new AsyncCallback(GetRequestStringCallback), request);
                }
            }
            catch { }
        }

        #endregion

        #region HTTP Handlers

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
