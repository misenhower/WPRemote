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
using Microsoft.Phone.Info;

namespace Komodex.Common.Phone
{
    public static class FirstRunNotifier
    {
        // Notification URL
        private const string NotificationURL = "http://sys.komodex.com/wp7/notify/";

        #region Isolated Storage Settings

        private static readonly Setting<string> _notificationToBeSent = new Setting<string>("FirstRunNotificationToBeSent");
        private static string NotificationToBeSent
        {
            get { return _notificationToBeSent.Value; }
            set
            {
                if (_notificationToBeSent.Value == value)
                    return;
                _notificationToBeSent.Value = value;
            }
        }

        private static readonly Setting<string> _previousVersion = new Setting<string>("FirstRunCompleted");
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

        private static readonly Setting<bool> _previousTrialMode = new Setting<bool>("FirstRunTrialMode");
        private static bool PreviousTrialMode
        {
            get { return _previousTrialMode.Value; }
            set
            {
                if (_previousTrialMode.Value == value)
                    return;
                _previousTrialMode.Value = value;
            }
        }

        private static readonly Setting<DateTime> _firstRunDate = new Setting<DateTime>("FirstRunDate", DateTime.Now);
        public static DateTime FirstRunDate
        {
            get { return _firstRunDate.Value; }
        }

        #endregion

        #region Other Properties

        public static bool IsUpgrade { get; private set; }
        internal static bool WasTrial { get; private set; }

        #endregion

        #region Public Methods

        public static void CheckFirstRun()
        {
            IsUpgrade = PreviousVersion != Utility.ApplicationVersion;
            WasTrial = PreviousTrialMode && !TrialManager.Current.IsTrial;
            
            if (IsUpgrade || WasTrial)
                SetFirstRunNotification();

            PreviousVersion = Utility.ApplicationVersion;
            PreviousTrialMode = TrialManager.Current.IsTrial;

            SendFirstRunNotification();
        }

        #endregion

        #region HTTP Request and Response

        private static void SetFirstRunNotification()
        {
            string version = Utility.ApplicationVersion;
            string previousVersion = PreviousVersion;
            bool isTrial = TrialManager.Current.IsTrial;

            // Build the URL
            string request = "?p=" + Utility.ApplicationIdentifier + "&v=" + version;

            // Determine notification type
            if (string.IsNullOrEmpty(PreviousVersion))
                request += "&t=n";
            else if (!isTrial && PreviousTrialMode != isTrial)
                request += "&t=n&tu=1";
            else
                request += "&t=u";

            // Trial mode
            if (isTrial)
                request += "&tr=1";

            // Previous version
            if (version != previousVersion && !string.IsNullOrEmpty(previousVersion))
                request += "&pv=" + previousVersion;

            // Emulator
            if (Microsoft.Devices.Environment.DeviceType == DeviceType.Emulator)
                request += "&e=1";

#if DEBUG
            request += "&d=1";
#endif

            // Device info
            request += "&dm=" + Uri.EscapeDataString(DeviceStatus.DeviceManufacturer);
            request += "&dn=" + Uri.EscapeDataString(DeviceStatus.DeviceName);

            NotificationToBeSent = request;
        }

        private static void SendFirstRunNotification()
        {
            if (string.IsNullOrEmpty(NotificationToBeSent))
                return;

            string url = NotificationURL + NotificationToBeSent;

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

                NotificationToBeSent = null;
            }
            catch { }
        }

        #endregion

    }
}
