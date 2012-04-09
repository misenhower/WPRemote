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
using System.Text;
using System.Security.Cryptography;

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

        #region Encryption

        /// <summary>
        /// Encrypts a string using the ProtectedData class and returns the protected value as a Base 64 string.
        /// </summary>
        public static string EncryptString(string value)
        {
            byte[] valueBytes = Encoding.UTF8.GetBytes(value);
            byte[] protectedBytes = ProtectedData.Protect(valueBytes, null);
            return Convert.ToBase64String(protectedBytes, 0, protectedBytes.Length);
        }

        /// <summary>
        /// Attempts to decrypt a Base 64-encoded string protected with the ProtectedData class.  Returns null if decryption fails.
        /// </summary>
        public static string DecryptString(string value)
        {
            try
            {
                byte[] protectedBytes = Convert.FromBase64String(value);
                byte[] unprotectedBytes = ProtectedData.Unprotect(protectedBytes, null);
                return Encoding.UTF8.GetString(unprotectedBytes, 0, unprotectedBytes.Length);
            }
            catch { return null; }
        }

        #endregion
    }
}
