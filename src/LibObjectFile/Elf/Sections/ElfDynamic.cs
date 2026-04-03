// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.Elf
{
    /// <summary>
    /// Dynamic table entry for ELF.
    /// </summary>
    public record struct ElfDynamic
    {
        /// <summary>
        /// Type of dynamic table entry.
        /// </summary>
        public long Tag { get; set; }

        /// <summary>
        /// Integer value of entry.
        /// </summary>
        public ulong Value { get; set; }

        /// <summary>
        /// Pointer value of entry.
        /// </summary>
        public ulong Pointer => Value;

        /// <summary>
        /// Dynamic Tag
        /// </summary>
        public ElfDynamicTag TagType => (ElfDynamicTag)Tag;
    }
}