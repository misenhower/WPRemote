using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Komodex.Common
{
    public static class BitUtility
    {
        #region Host/Network Order Conversion

        public static short HostToNetworkOrder(short host)
        {
            return (short)(((host & 0xFF) << 8) | ((host >> 8) & 0xFF));
        }

        public static int HostToNetworkOrder(int host)
        {
            return (((HostToNetworkOrder((short)host) & 0xFFFF) << 16) | (HostToNetworkOrder((short)(host >> 16)) & 0xFFFF));
        }

        public static long HostToNetworkOrder(long host)
        {
            return (long)(((HostToNetworkOrder((int)host) & 0xFFFFFFFFL) << 32) | (HostToNetworkOrder((int)(host >> 32)) & 0xFFFFFFFFL));
        }

        public static short NetworkToHostOrder(short network)
        {
            return HostToNetworkOrder(network);
        }

        public static int NetworkToHostOrder(int network)
        {
            return HostToNetworkOrder(network);
        }

        public static long NetworkToHostOrder(long network)
        {
            return HostToNetworkOrder(network);
        }

        #endregion

        #region Bit Manipulation Extensions

        /// <summary>
        /// Gets the value of the bit at a specific position.
        /// </summary>
        /// <param name="index">The zero-based index of the value to get.</param>
        /// <returns></returns>
        public static bool GetBit(this byte b, int index)
        {
            return (b & (1 << index)) != 0;
        }

        /// <summary>
        /// Sets the bit at a specific position to the specified value.
        /// </summary>
        /// <param name="index">The zero-based index of the value to get.</param>
        /// <param name="value">The Boolean value to assign to the bit.</param>
        /// <returns></returns>
        public static void SetBit(ref byte b, int index, bool value)
        {
            if (value)
                b = (byte)(b | (1 << index));
            else
                b = (byte)(b & ~(1 << index));
        }

        #endregion

        #region Byte Array Extensions

        public static string ToHexString(this byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", "").ToLower();
        }

        #endregion
    }
}
