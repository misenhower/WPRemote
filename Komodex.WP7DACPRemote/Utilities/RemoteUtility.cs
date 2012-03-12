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
using Komodex.WP7DACPRemote.Controls;
using Komodex.Common;

namespace Komodex.WP7DACPRemote
{
    public static class RemoteUtility
    {
        #region Initialization

        public static void Initialize()
        {
            Utility.InitializeApplicationID("remote", "Remote");
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

        public static AnimatorHelperBase GetListSelectorAnimation(this AnimatedBasePage page, LongListSelectorEx listSelector, AnimationType animationType)
        {
            if (listSelector.SelectedItem != null)
            {
                var contentPresenter = listSelector.SelectedContentPresenter;

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
}
