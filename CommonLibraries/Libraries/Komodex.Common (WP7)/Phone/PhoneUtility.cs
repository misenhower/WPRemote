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
using System.IO.IsolatedStorage;
using System.IO;

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

        #region Isolated Storage

        public static string GetFormattedIsolatedStorageContents()
        {
            StringBuilder sb = new StringBuilder();
            using (var isolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                sb.AppendLine("Isolated Storage Contents");
                sb.AppendLine("Quota: " + Utility.ReadableFilesize(isolatedStorage.Quota));
                sb.AppendLine("Available Free Space: " + Utility.ReadableFilesize(isolatedStorage.AvailableFreeSpace));

                sb.AppendLine(".");
                sb.Append(GetDirectoryContent(isolatedStorage, string.Empty, string.Empty));
            }

            return sb.ToString();
        }

        private static string GetDirectoryContent(IsolatedStorageFile isolatedStorage, string directory, string indent)
        {
            StringBuilder sb = new StringBuilder();

            // Get file and directory names
            var fileNames = isolatedStorage.GetFileNames(directory + "/*");
            var dirNames = isolatedStorage.GetDirectoryNames(directory + "/*");

            // List all files
            string fileIndent = indent + ((dirNames.Length > 0) ? "|  " : "   ");
            foreach (var fileName in fileNames)
            {
                // Get file size
                long fileLength;
                using (var file = isolatedStorage.OpenFile(directory + "/" + fileName, FileMode.Open))
                    fileLength = file.Length;

                // Add file info to output
                sb.Append(fileIndent + fileName);
                sb.AppendFormat(" ({0})", Utility.ReadableFilesize(fileLength));
                sb.AppendLine();
            }

            // Add an extra line
            if (fileNames.Length > 0)
                sb.AppendLine(fileIndent);

            // List all directories
            for (int i = 0; i < dirNames.Length; i++)
            {
                string dir = dirNames[i];
                bool last = (i == dirNames.Length - 1);

                sb.Append(indent);

                if (last)
                    sb.Append("`--");
                else
                    sb.Append("+--");
                sb.AppendLine(dir);

                string newIndent = indent + ((last) ? "   " : "|  ");

                sb.Append(GetDirectoryContent(isolatedStorage, directory + "/" + dir, newIndent));
            }
            return sb.ToString();
        }

        #endregion
    }
}
