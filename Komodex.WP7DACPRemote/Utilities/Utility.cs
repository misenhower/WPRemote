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

namespace Komodex.WP7DACPRemote
{
    public static class Utility
    {
        #region Application Version

        public static string GetApplicationVersion()
        {
            string assemblyInfo = Assembly.GetExecutingAssembly().FullName;
            if (string.IsNullOrEmpty(assemblyInfo))
                return string.Empty;

            var splitString = assemblyInfo.Split(',');
            foreach (var stringPart in splitString)
            {
                var stringPartTrimmed = stringPart.Trim();
                if (stringPartTrimmed.StartsWith("Version="))
                    return stringPartTrimmed.Substring(8).Trim();
            }

            return string.Empty;
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
                    MessageBox.Show("Could not connect to iTunes. Please disconnect the USB cable between your phone and computer and try again.", "Network Connection Error", MessageBoxButton.OK);
                    return false;
                default:
                    MessageBox.Show("Please make sure your phone is connected to a Wi-fi network and try again.", "Wi-fi Connection Error", MessageBoxButton.OK);
                    return false;
            }
        }

        #endregion

        #region LongListSelector helpers

        public static void LongListSelectorGroupAnimationHelper(LongListSelector sender, GroupViewOpenedEventArgs e)
        {
            ItemContainerGenerator itemContainerGenerator = e.ItemsControl.ItemContainerGenerator;
            SwivelTransition swivel = new SwivelTransition();
            swivel.Mode = SwivelTransitionMode.ForwardIn;

            foreach (var item in e.ItemsControl.Items)
            {
                UIElement element = (UIElement)itemContainerGenerator.ContainerFromItem(item);
                ITransition animation = swivel.GetTransition(element);
                animation.Begin();
            }
        }

        public static void LongListSelectorGroupAnimationHelper(LongListSelector sender, GroupViewClosingEventArgs e)
        {
            LongListSelector list = (LongListSelector)sender;

            ItemContainerGenerator itemContainerGenerator = e.ItemsControl.ItemContainerGenerator;
            SwivelTransition swivel = new SwivelTransition();
            swivel.Mode = SwivelTransitionMode.BackwardOut;

            ITransition animation = null;

            foreach (var item in e.ItemsControl.Items)
            {
                // Begin the previous item's animation
                if (animation != null)
                    animation.Begin();

                UIElement element = (UIElement)itemContainerGenerator.ContainerFromItem(item);
                animation = swivel.GetTransition(element);
                animation.Begin();
            }

            if (animation != null)
            {
                animation.Completed += delegate
                {
                    list.CloseGroupView();
                };
                animation.Begin();
                e.Handled = true;
            }

            list.ScrollToGroup(e.SelectedGroup);
        }

        #endregion
    }
}
