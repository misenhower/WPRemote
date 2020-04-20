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

        public static bool Navigate(this PhoneApplicationFrame frame, string source, params KeyValuePair<string, string>[] parameters)
        {

            return frame.Navigate(source, false, parameters);
        }

        public static bool Navigate(this PhoneApplicationFrame frame, string source, bool removeBackEntry, params KeyValuePair<string, string>[] parameters)
        {
            if (removeBackEntry)
            {
                if (parameters == null)
                    parameters = new KeyValuePair<string, string>[1];
                else
                    Array.Resize(ref parameters, parameters.Length + 1);

                parameters[parameters.Length - 1] = new KeyValuePair<string, string>(NavigationRemoveBackEntryParameterName, "true");
            }

            if (parameters != null)
            {
                if (source.Contains('?'))
                    source += "&";
                else
                    source += "?";

                source += string.Join("&", parameters.Select(p => Uri.EscapeDataString(p.Key) + "=" + Uri.EscapeDataString(p.Value)));
            }

            return frame.Navigate(source);
        }

        #endregion
    }
}
