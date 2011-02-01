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
        private static IsolatedStorageSettings isolatedSettings = IsolatedStorageSettings.ApplicationSettings;

        private static readonly string kFirstRunKey = "FirstRunCompleted";

        public static void CheckFirstRun()
        {
            if (isolatedSettings.Contains(kFirstRunKey))
            {
                string previousVersion = isolatedSettings[kFirstRunKey] as string;
                if (previousVersion != Utility.ApplicationVersion)
                    SendFirstRunNotification();
            }
            else
            {
                SendFirstRunNotification();
            }
        }

        private static void SendFirstRunNotification()
        {
            string version = Utility.ApplicationVersion;
            string url = "http://sys.komodex.com/wp7/notify/?p=remote&v=" + version;

            if (!isolatedSettings.Contains(kFirstRunKey))
                url += "&t=n";
            else
            {
                url += "&t=u";
                if (isolatedSettings[kFirstRunKey] is string)
                    url += "&pv=" + (string)isolatedSettings[kFirstRunKey];
            }

#if DEBUG
            url += "&d=1";
            // TODO: Could also include device info
#endif
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

                isolatedSettings[kFirstRunKey] = Utility.ApplicationVersion;
                isolatedSettings.Save();
            }
            catch { }
        }
    }
}
