using Komodex.Common.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Web.Http;

namespace Komodex.Common.Analytics
{
    public static class InstallationNotifier
    {
        private static readonly Log _log = new Log("Installation Notifier");
        private const string BaseUri = "http://sys.komodex.com/appnotify/";

        private static readonly Setting<string> _lastNotifiedVersion = new Setting<string>("InstallationNotifier.LastNotifiedVersion");
        private static string LastNotifiedVersion
        {
            get { return _lastNotifiedVersion.Value; }
            set { _lastNotifiedVersion.Value = value; }
        }

        private static readonly Setting<bool> _lastNotifiedTrialState = new Setting<bool>("InstallationNotifier.LastNotifiedTrialState");
        private static bool LastNotifiedTrialState
        {
            get { return _lastNotifiedTrialState.Value; }
            set { _lastNotifiedTrialState.Value = value; }
        }

        public static async void Initialize()
        {
            _log.Debug("Initializing...");

            // Check whether we need to send a notification message
            bool needsNotification = false;
            // Check whether the version has changed
            if (AppInfo.Version != LastNotifiedVersion)
                needsNotification = true;
            // Check whether the trial state has changed
            else if (TrialManager.Current.IsTrial != LastNotifiedTrialState)
                needsNotification = true;

            if (!needsNotification)
            {
                _log.Trace("Notification already up-to-date.");
                return;
            }

            // Attempt to send the notification
            _log.Debug("Sending notification...");
            bool success = await SendNotificationAsync();

            if (success)
            {
                _log.Debug("Successfully sent notification.");
                // Update the persistent settings with current values
                LastNotifiedVersion = AppInfo.Version;
                LastNotifiedTrialState = TrialManager.Current.IsTrial;
            }
            else
            {
                _log.Error("Couldn't send installation notification");
            }
        }

        private static async Task<bool> SendNotificationAsync()
        {
            // Build the query string
            var queryStringParts = new List<KeyValuePair<string, string>>();

            // Product
            queryStringParts.Add("p", AppInfo.Name);

            // Version
            queryStringParts.Add("v", AppInfo.Version);

            // Notification type
            if (LastNotifiedVersion == null)
                queryStringParts.Add("t", "n");
            else if (!TrialManager.Current.IsTrial && LastNotifiedTrialState)
            {
                queryStringParts.Add("t", "n");
                queryStringParts.Add("tu", "1");
            }
            else
                queryStringParts.Add("t", "u");

            // Trial Mode
            if (TrialManager.Current.IsTrial)
                queryStringParts.Add("tr", "1");

            // Previous Version
            if (LastNotifiedVersion != null && AppInfo.Version != LastNotifiedVersion)
                queryStringParts.Add("pv", LastNotifiedVersion);

            // Device Type
            switch (DeviceInfo.Type)
            {
                case DeviceType.Windows:
                    queryStringParts.Add("dt", "win");
                    break;
                case DeviceType.WindowsPhone:
                    queryStringParts.Add("dt", "wp");
                    break;
                case DeviceType.Unknown:
                default:
                    queryStringParts.Add("dt", string.Empty);
                    break;
            }

#if DEBUG
            // Debug
            queryStringParts.Add("d", "1");
#endif

            // Build the URI
            UriBuilder uriBuilder = new UriBuilder(BaseUri);
            string queryString = string.Join("&", queryStringParts.Select(kvp => kvp.Key + "=" + kvp.Value));
            uriBuilder.Query = queryString;

            // Send the request
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders["User-Agent"] = AppInfo.UserAgent;
            try
            {
                _log.Debug("Sending notification to: " + uriBuilder.Uri);
                var response = await client.PostAsync(uriBuilder.Uri, new HttpStringContent(string.Empty)).AsTask().ConfigureAwait(false);
                return response.IsSuccessStatusCode;
            }
            catch { }

            return false;
        }
    }
}
