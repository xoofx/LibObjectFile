using LibObjectFile.Utils;

namespace LibObjectFile.Elf
{
    internal readonly struct ElfEncoderSwap : IElfEncoder
    {
        public void Encode(out RawElf.Elf32_Half dest, ushort value)
        {
            dest = BinaryUtil.SwapBits(value);
        }

        public void Encode(out RawElf.Elf64_Half dest, ushort value)
        {
            dest = BinaryUtil.SwapBits(value);
        }

        public void Encode(out RawElf.Elf32_Word dest, uint value)
        {
            dest = BinaryUtil.SwapBits(value);
        }

        public void Encode(out RawElf.Elf64_Word dest, uint value)
        {
            dest = BinaryUtil.SwapBits(value);
        }

        public void Encode(out RawElf.Elf32_Sword dest, int value)
        {
            dest = BinaryUtil.SwapBits(value);
        }

        public void Encode(out RawElf.Elf64_Sword dest, int value)
        {
            dest = BinaryUtil.SwapBits(value);
        }

        public void Encode(out RawElf.Elf32_Xword dest, ulong value)
        {
            dest = BinaryUtil.SwapBits(value);
        }

        public void Encode(out RawElf.Elf32_Sxword dest, long value)
        {
            dest = BinaryUtil.SwapBits(value);
        }

        public void Encode(out RawElf.Elf64_Xword dest, ulong value)
        {
            dest = BinaryUtil.SwapBits(value);
        }

        public void Encode(out RawElf.Elf64_Sxword dest, long value)
        {
            dest = BinaryUtil.SwapBits(value);
        }

        public void Encode(out RawElf.Elf32_Addr dest, uint value)
        {
            dest = BinaryUtil.SwapBits(value);
        }

        public void Encode(out RawElf.Elf64_Addr dest, ulong value)
        {
            dest = BinaryUtil.SwapBits(value);
        }

        public void Encode(out RawElf.Elf32_Off dest, uint offset)
        {
            dest = BinaryUtil.SwapBits(offset);
        }

        public void Encode(out RawElf.Elf64_Off dest, ulong offset)
        {
            dest = BinaryUtil.SwapBits(offset);
        }

        public void Encode(out RawElf.Elf32_Section dest, ushort index)
        {
            dest = BinaryUtil.SwapBits(index);
        }

        public void Encode(out RawElf.Elf64_Section dest, ushort index)
        {
            dest = BinaryUtil.SwapBits(index);
        }

        public void Encode(out RawElf.Elf32_Versym dest, ushort value)
        {
            dest = (RawElf.Elf32_Half)BinaryUtil.SwapBits(value);
        }

        public void Encode(out RawElf.Elf64_Versym dest, ushort value)
        {
            dest = (RawElf.Elf64_Half)BinaryUtil.SwapBits(value);
        }
    }
}