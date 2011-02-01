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
using System.IO.IsolatedStorage;

namespace Komodex.WP7DACPRemote
{
    public static class FirstRunNotifier
    {
        // Notification URL parameters
        private const string NotificationURL = "http://sys.komodex.com/wp7/notify/";
        private const string ProductName = "remote";

        // Isolated storage key
        private const string FirstRunKey = "FirstRunCompleted";
        private static readonly IsolatedStorageSettings isolatedSettings = IsolatedStorageSettings.ApplicationSettings;

        public static void CheckFirstRun()
        {
            if (GetPreviousVersion() != Utility.ApplicationVersion)
                SendFirstRunNotification();
        }

        private static string GetPreviousVersion()
        {
            if (isolatedSettings.Contains(FirstRunKey))
                return isolatedSettings[FirstRunKey] as string;
            return null;
        }

        #region HTTP Request and Response

        private static void SendFirstRunNotification()
        {
            // Build the URL
            string url = NotificationURL + "?p=" + ProductName + "&v=" + Utility.ApplicationVersion;

            if (!isolatedSettings.Contains(FirstRunKey))
                url += "&t=n";
            else
            {
                url += "&t=u";
                string previousVersion = GetPreviousVersion();
                if (!string.IsNullOrEmpty(previousVersion))
                    url += "&pv=" + previousVersion;
            }

#if DEBUG
            url += "&d=1";
#endif

            // Submit the HTTP request
            try
            {
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
                webRequest.Method = "POST";
                webRequest.BeginGetResponse(new AsyncCallback(HandleResponse), webRequest);
            }
            catch { }
        }

        private static void HandleResponse(IAsyncResult result)
        {
            HttpWebRequest webRequest = (HttpWebRequest)result.AsyncState;

            try
            {
                WebResponse response = webRequest.EndGetResponse(result);
                response.GetResponseStream();

                // Update the Isolated Storage settings
                isolatedSettings[FirstRunKey] = Utility.ApplicationVersion;
                isolatedSettings.Save();
            }
            catch { }
        }

        #endregion
    }
}
