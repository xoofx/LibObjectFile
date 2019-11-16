// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.Elf
{
    public interface IElfDecoder
    {
        ushort Decode(RawElf.Elf32_Half src);

        ushort Decode(RawElf.Elf64_Half src);

        uint Decode(RawElf.Elf32_Word src);

        uint Decode(RawElf.Elf64_Word src);

        int Decode(RawElf.Elf32_Sword src);

        int Decode(RawElf.Elf64_Sword src);

        ulong Decode(RawElf.Elf32_Xword src);

        long Decode(RawElf.Elf32_Sxword src);

        ulong Decode(RawElf.Elf64_Xword src);

        long Decode(RawElf.Elf64_Sxword src);

        uint Decode(RawElf.Elf32_Addr src);

        ulong Decode(RawElf.Elf64_Addr src);

        uint Decode(RawElf.Elf32_Off src);

        ulong Decode(RawElf.Elf64_Off src);

        ushort Decode(RawElf.Elf32_Section src);

        ushort Decode(RawElf.Elf64_Section src);

        ushort Decode(RawElf.Elf32_Versym src);

        ushort Decode(RawElf.Elf64_Versym src);
    }
}