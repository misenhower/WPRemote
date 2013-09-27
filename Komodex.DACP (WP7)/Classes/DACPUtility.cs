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
using Komodex.Common;

namespace Komodex.DACP
{
    internal static class DACPUtility
    {
        #region Response Parsing

        public static IEnumerable<DACPNode> GetResponseNodes(byte[] data)
        {
            int dataLength = data.Length;
            int location = 0;

            while (location + 8 < dataLength)
            {
                string code = Encoding.UTF8.GetString(data, location, 4);
                int length = BitUtility.NetworkToHostOrder(BitConverter.ToInt32(data, location + 4));
                byte[] nodeBody = new byte[length];
                Buffer.BlockCopy(data, location + 8, nodeBody, 0, length);

                yield return new DACPNode(code, nodeBody);

                location += 8 + length;
            }
        }

        #endregion

        #region DACP Data Extension Methods

        public static Int16 GetInt16Value(this byte[] data)
        {
            return BitUtility.NetworkToHostOrder(BitConverter.ToInt16(data, 0));
        }

        public static Int32 GetInt32Value(this byte[] data)
        {
            return BitUtility.NetworkToHostOrder(BitConverter.ToInt32(data, 0));
        }

        public static Int64 GetInt64Value(this byte[] data)
        {
            return BitUtility.NetworkToHostOrder(BitConverter.ToInt64(data, 0));
        }

        public static string GetStringValue(this byte[] data)
        {
            return Encoding.UTF8.GetString(data, 0, data.Length);
        }

        public static bool GetBoolValue(byte[] data)
        {
            return !(data[0] == 0);
        }

        public static DateTime GetDateTimeValue(this byte[] data)
        {
            int timestamp = GetInt32Value(data);
            return (new DateTime(1970, 1, 1)).AddSeconds(timestamp);
        }

        #endregion

        #region Other Extension Methods

        public static string EscapeSingleQuotes(string input)
        {
            return input.Replace("'", "\\'");
        }

        public static string QueryEncodeString(string input)
        {
            return Uri.EscapeDataString(DACPUtility.EscapeSingleQuotes(input));
        }

        public static string GetPathAndQueryString(this Uri uri)
        {
            return uri.AbsolutePath + uri.Query;
        }

        #endregion
    }
}
