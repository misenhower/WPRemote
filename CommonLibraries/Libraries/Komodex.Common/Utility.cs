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
using System.Reflection;
using System.Globalization;
using System.Windows.Threading;
using System.ComponentModel;
using System.Linq;
using System.Xml.Linq;

namespace Komodex.Common
{
    public static class Utility
    {
        #region Application Identity

        private static string _applicationIdentifier;
        /// <summary>
        /// The application's short identifier, e.g., "serverpulse"
        /// </summary>
        public static string ApplicationIdentifier
        {
            get
            {
                if (_applicationIdentifier == null)
                    throw new Exception("Must call SetApplicationID before using ApplicationIdentifier.");
                return _applicationIdentifier;
            }
            private set { _applicationIdentifier = value; }
        }

        private static string _applicationName;
        /// <summary>
        /// The application's name, e.g., "Server Pulse"
        /// </summary>
        public static string ApplicationName
        {
            get
            {
                if (_applicationName == null)
                    throw new Exception("Must call SetApplicationID before using ApplicationName.");
                return _applicationName;
            }
            private set { _applicationName = value; }
        }

        private static string _applicationVersion;
        /// <summary>
        /// The current application version, e.g., "1.0.0.0"
        /// </summary>
        public static string ApplicationVersion
        {
            get
            {
                if (_applicationVersion == null)
                    throw new Exception("Must call SetApplicationID before using ApplicationVersion.");
                return _applicationVersion;
            }
            private set { _applicationVersion = value; }
        }

        /// <summary>
        /// Initializes the ApplicationIdentifier, ApplicationName, and ApplicationVersion properties.
        /// </summary>
        /// <param name="applicationIdentifier">The application's identifier, e.g., "serverpulse"</param>
        /// <param name="applicationName">The application's name, e.g., "Server Pulse"</param>
        /// <param name="applicationVersion">The application's version, e.g., "1.0.0.0" or null to automatically detect</param>
        public static void InitializeApplicationID(string applicationIdentifier, string applicationName, string applicationVersion = null)
        {
            if (applicationIdentifier == null)
                throw new ArgumentNullException("applicationIdentifier");
            if (applicationName == null)
                throw new ArgumentNullException("applicationName");

            ApplicationIdentifier = applicationIdentifier;
            ApplicationName = applicationName;

            if (string.IsNullOrEmpty(applicationVersion))
            {
                string assemblyInfo = Assembly.GetCallingAssembly().FullName;
                applicationVersion = assemblyInfo.Split('=')[1].Split(',')[0];
            }
            ApplicationVersion = applicationVersion;
        }

        #endregion

        #region HTTP Requests

        #region User Agent String

        private static string _userAgentString;
        public static string UserAgentString
        {
            get
            {
                if (_userAgentString == null)
                    _userAgentString = string.Format("Komodex {0}/{1} ({2})", ApplicationName, ApplicationVersion, Environment.OSVersion.ToString());
                return _userAgentString;
            }
        }

        #endregion

        #region HttpWebRequest Setup

        public static void PrepareHttpWebRequest(HttpWebRequest request)
        {
            request.UserAgent = UserAgentString;
            request.Headers["Accept-Language"] = CultureInfo.CurrentCulture.ToString();
            request.Headers["Application-Version"] = ApplicationVersion;
            request.Headers["OS-Version"] = Environment.OSVersion.Version.ToString();
        }

        #endregion

        #endregion

        #region BeginInvoke on UI Thread

#if WINDOWS_PHONE
        private static Dispatcher _dispatcher = Deployment.Current.Dispatcher;
#else
        // TODO
#endif

        public static void BeginInvokeOnUIThread(Action a)
        {
            // If we're already on the UI thread, just invoke the action
            if (_dispatcher.CheckAccess())
                a();
            // Otherwise, send it to the dispatcher
            else
                _dispatcher.BeginInvoke(a);
        }

        #endregion

        #region Event Raise/RaiseOnUIThread Extension Methods

        #region EventHandler<T>

        public static void Raise<T>(this EventHandler<T> eventHandler, object sender, T e)
            where T : EventArgs
        {
            if (eventHandler == null)
                return;

            eventHandler(sender, e);
        }

        public static void RaiseOnUIThread<T>(this EventHandler<T> eventHandler, object sender, T e)
            where T : EventArgs
        {
            BeginInvokeOnUIThread(() =>
            {
                eventHandler.Raise(sender, e);
            });
        }

        #endregion

        #region PropertyChangedEventHandler

        public static void RaiseOnUIThread(this PropertyChangedEventHandler eventHandler, object sender, string firstPropertyName, params string[] additionalPropertyNames)
        {
            BeginInvokeOnUIThread(() =>
            {
                if (eventHandler == null)
                    return;

                eventHandler(sender, new PropertyChangedEventArgs(firstPropertyName));

                foreach (string propertyName in additionalPropertyNames)
                    eventHandler(sender, new PropertyChangedEventArgs(propertyName));
            });
        }

        #endregion

        #region PropertyChangingEventHandler

        public static void Raise(this PropertyChangingEventHandler eventHandler, object sender, string firstPropertyName, params string[] additionalPropertyNames)
        {
            if (eventHandler == null)
                return;

            eventHandler(sender, new PropertyChangingEventArgs(firstPropertyName));

            foreach (string propertyName in additionalPropertyNames)
                eventHandler(sender, new PropertyChangingEventArgs(propertyName));
        }

        #endregion

        #endregion

        #region Local Hostname/IP Address Methods

        public static bool IsIPAddress(string value)
        {
            IPAddress ip;
            if (IPAddress.TryParse(value, out ip))
                return true;

            return false;
        }

        public static bool IsLocalHostname(string hostname)
        {
            if (hostname.Contains('.') && !hostname.EndsWith(".local"))
                return false;

            return true;
        }

        public static bool IsLocalIPAddress(IPAddress ip)
        {
            byte[] ipBytes = ip.GetAddressBytes();

            byte a = ipBytes[0];
            byte b = ipBytes[1];

            if (a == 127
                || a == 10
                || (a == 172 && b >= 16 && b <= 31)
                || (a == 192 && b == 168))
                return true;

            return false;
        }

        #endregion

        #region XML Extensions

        public static T GetValue<T>(this XAttribute attribute, T defaultValue = default(T))
        {
            if (attribute != null)
            {
                try
                {
                    return (T)Convert.ChangeType(attribute.Value, typeof(T), null);
                }
                catch { }
            }

            return defaultValue;
        }

        #endregion

        #region Bit Manipulation Extensions

        /// <summary>
        /// Gets the value of the bit at a specific position.
        /// </summary>
        /// <param name="index">The zero-based index of the value to get.</param>
        /// <returns></returns>
        public static bool GetBit(this byte b, int index)
        {
            return (b & (1 << index)) != 0;
        }

        /// <summary>
        /// Sets the bit at a specific position to the specified value.
        /// </summary>
        /// <param name="index">The zero-based index of the value to get.</param>
        /// <param name="value">The Boolean value to assign to the bit.</param>
        /// <returns></returns>
        public static void SetBit(ref byte b, int index, bool value)
        {
            if (value)
                b = (byte)(b | (1 << index));
            else
                b = (byte)(b & ~(1 << index));
        }

        #endregion

        #region Byte Array Extensions

        public static string ToHexString(this byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", "").ToLower();
        }

        #endregion

    }
}
