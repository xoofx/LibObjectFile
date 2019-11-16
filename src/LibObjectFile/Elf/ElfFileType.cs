// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.Elf
{
    public enum ElfFileType : ushort
    {
        /// <summary>
        /// No file type
        /// </summary>
        None = RawElf.ET_NONE,

        /// <summary>
        /// Relocatable file
        /// </summary>
        Relocatable = RawElf.ET_REL,

        /// <summary>
        /// Executable file
        /// </summary>
        Executable = RawElf.ET_EXEC,

        /// <summary>
        /// Shared object file 
        /// </summary>
        Dynamic = RawElf.ET_DYN,

        /// <summary>
        /// Core file
        /// </summary>
        Core = RawElf.ET_CORE,
    }
}