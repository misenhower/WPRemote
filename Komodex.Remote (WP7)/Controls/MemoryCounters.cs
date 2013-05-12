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
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using Microsoft.Phone.Info;

namespace Komodex.Remote.Controls
{
#if DEBUG
    public static class MemoryCounters
    {
        private static Popup memoryPopup = null;
        private static TextBlock memoryCurrentText = null;
        private static TextBlock memoryPeakText = null;
        private static TextBlock memoryTotalText = null;
        private static DispatcherTimer timer = null;

        public static bool EnableMemoryCounters
        {
            get
            {
                if (memoryPopup == null)
                    return false;
                return memoryPopup.IsOpen;
            }
            set
            {
                if (value)
                    Show();
                else
                    Hide();
            }
        }

        public static void Show()
        {
            if (memoryPopup == null)
            {
                memoryPopup = new Popup();

                // Border
                Border bdr = new Border();
                bdr.Background = (Brush)App.Current.Resources["PhoneBackgroundBrush"];
                bdr.Margin = new Thickness(481, 415, 0, 0);

                RotateTransform rt = new RotateTransform();
                rt.Angle = 90;
                bdr.RenderTransform = rt;

                // StackPanel
                StackPanel sp = new StackPanel();
                sp.Orientation = Orientation.Horizontal;

                // TextBlock
                memoryCurrentText = new TextBlock();
                memoryCurrentText.FontSize = 15;
                memoryCurrentText.FontWeight = FontWeights.Bold;
                memoryCurrentText.Foreground = (Brush)App.Current.Resources["PhoneForegroundBrush"];
                memoryCurrentText.Width = 100;
                sp.Children.Add(memoryCurrentText);

                memoryPeakText = new TextBlock();
                memoryPeakText.FontSize = 15;
                memoryPeakText.FontWeight = FontWeights.Bold;
                memoryPeakText.Foreground = (Brush)App.Current.Resources["PhoneForegroundBrush"];
                memoryPeakText.Width = 100;
                sp.Children.Add(memoryPeakText);

                memoryTotalText = new TextBlock();
                memoryTotalText.FontSize = 15;
                memoryTotalText.FontWeight = FontWeights.Bold;
                memoryTotalText.Foreground = (Brush)App.Current.Resources["PhoneForegroundBrush"];
                memoryTotalText.Width = 100;
                sp.Children.Add(memoryTotalText);

                bdr.Child = sp;
                memoryPopup.Child = bdr;

                timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(1);
                timer.Tick += timer_Tick;
            }

            UpdateMemoryCounterText();
            memoryPopup.IsOpen = true;
            timer.Start();
        }

        public static void Hide()
        {
            if (memoryPopup == null)
                return;

            timer.Stop();
            memoryPopup.IsOpen = false;
        }

        private static void UpdateMemoryCounterText()
        {
            memoryCurrentText.Text = "C: " + BytesToMegabytes(CurrentMemoryUsage).ToString("0.00") + " MB";
            memoryPeakText.Text = "P: " + BytesToMegabytes(PeakMemoryUsage).ToString("0.00") + " MB";
            memoryTotalText.Text = "T: " + BytesToMegabytes(DeviceTotalMemory).ToString("0.00") + " MB";
        }

        static void timer_Tick(object sender, EventArgs e)
        {
            UpdateMemoryCounterText();
        }

        #region Properties

        public static Int64 CurrentMemoryUsage
        {
            get { return (Int64)DeviceExtendedProperties.GetValue("ApplicationCurrentMemoryUsage"); }
        }

        public static Int64 PeakMemoryUsage
        {
            get { return (Int64)DeviceExtendedProperties.GetValue("ApplicationPeakMemoryUsage"); }
        }

        public static Int64 DeviceTotalMemory
        {
            get { return (Int64)DeviceExtendedProperties.GetValue("DeviceTotalMemory"); }
        }

        #endregion

        #region General Methods

        public static double BytesToMegabytes(Int64 bytes)
        {
            return bytes / 1024d / 1024d;
        }

        #endregion

    }
#endif
}
