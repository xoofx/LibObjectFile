// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.Elf
{
    /// <summary>
    /// Defines the flag of a section.
    /// </summary>
    [Flags]
    public enum ElfSectionFlags : uint
    {
        None = 0,

        /// <summary>
        /// Writable
        /// </summary>
        Write = RawElf.SHF_WRITE,

        /// <summary>
        /// Occupies memory during execution
        /// </summary>
        Alloc = RawElf.SHF_ALLOC,

        /// <summary>
        /// Executable
        /// </summary>
        Executable = RawElf.SHF_EXECINSTR,

        /// <summary>
        /// Might be merged
        /// </summary>
        Merge = RawElf.SHF_MERGE,

        /// <summary>
        /// Contains nul-terminated strings
        /// </summary>
        Strings = RawElf.SHF_STRINGS,

        /// <summary>
        /// `sh_info' contains SHT index
        /// </summary>
        InfoLink = RawElf.SHF_INFO_LINK,

        /// <summary>
        /// Preserve order after combining
        /// </summary>
        LinkOrder = RawElf.SHF_LINK_ORDER,

        /// <summary>
        /// Non-standard OS specific handling required
        /// </summary>
        OsNonConforming = RawElf.SHF_OS_NONCONFORMING,

        /// <summary>
        /// Section is member of a group. 
        /// </summary>
        Group = RawElf.SHF_GROUP,

        /// <summary>
        /// Section hold thread-local data. 
        /// </summary>
        Tls = RawElf.SHF_TLS,

        /// <summary>
        /// Section with compressed data.
        /// </summary>
        Compressed = RawElf.SHF_COMPRESSED,
    }
}