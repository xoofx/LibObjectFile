// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.Elf
{
    /// <summary>
    /// Defines a symbol type.
    /// This is the value seen compressed in <see cref="RawElf.Elf32_Sym.st_info"/> or <see cref="RawElf.Elf64_Sym.st_info"/>
    /// as well as the various defines (e.g <see cref="RawElf.STT_NOTYPE"/>).
    /// </summary>
    public enum ElfSymbolType : byte
    {
        /// <summary>
        /// Symbol type is unspecified
        /// </summary>
        NoType = RawElf.STT_NOTYPE,

        /// <summary>
        /// Symbol is a data object
        /// </summary>
        Object = RawElf.STT_OBJECT,

        /// <summary>
        /// Symbol is a code object
        /// </summary>
        Function = RawElf.STT_FUNC,

        /// <summary>
        /// Symbol associated with a section
        /// </summary>
        Section = RawElf.STT_SECTION,

        /// <summary>
        /// Symbol's name is file name
        /// </summary>
        File = RawElf.STT_FILE,

        /// <summary>
        /// Symbol is a common data object
        /// </summary>
        Common = RawElf.STT_COMMON,

        /// <summary>
        /// Symbol is thread-local data object
        /// </summary>
        Tls = RawElf.STT_TLS,

        /// <summary>
        /// Symbol is indirect code object
        /// </summary>
        GnuIndirectFunction = RawElf.STT_GNU_IFUNC,

        /// <summary>
        /// OS-specific 0
        /// </summary>
        SpecificOS0 = RawElf.STT_GNU_IFUNC,

        /// <summary>
        /// OS-specific 1
        /// </summary>
        SpecificOS1 = RawElf.STT_GNU_IFUNC + 1,

        /// <summary>
        /// OS-specific 2
        /// </summary>
        SpecificOS2 = RawElf.STT_GNU_IFUNC + 2,

        /// <summary>
        /// Processor-specific 0
        /// </summary>
        SpecificProcessor0 = RawElf.STT_LOPROC,

        /// <summary>
        /// Processor-specific 1
        /// </summary>
        SpecificProcessor1 = RawElf.STT_LOPROC + 1,

        /// <summary>
        /// Processor-specific 2
        /// </summary>
        SpecificProcessor2 = RawElf.STT_LOPROC + 2,
    }
}