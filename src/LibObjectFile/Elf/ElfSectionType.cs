// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.Elf
{
    /// <summary>
    /// Defines the type of a section.
    /// </summary>
    public enum ElfSectionType : uint
    {
        /// <summary>
        /// Section header table entry unused
        /// </summary>
        Null = RawElf.SHT_NULL,

        /// <summary>
        /// Program data
        /// </summary>
        ProgBits = RawElf.SHT_PROGBITS,

        /// <summary>
        /// Symbol table
        /// </summary>
        SymbolTable = RawElf.SHT_SYMTAB,

        /// <summary>
        /// String table
        /// </summary>
        StringTable = RawElf.SHT_STRTAB,

        /// <summary>
        /// Relocation entries with addends
        /// </summary>
        RelocationAddends = RawElf.SHT_RELA,

        /// <summary>
        /// Symbol hash table
        /// </summary>
        SymbolHashTable = RawElf.SHT_HASH,

        /// <summary>
        /// Dynamic linking information 
        /// </summary>
        DynamicLinking = RawElf.SHT_DYNAMIC,

        /// <summary>
        /// Notes
        /// </summary>
        Note = RawElf.SHT_NOTE,

        /// <summary>
        /// Program space with no data (bss)
        /// </summary>
        NoBits = RawElf.SHT_NOBITS,

        /// <summary>
        /// Relocation entries, no addends
        /// </summary>
        Relocation = RawElf.SHT_REL,

        /// <summary>
        /// Reserved
        /// </summary>
        Shlib = RawElf.SHT_SHLIB,

        /// <summary>
        /// Dynamic linker symbol table
        /// </summary>
        DynamicLinkerSymbolTable = RawElf.SHT_DYNSYM,
    }
}