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
using System.Windows.Navigation;
using System.IO;
using Microsoft.Phone.Shell;

namespace Komodex.Analytics
{
    public static class PhoneAppCrashReporter
    {
        private static PhoneApplicationFrame RootFrame = null;
        private static bool IsObscured = false;
        private static bool IsLocked = false;


        public static void Initialize(Application application, PhoneApplicationFrame rootFrame)
        {
            // Initialize the CrashReporter if it hasn't already been initialized
            CrashReporter.Initialize(application);

            // Get the RootFrame object
            RootFrame = rootFrame;

            // Hook into navigation failed event
            RootFrame.NavigationFailed += new NavigationFailedEventHandler(RootFrame_NavigationFailed);

            // Hook into obscured/unobscured events
            RootFrame.Obscured += new EventHandler<ObscuredEventArgs>(RootFrame_Obscured);
            RootFrame.Unobscured += new EventHandler(RootFrame_Unobscured);

            // Add the navigation info callback to the CrashReporter
            CrashReporter.AdditionalLogInfoCallbacks.Add(NavigationInfoCallback);
        }


        #region Events

        private static void RootFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            CrashReporter.LogException(e.Exception, "Navigation Failed");
        }

        private static void RootFrame_Obscured(object sender, ObscuredEventArgs e)
        {
            IsObscured = true;
            IsLocked = e.IsLocked;
        }

        private static void RootFrame_Unobscured(object sender, EventArgs e)
        {
            IsObscured = false;
            IsLocked = false;
        }

        #endregion

        #region Log Info Callback

        private static void NavigationInfoCallback(TextWriter writer)
        {
            try
            {
                writer.WriteLine("-> Current page: " + RootFrame.CurrentSource);
            }
            catch { } // If we're not in the UI thread, attempting to access RootFrame.CurrentSource will throw an UnauthorizedAccessException

            writer.WriteLine("-> Startup Mode: " + PhoneApplicationService.Current.StartupMode);
            writer.WriteLine("-> Obscured: " + ((IsObscured) ? "Yes" : "No"));
            writer.WriteLine("-> Locked: " + ((IsLocked) ? "Yes" : "No"));
        }

        #endregion

    }
}
