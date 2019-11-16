// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.Elf
{

    /// <summary>
    /// Defines the File class byte index (32bit or 64bits) of an <see cref="ElfObjectFile"/>.
    /// This is the value seen in the ident part of an Elf header at index <see cref="RawElf.EI_CLASS"/>
    /// It is associated with <see cref="RawElf.ELFCLASSNONE"/>, <see cref="RawElf.ELFCLASS32"/> and <see cref="RawElf.ELFCLASS64"/>
    /// </summary>
    public enum ElfFileClass : byte
    {
        /// <summary>
        /// Invalid class. Equivalent of <see cref="RawElf.ELFCLASSNONE"/>.
        /// </summary>
        None = RawElf.ELFCLASSNONE,

        /// <summary>
        /// 32-bit objects. Equivalent of <see cref="RawElf.ELFCLASS32"/>.
        /// </summary>
        Is32 = RawElf.ELFCLASS32,

        /// <summary>
        /// 64-bit objects. Equivalent of <see cref="RawElf.ELFCLASS64"/>.
        /// </summary>
        Is64 = RawElf.ELFCLASS64,
    }
}