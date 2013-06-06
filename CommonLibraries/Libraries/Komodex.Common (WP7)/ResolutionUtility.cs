using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace Komodex.Common
{
    public static class ResolutionUtility
    {
        static ResolutionUtility()
        {
#if WP7
            ScreenResolution = ScreenResolution.WVGA;
#else
            switch (Application.Current.Host.Content.ScaleFactor)
            {
                case 150:
                    ScreenResolution = ScreenResolution.HD720p;
                    break;

                case 160:
                    ScreenResolution = ScreenResolution.WXGA;
                    break;

                case 100:
                default:
                    ScreenResolution = ScreenResolution.WVGA;
                    break;
            }
#endif

            Log.Main.Info("Screen resolution: " + ScreenResolution);
        }

        public static ScreenResolution ScreenResolution { get; private set; }
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
