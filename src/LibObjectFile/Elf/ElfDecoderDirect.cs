namespace LibObjectFile.Elf
{
    public struct ElfDecoderDirect : IElfDecoder
    {
        public ushort Decode(RawElf.Elf32_Half src)
        {
            return src.Value;
        }

        public ushort Decode(RawElf.Elf64_Half src)
        {
            return src.Value;
        }

        public uint Decode(RawElf.Elf32_Word src)
        {
            return src.Value;
        }

        public uint Decode(RawElf.Elf64_Word src)
        {
            return src.Value;
        }

        public int Decode(RawElf.Elf32_Sword src)
        {
            return src.Value;
        }

        public int Decode(RawElf.Elf64_Sword src)
        {
            return src.Value;
        }

        public ulong Decode(RawElf.Elf32_Xword src)
        {
            return src.Value;
        }

        public long Decode(RawElf.Elf32_Sxword src)
        {
            return src.Value;
        }

        public ulong Decode(RawElf.Elf64_Xword src)
        {
            return src.Value;
        }

        public long Decode(RawElf.Elf64_Sxword src)
        {
            return src.Value;
        }

        public uint Decode(RawElf.Elf32_Addr src)
        {
            return src.Value;
        }

        public ulong Decode(RawElf.Elf64_Addr src)
        {
            return src.Value;
        }

        public uint Decode(RawElf.Elf32_Off src)
        {
            return src.Value;
        }

        public ulong Decode(RawElf.Elf64_Off src)
        {
            return src.Value;
        }

        public ushort Decode(RawElf.Elf32_Section src)
        {
            return src.Value;
        }

        public ushort Decode(RawElf.Elf64_Section src)
        {
            return src.Value;
        }

        public ushort Decode(RawElf.Elf32_Versym src)
        {
            return src.Value;
        }

        public ushort Decode(RawElf.Elf64_Versym src)
        {
            return src.Value;
        }
    }
}