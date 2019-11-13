using LibObjectFile.Utils;

namespace LibObjectFile.Elf
{
    public readonly struct ElfDecoderSwap : IElfDecoder
    {
        public ushort Decode(RawElf.Elf32_Half src)
        {
            return BinaryUtil.SwapBits(src);
        }

        public ushort Decode(RawElf.Elf64_Half src)
        {
            return BinaryUtil.SwapBits(src);
        }

        public uint Decode(RawElf.Elf32_Word src)
        {
            return BinaryUtil.SwapBits(src);
        }

        public uint Decode(RawElf.Elf64_Word src)
        {
            return BinaryUtil.SwapBits(src);
        }

        public int Decode(RawElf.Elf32_Sword src)
        {
            return BinaryUtil.SwapBits(src);
        }

        public int Decode(RawElf.Elf64_Sword src)
        {
            return BinaryUtil.SwapBits(src);
        }

        public ulong Decode(RawElf.Elf32_Xword src)
        {
            return BinaryUtil.SwapBits(src);
        }

        public long Decode(RawElf.Elf32_Sxword src)
        {
            return BinaryUtil.SwapBits(src);
        }

        public ulong Decode(RawElf.Elf64_Xword src)
        {
            return BinaryUtil.SwapBits(src);
        }

        public long Decode(RawElf.Elf64_Sxword src)
        {
            return BinaryUtil.SwapBits(src);
        }

        public uint Decode(RawElf.Elf32_Addr src)
        {
            return BinaryUtil.SwapBits(src);
        }

        public ulong Decode(RawElf.Elf64_Addr src)
        {
            return BinaryUtil.SwapBits(src);
        }

        public uint Decode(RawElf.Elf32_Off src)
        {
            return BinaryUtil.SwapBits(src);
        }

        public ulong Decode(RawElf.Elf64_Off src)
        {
            return BinaryUtil.SwapBits(src);
        }

        public ushort Decode(RawElf.Elf32_Section src)
        {
            return BinaryUtil.SwapBits(src);
        }

        public ushort Decode(RawElf.Elf64_Section src)
        {
            return BinaryUtil.SwapBits(src);
        }

        public ushort Decode(RawElf.Elf32_Versym src)
        {
            return BinaryUtil.SwapBits((RawElf.Elf32_Half)src);
        }

        public ushort Decode(RawElf.Elf64_Versym src)
        {
            return BinaryUtil.SwapBits((RawElf.Elf64_Half)src);
        }
    }
}