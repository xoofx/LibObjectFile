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
        Null = ElfNative.SHT_NULL,

        /// <summary>
        /// Program data
        /// </summary>
        ProgBits = ElfNative.SHT_PROGBITS,

        /// <summary>
        /// Symbol table
        /// </summary>
        SymbolTable = ElfNative.SHT_SYMTAB,

        /// <summary>
        /// String table
        /// </summary>
        StringTable = ElfNative.SHT_STRTAB,

        /// <summary>
        /// Relocation entries with addends
        /// </summary>
        RelocationAddends = ElfNative.SHT_RELA,

        /// <summary>
        /// Symbol hash table
        /// </summary>
        SymbolHashTable = ElfNative.SHT_HASH,

        /// <summary>
        /// Dynamic linking information 
        /// </summary>
        DynamicLinking = ElfNative.SHT_DYNAMIC,

        /// <summary>
        /// Notes
        /// </summary>
        Note = ElfNative.SHT_NOTE,

        /// <summary>
        /// Program space with no data (bss)
        /// </summary>
        NoBits = ElfNative.SHT_NOBITS,

        /// <summary>
        /// Relocation entries, no addends
        /// </summary>
        Relocation = ElfNative.SHT_REL,

        /// <summary>
        /// Reserved
        /// </summary>
        Shlib = ElfNative.SHT_SHLIB,

        /// <summary>
        /// Dynamic linker symbol table
        /// </summary>
        DynamicLinkerSymbolTable = ElfNative.SHT_DYNSYM,
    }
}