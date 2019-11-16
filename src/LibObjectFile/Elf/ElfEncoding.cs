// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.Elf
{
    /// <summary>
    /// Encoding of an <see cref="ElfObjectFile"/>.
    /// This is the value seen in the ident part of an Elf header at index <see cref="RawElf.EI_DATA"/>
    /// It is associated with <see cref="RawElf.ELFDATANONE"/>, <see cref="RawElf.ELFDATA2LSB"/> and <see cref="RawElf.ELFDATA2MSB"/>
    /// </summary>
    public enum ElfEncoding : byte
    {
        /// <summary>
        /// Invalid data encoding. Equivalent of <see cref="RawElf.ELFDATANONE"/>
        /// </summary>
        None = RawElf.ELFDATANONE,

        /// <summary>
        /// 2's complement, little endian. Equivalent of <see cref="RawElf.ELFDATA2LSB"/>
        /// </summary>
        Lsb = RawElf.ELFDATA2LSB,

        /// <summary>
        /// 2's complement, big endian. Equivalent of <see cref="RawElf.ELFDATA2MSB"/>
        /// </summary>
        Msb = RawElf.ELFDATA2MSB,
    }
}