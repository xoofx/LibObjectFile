using System.Runtime.CompilerServices;

namespace LibObjectFile.Elf
{
    internal readonly struct ElfEncoderSwap : ElfEncoder
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ushort SwapBits(ushort value)
        {
            return (ushort) (((byte) value << 8) | (byte) (value >> 8));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int SwapBits(int value)
        {
            return (int) SwapBits((uint) value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint SwapBits(uint value)
        {
            return ((uint)((byte) value << 24)
                           | ((value & 0x0000FF00) << 16)
                           | ((value & 0x00FF0000) >> 8)
                           | ((value & 0xFF000000) >> 24)
                );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long SwapBits(long value)
        {
            return (long) SwapBits((ulong) value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe ulong SwapBits(ulong value)
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
        
        public void Encode(out RawElf.Elf32_Half dest, ushort value)
        {
            dest = SwapBits(value);
        }

        public void Encode(out RawElf.Elf64_Half dest, ushort value)
        {
            dest = SwapBits(value);
        }

        public void Encode(out RawElf.Elf32_Word dest, uint value)
        {
            dest = SwapBits(value);
        }

        public void Encode(out RawElf.Elf64_Word dest, uint value)
        {
            dest = SwapBits(value);
        }

        public void Encode(out RawElf.Elf32_Sword dest, int value)
        {
            dest = SwapBits(value);
        }

        public void Encode(out RawElf.Elf64_Sword dest, int value)
        {
            dest = SwapBits(value);
        }

        public void Encode(out RawElf.Elf32_Xword dest, ulong value)
        {
            dest = SwapBits(value);
        }

        public void Encode(out RawElf.Elf32_Sxword dest, long value)
        {
            dest = SwapBits(value);
        }

        public void Encode(out RawElf.Elf64_Xword dest, ulong value)
        {
            dest = SwapBits(value);
        }

        public void Encode(out RawElf.Elf64_Sxword dest, long value)
        {
            dest = SwapBits(value);
        }

        public void Encode(out RawElf.Elf32_Addr dest, uint value)
        {
            dest = SwapBits(value);
        }

        public void Encode(out RawElf.Elf64_Addr dest, ulong value)
        {
            dest = SwapBits(value);
        }

        public void Encode(out RawElf.Elf32_Off dest, uint offset)
        {
            dest = SwapBits(offset);
        }

        public void Encode(out RawElf.Elf64_Off dest, ulong offset)
        {
            dest = SwapBits(offset);
        }

        public void Encode(out RawElf.Elf32_Section dest, ushort index)
        {
            dest = SwapBits(index);
        }

        public void Encode(out RawElf.Elf64_Section dest, ushort index)
        {
            dest = SwapBits(index);
        }

        public void Encode(out RawElf.Elf32_Versym dest, ushort value)
        {
            dest = (RawElf.Elf32_Half)SwapBits(value);
        }

        public void Encode(out RawElf.Elf64_Versym dest, ushort value)
        {
            dest = (RawElf.Elf64_Half)SwapBits(value);
        }
    }
}