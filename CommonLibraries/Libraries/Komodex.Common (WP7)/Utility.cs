using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Xml.Linq;
using System.Xml.Serialization;

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
        /// <param name="applicationVersion">The application's version, e.g., "1.0.0.0"</param>
        public static void InitializeApplicationID(string applicationIdentifier, string applicationName, string applicationVersion)
        {
            if (applicationIdentifier == null)
                throw new ArgumentNullException("applicationIdentifier");
            if (applicationName == null)
                throw new ArgumentNullException("applicationName");
            if (applicationVersion == null)
                throw new ArgumentNullException("applicationVersion");

            ApplicationIdentifier = applicationIdentifier;
            ApplicationName = applicationName;
            ApplicationVersion = applicationVersion;

            // Get the current synchronization context (which should be on the UI thread)
            _uiSynchronizationContext = SynchronizationContext.Current;
        }


        /// <summary>
        /// Initializes the ApplicationIdentifier, ApplicationName, and ApplicationVersion properties.
        /// </summary>
        /// <param name="applicationIdentifier">The application's identifier, e.g., "serverpulse"</param>
        /// <param name="applicationName">The application's name, e.g., "Server Pulse"</param>
        /// <param name="applicationVersion">A Type used to derive the application version</param>
        public static void InitializeApplicationID(string applicationIdentifier, string applicationName, Type applicationType)
        {
            string assemblyInfo = applicationType.AssemblyQualifiedName;
            string applicationVersion = assemblyInfo.Split('=')[1].Split(',')[0];
            InitializeApplicationID(applicationIdentifier, applicationName, applicationVersion);
        }

#if WINDOWS_PHONE
        /// <summary>
        /// Initializes the ApplicationIdentifier, ApplicationName, and ApplicationVersion properties.
        /// Automatically detects the calling assembly's version.
        /// </summary>
        /// <param name="applicationIdentifier">The application's identifier, e.g., "serverpulse"</param>
        /// <param name="applicationName">The application's name, e.g., "Server Pulse"</param>
        public static void InitializeApplicationID(string applicationIdentifier, string applicationName)
        {
            string assemblyInfo = Assembly.GetCallingAssembly().FullName;
            string applicationVersion = assemblyInfo.Split('=')[1].Split(',')[0];
            InitializeApplicationID(applicationIdentifier, applicationName, applicationVersion);
        }
#endif

        #endregion

        #region HTTP Requests

        #region User Agent String

        private static string _userAgentString;
        public static string UserAgentString
        {
            get
            {
                if (_userAgentString == null)
                {
                    _userAgentString = string.Format("Komodex {0}/{1}", ApplicationName, ApplicationVersion);
#if WINDOWS_PHONE
                    _userAgentString += string.Format(" ({0})", Environment.OSVersion.ToString());
#endif
                }
                return _userAgentString;
            }
        }

        #endregion

        #region HttpWebRequest Setup

        public static void PrepareHttpWebRequest(HttpWebRequest request)
        {
#if !NETFX_CORE
            request.UserAgent = UserAgentString;
#endif
            request.Headers["Accept-Language"] = CultureInfo.CurrentCulture.ToString();
            request.Headers["Application-Version"] = ApplicationVersion;

#if WINDOWS_PHONE
            // Device Info
            request.Headers["OS-Version"] = Environment.OSVersion.Version.ToString();
            request.Headers["Device-Manufacturer"] = Microsoft.Phone.Info.DeviceStatus.DeviceManufacturer;
            request.Headers["Device-Model"] = Microsoft.Phone.Info.DeviceStatus.DeviceName;
#endif
        }

        #endregion

        #endregion

        #region BeginInvoke on UI Thread

        private static SynchronizationContext _uiSynchronizationContext;

        public static void BeginInvokeOnUIThread(Action a)
        {
            if (_uiSynchronizationContext == null)
                throw new Exception("Initialize Utility class from the UI thread before calling BeginInvokeOnUIThread.");

            // If we're already on the UI thread, just invoke the action
            if (_uiSynchronizationContext == SynchronizationContext.Current)
                a();
            // Otherwise, send it to the dispatcher
            else
                _uiSynchronizationContext.Post((state) => a(), null);
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

#if WINDOWS_PHONE
        public static void Raise(this PropertyChangingEventHandler eventHandler, object sender, string firstPropertyName, params string[] additionalPropertyNames)
        {
            if (eventHandler == null)
                return;

            eventHandler(sender, new PropertyChangingEventArgs(firstPropertyName));

            foreach (string propertyName in additionalPropertyNames)
                eventHandler(sender, new PropertyChangingEventArgs(propertyName));
        }
#endif

        #endregion

        #endregion

        #region Local Hostname/IP Address Methods

#if WINDOWS_PHONE
        public static bool IsIPAddress(string value)
        {
            IPAddress ip;
            if (IPAddress.TryParse(value, out ip))
                return true;

            return false;
        }
#endif

        public static bool IsLocalHostname(string hostname)
        {
            if (hostname.Contains(".") && !hostname.EndsWith(".local"))
                return false;

            return true;
        }

#if WINDOWS_PHONE
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
#endif

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

        #region IDictionary Extensions

        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue = default(TValue))
        {
            if (dictionary == null)
                return defaultValue;
            if (!dictionary.ContainsKey(key))
                return defaultValue;

            return dictionary[key];
        }

        public static bool GetBoolValue<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
        {
            var value = dictionary.GetValueOrDefault(key);
            if (value == null)
                return false;

            bool result;
            bool.TryParse(value.ToString(), out result);
            return result;
        }

        public static bool TryGetValue<T>(this IDictionary<string,object> dictionary, string key, out T value)
        {
            object retrievedValue;
            bool success = dictionary.TryGetValue(key, out retrievedValue);
            if (success && retrievedValue is T)
            {
                value = (T)retrievedValue;
                return true;
            }
            value = default(T);
            return false;
        }

        #endregion

        #region ICollection<T> Extensions

        /// <summary>
        /// Adds an item to the collection only if it is not already contained in the collection.
        /// </summary>
        /// <param name="collection"></param>
        public static void AddOnce<T>(this ICollection<T> collection, T item)
        {
            if (!collection.Contains(item))
                collection.Add(item);
        }

        #endregion

        #region File Size Formatting

        public static string ReadableFilesize(long bytes)
        {
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            int i;
            for (i = 0; bytes >= 1024 && i < units.Length - 1; i++)
                bytes /= 1024;

            return string.Format("{0:0.##} {1}", bytes, units[i]);
        }

        #endregion

        #region XmlSerializer Extensions

        public static string SerializeToString(this XmlSerializer xmlSerializer, object value)
        {
            StringWriter stringWriter = new StringWriter();
            xmlSerializer.Serialize(stringWriter, value);
            return stringWriter.ToString();
        }

        public static bool TryDeserialize<T>(this XmlSerializer xmlSerializer, string input, out T value)
        {
            StringReader stringReader = new StringReader(input);
            try
            {
                object obj = xmlSerializer.Deserialize(stringReader);
                if (obj is T)
                {
                    value = (T)obj;
                    return true;
                }
            }
            catch { }

            value = default(T);
            return false;
        }

        #endregion
    }
}
