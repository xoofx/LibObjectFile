using System.Runtime.CompilerServices;

namespace LibObjectFile.Utils
{
    public static class BinaryUtil
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short SwapBits(short value)
        {
            return (short) SwapBits((ushort) value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort SwapBits(ushort value)
        {
            return (ushort)(((byte)value << 8) | (byte)(value >> 8));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SwapBits(int value)
        {
            return (int)SwapBits((uint)value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint SwapBits(uint value)
        {
            return ((uint)((byte)value << 24)
                    | ((value & 0x0000FF00) << 16)
                    | ((value & 0x00FF0000) >> 8)
                    | ((value & 0xFF000000) >> 24)
                );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long SwapBits(long value)
        {
            return (long)SwapBits((ulong)value);
        }

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