// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Runtime.CompilerServices;

namespace LibObjectFile.Utils
{
    /// <summary>
    /// Binary helper class to swap LSB/MSB integers.
    /// </summary>
    public static class BinaryUtil
    {
        /// <summary>
        /// Swap the bits LSB/MSB for the specified value.
        /// </summary>
        /// <param name="value">The value to swap the LSB/MSB bits</param>
        /// <returns>The value swapped</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short SwapBits(short value)
        {
            return (short) SwapBits((ushort) value);
        }

        /// <summary>
        /// Swap the bits LSB/MSB for the specified value.
        /// </summary>
        /// <param name="value">The value to swap the LSB/MSB bits</param>
        /// <returns>The value swapped</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort SwapBits(ushort value)
        {
            return (ushort)(((byte)value << 8) | (byte)(value >> 8));
        }

        /// <summary>
        /// Swap the bits LSB/MSB for the specified value.
        /// </summary>
        /// <param name="value">The value to swap the LSB/MSB bits</param>
        /// <returns>The value swapped</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SwapBits(int value)
        {
            return (int)SwapBits((uint)value);
        }

        /// <summary>
        /// Swap the bits LSB/MSB for the specified value.
        /// </summary>
        /// <param name="value">The value to swap the LSB/MSB bits</param>
        /// <returns>The value swapped</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint SwapBits(uint value)
        {
            return ((uint)((byte)value << 24)
                    | ((value & 0x0000FF00) << 8)
                    | ((value & 0x00FF0000) >> 8)
                    | ((value & 0xFF000000) >> 24)
                );
        }

        /// <summary>
        /// Swap the bits LSB/MSB for the specified value.
        /// </summary>
        /// <param name="value">The value to swap the LSB/MSB bits</param>
        /// <returns>The value swapped</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long SwapBits(long value)
        {
            return (long)SwapBits((ulong)value);
        }

        /// <summary>
        /// Swap the bits LSB/MSB for the specified value.
        /// </summary>
        /// <param name="value">The value to swap the LSB/MSB bits</param>
        /// <returns>The value swapped</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe ulong SwapBits(ulong value)
        {
            ulong dest;
            var pDest = (byte*)&dest;
            var pSrc = (byte*)&value + 7;
            *pDest++ = *pSrc--;
            *pDest++ = *pSrc--;
            *pDest++ = *pSrc--;
            *pDest++ = *pSrc--;
            *pDest++ = *pSrc--;
            *pDest++ = *pSrc--;
            *pDest++ = *pSrc--;
            *pDest = *pSrc;
            return dest;
        }
    }
}