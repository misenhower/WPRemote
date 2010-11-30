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
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace Komodex.DACP
{
    internal static class Utility
    {
        public static void DebugWrite(string message)
        {
#if DEBUG
            Debug.WriteLine(message);
#endif
        }

        public static List<KeyValuePair<string, byte[]>> GetResponseNodes(byte[] data, bool firstOnly = false)
        {
            // TODO: Error checking

            List<KeyValuePair<string, byte[]>> result = new List<KeyValuePair<string, byte[]>>();

            int dataLength = data.Length;
            int location = 0;

            while (location + 8 < dataLength)
            {
                string code = Encoding.UTF8.GetString(data, location, 4);
                int length = BitConverter.ToInt32(data, location + 4).SwapBits();
                byte[] nodeBody = data.Skip(location + 8).Take(length).ToArray();

                result.Add(new KeyValuePair<string, byte[]>(code, nodeBody));

                location += 8 + length;

                if (firstOnly)
                    break;
            }

            return result;
        }

        public static string EscapeSingleQuotes(string input)
        {
            return input.Replace("'", "\\'");
        }

        #region Extension Methods

        public static Int32 SwapBits(this Int32 value)
        {
            UInt32 uvalue = (UInt32)value;
            UInt32 swapped = ((0x000000FF) & (uvalue >> 24)
                             | (0x0000FF00) & (uvalue >> 8)
                             | (0x00FF0000) & (uvalue << 8)
                             | (0xFF000000) & (uvalue << 24));
            return (Int32)swapped;
        }

        public static Int64 SwapBits(this Int64 value)
        {
            UInt64 uvalue = (UInt64)value;
            UInt64 swapped = ((0x00000000000000FF) & (uvalue >> 56)
                             | (0x000000000000FF00) & (uvalue >> 40)
                             | (0x0000000000FF0000) & (uvalue >> 24)
                             | (0x00000000FF000000) & (uvalue >> 8)
                             | (0x000000FF00000000) & (uvalue << 8)
                             | (0x0000FF0000000000) & (uvalue << 24)
                             | (0x00FF000000000000) & (uvalue << 40)
                             | (0xFF00000000000000) & (uvalue << 56));
            return (Int64)swapped;
        }

        public static Int32 GetInt32Value(this byte[] data)
        {
            return BitConverter.ToInt32(data, 0).SwapBits();
        }

        public static Int64 GetInt64Value(this byte[] data)
        {
            return BitConverter.ToInt64(data, 0).SwapBits();
        }

        public static string GetStringValue(this byte[] data)
        {
            return Encoding.UTF8.GetString(data, 0, data.Length);
        }

        #endregion
    }
}
