using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Komodex.Remote.DACPServerManagement;
using Komodex.Remote.Settings;
using Komodex.Remote.Utilities;
using Komodex.Remote.Controls;
using Komodex.Common.Phone;
using System.Threading;
using System.Globalization;
using Komodex.Analytics;
using Komodex.Remote.ServerManagement;

namespace Komodex.Remote
{
    public partial class App : Application
    {
        protected const int TrialDays = 7;

        /// <summary>
        /// Provides easy access to the root frame of the Phone Application.
        /// </summary>
        /// <returns>The root frame of the Phone Application.</returns>
        public static PhoneApplicationFrame RootFrame { get; private set; }

        /// <summary>
        /// Constructor for the Application object.
        /// </summary>
        public App()
        {
            //CultureInfo culture = new CultureInfo("de-DE");
            //Thread.CurrentThread.CurrentCulture = culture;
            //Thread.CurrentThread.CurrentUICulture = culture;

            // Global handler for uncaught exceptions. 
            UnhandledException += Application_UnhandledException;

            // Show graphics profiling information while debugging.
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // Display the current frame rate counters.
                Application.Current.Host.Settings.EnableFrameRateCounter = true;

                // Show the areas of the app that are being redrawn in each frame.
                //Application.Current.Host.Settings.EnableRedrawRegions = true;

                // Enable non-production analysis visualization mode, 
                // which shows areas of a page that are being GPU accelerated with a colored overlay.
                //Application.Current.Host.Settings.EnableCacheVisualization = true;

#if DEBUG
                Komodex.Remote.Controls.MemoryCounters.Show();
#endif
            }

            // Standard Silverlight initialization
            InitializeComponent();

            // Phone-specific initialization
            InitializePhoneApplication();

            RemoteUtility.Initialize();

            // Update settings if necessary
            SettingsUpdater.CheckForUpdate();

            TrialManager.Initialize(TrialDays, false, true);

            // Error reporter initialization
            PhoneAppCrashReporter.Initialize(this, RootFrame);
            CrashReporter.AdditionalLogInfoCallbacks.Add(RemoteUtility.DACPInfoCrashReporterCallback);

            // Set up hooks
            NavigationManager.DoFirstLoad(RootFrame);

            ServerManager.Initialize();
            NetworkManager.Initialize();
            BonjourManager.Initialize();
            ConnectionStatusPopupManager.Initialize();

            // Other initialization
            SettingsManager.Initialize();

            // Tilt Effect
            TiltEffect.TiltableItems.Add(typeof(FakeButton));

#if WP7
            // Remove LongListSelector from the TiltableItems list
            if (TiltEffect.TiltableItems.Contains(typeof(LongListSelector)))
                TiltEffect.TiltableItems.Remove(typeof(LongListSelector));
#endif
        }

#if DEBUG
        public bool EnableDiagnosticData
        {
            get { return Application.Current.Host.Settings.EnableFrameRateCounter; }
            set
            {
                Application.Current.Host.Settings.EnableFrameRateCounter = value;
                Komodex.Remote.Controls.MemoryCounters.EnableMemoryCounters = value;
            }
        }
#endif

        // Code to execute when the application is launching (eg, from Start)
        // This code will not execute when the application is reactivated
        private void Application_Launching(object sender, LaunchingEventArgs e)
        {
            FirstRunNotifier.CheckFirstRun();
        }

        // Code to execute when the application is activated (brought to foreground)
        // This code will not execute when the application is first launched
        private void Application_Activated(object sender, ActivatedEventArgs e)
        {
            DACPServerManager.ApplicationActivated();
        }

        // Code to execute when the application is deactivated (sent to background)
        // This code will not execute when the application is closing
        private void Application_Deactivated(object sender, DeactivatedEventArgs e)
        {
            DACPServerManager.ApplicationDeactivated();
        }

        // Code to execute when the application is closing (eg, user hit Back)
        // This code will not execute when the application is deactivated
        private void Application_Closing(object sender, ClosingEventArgs e)
        {
        }

        // Code to execute if a navigation fails
        private void RootFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // A navigation has failed; break into the debugger
                System.Diagnostics.Debugger.Break();
            }
        }

        // Code to execute on Unhandled Exceptions
        private void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // An unhandled exception has occurred; break into the debugger
                System.Diagnostics.Debugger.Break();
            }
        }

        #region Phone application initialization

        // Avoid double-initialization
        private bool phoneApplicationInitialized = false;

        // Do not add any additional code to this method
        private void InitializePhoneApplication()
        {
            if (phoneApplicationInitialized)
                return;

            // Create the frame but don't set it as RootVisual yet; this allows the splash
            // screen to remain active until the application is ready to render.
            RootFrame = new PhoneApplicationFrame();
            RootFrame.Navigated += CompleteInitializePhoneApplication;

            // Handle navigation failures
            RootFrame.NavigationFailed += RootFrame_NavigationFailed;

            // Ensure we don't initialize again
            phoneApplicationInitialized = true;
        }

        // Do not add any additional code to this method
        private void CompleteInitializePhoneApplication(object sender, NavigationEventArgs e)
        {
            // Set the root visual to allow the application to render
            if (RootVisual != RootFrame)
                RootVisual = RootFrame;

            // Remove this handler since it is no longer needed
            RootFrame.Navigated -= CompleteInitializePhoneApplication;
        }

        #endregion
    }
}