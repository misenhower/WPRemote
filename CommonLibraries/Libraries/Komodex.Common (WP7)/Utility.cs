﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
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
            using (StringWriter stringWriter = new StringWriter())
            {
                xmlSerializer.Serialize(stringWriter, value);
                return stringWriter.ToString();
            }
        }

        public static bool TryDeserialize<T>(this XmlSerializer xmlSerializer, string input, out T value)
        {
            using (StringReader stringReader = new StringReader(input))
            {
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
            }

            value = default(T);
            return false;
        }

        #endregion

        #region TimeSpan Extensions

        private static string _timeSeparator = DateTime.MinValue.ToString("%:");

        public static string ToShortTimeString(this TimeSpan ts, bool nullIfZero = false)
        {
            if (nullIfZero && ts == TimeSpan.Zero)
                return null;

            string negativeSign = string.Empty;
            if (ts < TimeSpan.Zero)
            {
                negativeSign = "-";
                ts = ts.Duration();
            }

            int hours = (int)ts.TotalHours;
            int minutes = ts.Minutes;
            int seconds = ts.Seconds;

            if (hours > 0)
                return negativeSign + hours + _timeSeparator + minutes.ToString("00") + _timeSeparator + seconds.ToString("00");
            return negativeSign + minutes + _timeSeparator + seconds.ToString("00");
        }

        #endregion

        #region String Utilities

        public static string JoinNonEmptyStrings(string separator, params string[] values)
        {
            return JoinNonEmptyStrings(separator, (IEnumerable<string>)values);
        }

        public static string JoinNonEmptyStrings(string separator, IEnumerable<string> values)
        {
            return string.Join(separator, values.Where(s => !string.IsNullOrEmpty(s)));
        }

        public static string JoinNonEmptyStrings(string separator, params object[] values)
        {
            return JoinNonEmptyStrings(separator, (IEnumerable<object>)values);
        }

        public static string JoinNonEmptyStrings<T>(string separator, IEnumerable<T> values)
        {
            return JoinNonEmptyStrings(separator, values.Where(v => v != null).Select(v => v.ToString()));
        }

        public static bool Contains(this string s, string value, StringComparison comparisonType)
        {
            return (s.IndexOf(value, comparisonType) >= 0);
        }

        #endregion

        #region List<T> Utilities

#if WP7
        public static int FindIndex<T>(this List<T> list, Predicate<T> match)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (match(list[i]))
                    return i;
            }

            return -1;
        }
#endif

        #endregion
    }
}
