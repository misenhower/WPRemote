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
using System.Collections.Generic;
using System.Linq;

namespace Komodex.Common.Phone
{
    public static class PhoneApplicationUtility
    {
        #region Navigation

        internal const string NavigationRemoveBackEntryParameterName = "removebackentry";

        public static bool Navigate(this PhoneApplicationFrame frame, string source)
        {
            Uri uri = new Uri(source, UriKind.Relative);
            return frame.Navigate(uri);
        }

        public static bool Navigate(this PhoneApplicationFrame frame, string source, Dictionary<string, string> parameters)
        {
            return frame.Navigate(source, false, parameters);
        }

        public static bool Navigate(this PhoneApplicationFrame frame, string source, bool removeBackEntry, Dictionary<string, string> parameters = null)
        {
            if (removeBackEntry)
            {
                if (parameters == null)
                    parameters = new Dictionary<string, string>();

                parameters[NavigationRemoveBackEntryParameterName] = "true";
            }

            if (parameters != null)
            {
                source += "?" + string.Join("&", parameters.Select(p => Uri.EscapeDataString(p.Key) + "=" + Uri.EscapeDataString(p.Value)));
            }

            return frame.Navigate(source);
        }

        #endregion
    }
}
