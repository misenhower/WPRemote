using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

namespace Komodex.Common
{
    public static class ResolutionUtility
    {
        static ResolutionUtility()
        {
            FilenameSuffixWVGA = ".WVGA";
            FilenameSuffixWXGA = ".WXGA";
            FilenameSuffix720p = ".720p";

#if WP7
            ScreenResolution = ScreenResolution.WVGA;
#else
            switch (Application.Current.Host.Content.ScaleFactor)
            {
                case 100:
                default:
                    ScreenResolution = ScreenResolution.WVGA;
                    break;

                case 150:
                    ScreenResolution = ScreenResolution.HD720p;
                    break;

                case 160:
                    ScreenResolution = ScreenResolution.WXGA;
                    break;
            }
#endif

            Log.Main.Info("Current device screen resolution: " + ScreenResolution);
        }

        public static string FilenameSuffixWVGA { get; set; }
        public static string FilenameSuffixWXGA { get; set; }
        public static string FilenameSuffix720p { get; set; }

        public static ScreenResolution ScreenResolution { get; private set; }

        private static string GetCurrentFilenameSuffix()
        {
            switch (ScreenResolution)
            {
                case ScreenResolution.WVGA:
                default:
                    return FilenameSuffixWVGA;

                case ScreenResolution.WXGA:
                    return FilenameSuffixWXGA;

                case ScreenResolution.HD720p:
                    return FilenameSuffix720p;
            }
        }

        public static Uri GetUriWithResolutionSuffix(string source)
        {
            return GetUriWithResolutionSuffix(new Uri(source, UriKind.RelativeOrAbsolute));
        }

        public static Uri GetUriWithResolutionSuffix(Uri source)
        {
            if (source == null)
                return source;

            // Get the URI parts
            string uriString = source.ToString();
            int separatorIndex = uriString.LastIndexOf('.');
            string filename = uriString.Substring(0, separatorIndex);
            string extension = uriString.Substring(separatorIndex, uriString.Length - separatorIndex);

            // Assemble the new path
            string path = filename + GetCurrentFilenameSuffix() + extension;

            // Fix some designer issues
            if (DesignerProperties.IsInDesignTool)
            {
                if (source.IsAbsoluteUri)
                {
                    // Get the absolute path of the new URI (without "file:///")
                    Uri uri = new Uri(path, UriKind.Absolute);
                    path = uri.AbsolutePath;

                    // If the file doesn't exist, attempt to locate it
                    if (!File.Exists(path))
                    {
                        // Try to find the file in a shared "Assets" directory
                        int pos = path.LastIndexOf("/Assets");
                        if (pos >= 0)
                        {
                            string newPath = path.Insert(pos, "/..");
                            if (File.Exists(newPath))
                                path = newPath;
                        }
                    }
                }
            }

            UriKind kind = (source.IsAbsoluteUri) ? UriKind.Absolute : UriKind.Relative;
            return new Uri(path, kind);
        }

        public static int GetScaledPixels(int points)
        {
#if WP7
            return points;
#else
            switch (ScreenResolution)
            {
                case ScreenResolution.WVGA:
                default:
                    return points;

                case ScreenResolution.HD720p:
                    return (int)Math.Ceiling(points * 1.5);

                case ScreenResolution.WXGA:
                    return (int)Math.Ceiling(points * 1.6);
            }
#endif
        }
    }

    public enum ScreenResolution
    {
        /// <summary>
        /// <para>Resolution: 480x800px</para>
        /// <para>Scale factor: 100%</para>
        /// </summary>
        WVGA,

        /// <summary>
        /// <para>Resolution: 768x1280px</para>
        /// <para>Scale factor: 160%</para>
        /// </summary>
        WXGA,

        /// <summary>
        /// <para>Resolution: 720x1280px</para>
        /// <para>Scale factor: 150%</para>
        /// </summary>
        HD720p,
    }
}
