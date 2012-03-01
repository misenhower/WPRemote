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
using System.Windows.Threading;
using System.Threading;
using Microsoft.Phone.Net.NetworkInformation;

namespace Komodex.Common.Phone
{
    public static class PhoneUtility
    {
        #region Binding Helpers

        public static void BindFocusedTextBox()
        {
            try
            {
                TextBox tb = (TextBox)FocusManager.GetFocusedElement();
                var binding = tb.GetBindingExpression(TextBox.TextProperty);
                binding.UpdateSource();
            }
            catch { }
        }

        #endregion

        #region DNS Resolution

        /// <summary>
        /// Synchronously resolves hostnames
        /// </summary>
        public static NameResolutionResult ResolveHostname(DnsEndPoint endpoint)
        {
            AutoResetEvent signal = new AutoResetEvent(false);
            NameResolutionResult nameResolutionResult = null;
            DeviceNetworkInformation.ResolveHostNameAsync(endpoint, (result) =>
            {
                nameResolutionResult = result;
                signal.Set();
            }, null);

            signal.WaitOne();
            return nameResolutionResult;
        }

        #endregion

    }
}
