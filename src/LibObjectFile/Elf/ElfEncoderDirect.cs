// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.Elf
{
    internal readonly struct ElfEncoderDirect : IElfEncoder
    {
        public void Encode(out RawElf.Elf32_Half dest, ushort value)
        {
            dest = value;
        }

        public void Encode(out RawElf.Elf64_Half dest, ushort value)
        {
            dest = value;
        }

        public void Encode(out RawElf.Elf32_Word dest, uint value)
        {
            dest = value;
        }

        public void Encode(out RawElf.Elf64_Word dest, uint value)
        {
            dest = value;
        }

        public void Encode(out RawElf.Elf32_Sword dest, int value)
        {
            dest = value;
        }

        public void Encode(out RawElf.Elf64_Sword dest, int value)
        {
            dest = value;
        }

        public void Encode(out RawElf.Elf32_Xword dest, ulong value)
        {
            dest = value;
        }

        public void Encode(out RawElf.Elf32_Sxword dest, long value)
        {
            dest = value;
        }

        public void Encode(out RawElf.Elf64_Xword dest, ulong value)
        {
            dest = value;
        }

        public void Encode(out RawElf.Elf64_Sxword dest, long value)
        {
            dest = value;
        }

        public void Encode(out RawElf.Elf32_Addr dest, uint value)
        {
            dest = value;
        }

        public void Encode(out RawElf.Elf64_Addr dest, ulong value)
        {
            dest = value;
        }

        public void Encode(out RawElf.Elf32_Off dest, uint offset)
        {
            dest = offset;
        }

        public void Encode(out RawElf.Elf64_Off dest, ulong offset)
        {
            dest = offset;
        }

        public void Encode(out RawElf.Elf32_Section dest, ushort index)
        {
            dest = index;
        }

        public void Encode(out RawElf.Elf64_Section dest, ushort index)
        {
            dest = index;
        }

        public void Encode(out RawElf.Elf32_Versym dest, ushort value)
        {
            dest = (RawElf.Elf32_Half)value;
        }

        public void Encode(out RawElf.Elf64_Versym dest, ushort value)
        {
            dest = (RawElf.Elf64_Half)value;
        }
    }
}