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
using System.Reflection;
using Microsoft.Phone.Net.NetworkInformation;
using Clarity.Phone.Controls;
using Clarity.Phone.Controls.Animations;
using System.Linq;
using Komodex.WP7DACPRemote.Localization;

namespace Komodex.WP7DACPRemote
{
    public static class Utility
    {
        #region Application Version

        private static string _ApplicationVersion = null;
        public static string ApplicationVersion
        {
            get
            {
                if (_ApplicationVersion == null)
                    _ApplicationVersion = GetApplicationVersion();
                return _ApplicationVersion;
            }
        }

        private static string GetApplicationVersion()
        {
            string assemblyInfo = Assembly.GetExecutingAssembly().FullName;
            return assemblyInfo.Split('=')[1].Split(',')[0];
        }

        #endregion

        #region Network Info

        public static bool CheckNetworkConnectivity(bool notifyUser = true)
        {
            NetworkInterfaceType interfaceType = NetworkInterface.NetworkInterfaceType;

            switch (interfaceType)
            {
                case NetworkInterfaceType.Wireless80211:
                    return true;
                case NetworkInterfaceType.Ethernet:
                    MessageBox.Show(LocalizedStrings.LibraryUSBErrorBody, LocalizedStrings.LibraryUSBErrorTitle, MessageBoxButton.OK);
                    return false;
                default:
                    MessageBox.Show(LocalizedStrings.LibraryWifiErrorBody, LocalizedStrings.LibraryWifiErrorTitle, MessageBoxButton.OK);
                    return false;
            }
        }

        #endregion

        #region LongListSelector helpers

        public static AnimatorHelperBase GetListSelectorAnimation(this AnimatedBasePage page, LongListSelector listSelector, AnimationType animationType)
        {
            if (listSelector.SelectedItem != null)
            {
                var contentPresenters = listSelector.GetItemsWithContainers(true, true).Cast<ContentPresenter>();
                var contentPresenter = contentPresenters.FirstOrDefault(cp => cp.Content == listSelector.SelectedItem);

                if (animationType == AnimationType.NavigateBackwardIn)
                    listSelector.SelectedItem = null;

                if (contentPresenter != null)
                {
                    return page.GetContinuumAnimation(contentPresenter, animationType);
                }
            }

            return page.GetContinuumAnimation(page.AnimationContext, animationType);
        }

        #endregion
    }

    public static class Enum<T>
    {
        public static T Parse(string value)
        {
            return Enum<T>.Parse(value, true);
        }

        public static T Parse(string value, bool ignoreCase)
        {
            return (T)Enum.Parse(typeof(T), value, ignoreCase);
        }

        public static bool TryParse(string value, bool ignoreCase, out T result)
        {
            try
            {
                result = (T)Enum.Parse(typeof(T), value, ignoreCase);
                return true;
            }
            catch
            {
                result = default(T);
                return false;
            }
        }

        public static T ParseOrDefault(string value, T defaultValue)
        {
            return ParseOrDefault(value, defaultValue, true);
        }

        public static T ParseOrDefault(string value, T defaultValue, bool ignoreCase)
        {
            T result;

            if (TryParse(value, ignoreCase, out result))
                return result;
            else
                return defaultValue;
        }
    }
}
