﻿using System;
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
        public static bool GetBit(this byte value, int index)
        {
            return (value & (1 << index)) != 0;
        }

        /// <summary>
        /// Gets the value of the bit at a specific position.
        /// </summary>
        /// <param name="index">The zero-based index of the value to get.</param>
        /// <returns></returns>
        public static bool GetBit(this ushort value, int index)
        {
            return (value & (1 << index)) != 0;
        }

        /// <summary>
        /// Gets the value of the bit at a specific position.
        /// </summary>
        /// <param name="index">The zero-based index of the value to get.</param>
        /// <returns></returns>
        public static bool GetBit(this uint value, int index)
        {
            return (value & (1 << index)) != 0;
        }

        /// <summary>
        /// Sets the bit at a specific position to the specified value.
        /// </summary>
        /// <param name="index">The zero-based index of the value to set.</param>
        /// <param name="value">The Boolean value to assign to the bit.</param>
        /// <returns></returns>
        public static void SetBit(ref byte input, int index, bool value)
        {
            if (value)
                input = (byte)(input | (1 << index));
            else
                input = (byte)(input & ~(1 << index));
        }

        /// <summary>
        /// Sets the bit at a specific position to the specified value.
        /// </summary>
        /// <param name="index">The zero-based index of the value to set.</param>
        /// <param name="value">The Boolean value to assign to the bit.</param>
        /// <returns></returns>
        public static void SetBit(ref ushort input, int index, bool value)
        {
            if (value)
                input = (ushort)(input | (1 << index));
            else
                input = (ushort)(input & ~(1 << index));
        }

        /// <summary>
        /// Sets the bit at a specific position to the specified value.
        /// </summary>
        /// <param name="index">The zero-based index of the value to set.</param>
        /// <param name="value">The Boolean value to assign to the bit.</param>
        /// <returns></returns>
        public static void SetBit(ref uint input, int index, bool value)
        {
            if (value)
                input = (uint)(input | (uint)(1 << index));
            else
                input = (uint)(input & ~(uint)(1 << index));
        }

        #endregion

        #region Byte Array Extensions

        public static string ToHexString(this byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", "").ToLower();
        }

        #endregion

        #region Hexadecimal String to Byte Array

        public static byte[] FromHexString(string hex)
        {
            if (string.IsNullOrEmpty(hex))
                return null;
            if (hex.Length % 2 != 0)
                throw new ArgumentException("hex");

            byte[] result = new byte[hex.Length / 2];
            for (int i = 0; i < result.Length; i++)
                result[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);

            return result;
        }

        #endregion
    }
}
