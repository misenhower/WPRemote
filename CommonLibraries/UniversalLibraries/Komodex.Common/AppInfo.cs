using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;

namespace Komodex.Common
{
    public static class AppInfo
    {
        internal static bool IsInitialized { get; private set; }

        /// <summary>
        /// Initializes the application name and display name.
        /// </summary>
        /// <param name="name">Internal app name, e.g., "remote".</param>
        /// <param name="displayName">App display name, e.g., "Remote".</param>
        public static void Initialize(string name, string displayName)
        {
            if (IsInitialized)
                throw new Exception("Already initialized.");
            IsInitialized = true;

            _name = name;
            _displayName = displayName;
        }

        private static string _name;
        public static string Name { get { return _name; } }

        private static string _displayName;
        public static string DisplayName { get { return _displayName; } }


        private static string _version;
        public static string Version
        {
            get
            {
                if (_version == null)
                {
                    var version = Package.Current.Id.Version;
                    _version = string.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision);
                }

                return _version;
            }
        }

        private static string _displayVersion;
        public static string DisplayVersion
        {
            get
            {
                if (_displayVersion == null)
                {
                    var version = Package.Current.Id.Version;
                    string format;
                    if (version.Revision == 0)
                    {
                        if (version.Build == 0)
                            format = "{0}.{1}";
                        else
                            format = "{0}.{1}.{2}";
                    }
                    else
                    {
                        format = "{0}.{1}.{2}.{3}";
                    }
                    _displayVersion = string.Format(format, version.Major, version.Minor, version.Build, version.Revision);
                }

                return _displayVersion;
            }
        }

        private static string _userAgent;
        public static string UserAgent
        {
            get
            {
                if (_userAgent == null)
                    _userAgent = string.Format("Komodex {0}/{1}", DisplayName, Version);

                return _userAgent;
            }
        }
    }
}
