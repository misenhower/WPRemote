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
                e.Cancel = true;
            }

            list.ScrollToGroup(e.SelectedGroup);
        }

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
