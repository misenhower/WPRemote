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
using Microsoft.Devices;

namespace Komodex.Common.Phone
{
    public static class FirstRunNotifier
    {
        // Notification URL
        private const string NotificationURL = "http://sys.komodex.com/wp7/notify/";

        // Isolated storage settings
        private const string FirstRunKey = "FirstRunCompleted";
        private const string FirstRunTrialKey = "FirstRunTrialMode";
        private static readonly IsolatedStorageSettings isolatedSettings = IsolatedStorageSettings.ApplicationSettings;

        private static readonly Setting<string> _previousVersion = new Setting<string>(FirstRunKey);
        private static string PreviousVersion
        {
            get { return _previousVersion.Value; }
            set
            {
                if (_previousVersion.Value == value)
                    return;
                _previousVersion.Value = value;
            }
        }

        private static readonly Setting<bool> _previousTrialMode = new Setting<bool>(FirstRunTrialKey);
        internal static bool PreviousTrialMode
        {
            get { return _previousTrialMode.Value; }
            set
            {
                if (_previousTrialMode.Value == value)
                    return;
                _previousTrialMode.Value = value;
            }
        }

        #region Public Methods

        public static void CheckFirstRun()
        {
            if (_previousVersion.Value != Utility.ApplicationVersion || _previousTrialMode.Value != TrialManager.Current.IsTrial)
                SendFirstRunNotification();
        }

        #endregion

        #region HTTP Request and Response

        private static void SendFirstRunNotification()
        {
            string version = Utility.ApplicationVersion;
            string previousVersion = PreviousVersion;
            bool isTrial = TrialManager.Current.IsTrial;

            // Build the URL
            string url = NotificationURL + "?p=" + Utility.ApplicationIdentifier + "&v=" + version;

            // Determine notification type
            if (!isolatedSettings.Contains(FirstRunKey))
                url += "&t=n";
            else if (!isTrial && PreviousTrialMode != isTrial)
                url += "&t=n&tu=1";
            else
                url += "&t=u";

            // Trial mode
            if (isTrial)
                url += "&tr=1";

            // Previous version
            if (version != previousVersion && !string.IsNullOrEmpty(previousVersion))
                url += "&pv=" + previousVersion;

            // Emulator
            if (Microsoft.Devices.Environment.DeviceType == DeviceType.Emulator)
                url += "&e=1";

#if DEBUG
            url += "&d=1";
#endif

            // Submit the HTTP request
            try
            {
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
                Utility.PrepareHttpWebRequest(webRequest);
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
                isolatedSettings[FirstRunTrialKey] = TrialManager.Current.IsTrial;
                isolatedSettings.Save();
            }
            catch { }
        }

        #endregion

    }
}
