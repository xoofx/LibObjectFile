// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.Elf;

/// <summary>
/// Extensions for <see cref="ElfSection"/>
/// </summary>
public static class ElfSectionExtension
{
    public static string GetDefaultName(this ElfSectionSpecialType sectionSpecialType)
    {
        return sectionSpecialType switch
        {
            ElfSectionSpecialType.Bss => ".bss",
            ElfSectionSpecialType.Comment => ".comment",
            ElfSectionSpecialType.Data => ".data",
            ElfSectionSpecialType.Data1 => ".data1",
            ElfSectionSpecialType.Debug => ".debug",
            ElfSectionSpecialType.Dynamic => ".dynamic",
            ElfSectionSpecialType.DynamicStringTable => ".dynstr",
            ElfSectionSpecialType.DynamicSymbolTable => ".dynsym",
            ElfSectionSpecialType.Fini => ".fini",
            ElfSectionSpecialType.Got => ".got",
            ElfSectionSpecialType.Hash => ".hash",
            ElfSectionSpecialType.Init => ".init",
            ElfSectionSpecialType.Interp => ".interp",
            ElfSectionSpecialType.Line => ".line",
            ElfSectionSpecialType.Note => ".note",
            ElfSectionSpecialType.Plt => ".plt",
            ElfSectionSpecialType.Relocation => ElfRelocationTable.DefaultName,
            ElfSectionSpecialType.RelocationAddends => ElfRelocationTable.DefaultNameWithAddends,
            ElfSectionSpecialType.ReadOnlyData => ".rodata",
            ElfSectionSpecialType.ReadOnlyData1 => ".rodata1",
            ElfSectionSpecialType.SectionHeaderStringTable => ".shstrtab",
            ElfSectionSpecialType.StringTable => ElfStringTable.DefaultName,
            ElfSectionSpecialType.SymbolTable => ElfSymbolTable.DefaultName,
            ElfSectionSpecialType.Text => ".text",
            _ => throw new InvalidOperationException($"Invalid Enum {sectionSpecialType.GetType()}.{sectionSpecialType}")
        };
    }

    public static ElfSectionType GetSectionType(this ElfSectionSpecialType sectionSpecialType)
    {
        return sectionSpecialType switch
        {
            ElfSectionSpecialType.Bss => ElfSectionType.NoBits,
            ElfSectionSpecialType.Comment => ElfSectionType.ProgBits,
            ElfSectionSpecialType.Data => ElfSectionType.ProgBits,
            ElfSectionSpecialType.Data1 => ElfSectionType.ProgBits,
            ElfSectionSpecialType.Debug => ElfSectionType.ProgBits,
            ElfSectionSpecialType.Dynamic => ElfSectionType.DynamicLinking,
            ElfSectionSpecialType.DynamicStringTable => ElfSectionType.StringTable,
            ElfSectionSpecialType.DynamicSymbolTable => ElfSectionType.DynamicLinkerSymbolTable,
            ElfSectionSpecialType.Fini => ElfSectionType.ProgBits,
            ElfSectionSpecialType.Got => ElfSectionType.ProgBits,
            ElfSectionSpecialType.Hash => ElfSectionType.SymbolHashTable,
            ElfSectionSpecialType.Init => ElfSectionType.ProgBits,
            ElfSectionSpecialType.Interp => ElfSectionType.ProgBits,
            ElfSectionSpecialType.Line => ElfSectionType.ProgBits,
            ElfSectionSpecialType.Note => ElfSectionType.Note,
            ElfSectionSpecialType.Plt => ElfSectionType.ProgBits,
            ElfSectionSpecialType.Relocation => ElfSectionType.Relocation,
            ElfSectionSpecialType.RelocationAddends => ElfSectionType.RelocationAddends,
            ElfSectionSpecialType.ReadOnlyData => ElfSectionType.ProgBits,
            ElfSectionSpecialType.ReadOnlyData1 => ElfSectionType.ProgBits,
            ElfSectionSpecialType.SectionHeaderStringTable => ElfSectionType.StringTable,
            ElfSectionSpecialType.StringTable => ElfSectionType.StringTable,
            ElfSectionSpecialType.SymbolTable => ElfSectionType.SymbolTable,
            ElfSectionSpecialType.Text => ElfSectionType.ProgBits,
            _ => throw new InvalidOperationException($"Invalid Enum {sectionSpecialType.GetType()}.{sectionSpecialType}")
        };
    }

    public static ElfSectionFlags GetSectionFlags(this ElfSectionSpecialType sectionSpecialType)
    {
        return sectionSpecialType switch
        {
            ElfSectionSpecialType.Bss => ElfSectionFlags.Alloc | ElfSectionFlags.Write,
            ElfSectionSpecialType.Comment => ElfSectionFlags.None,
            ElfSectionSpecialType.Data => ElfSectionFlags.Alloc | ElfSectionFlags.Write,
            ElfSectionSpecialType.Data1 => ElfSectionFlags.Alloc | ElfSectionFlags.Write,
            ElfSectionSpecialType.Debug => ElfSectionFlags.None,
            ElfSectionSpecialType.Dynamic => ElfSectionFlags.Alloc,
            ElfSectionSpecialType.DynamicStringTable => ElfSectionFlags.Alloc,
            ElfSectionSpecialType.DynamicSymbolTable => ElfSectionFlags.Alloc,
            ElfSectionSpecialType.Fini => ElfSectionFlags.Alloc | ElfSectionFlags.Executable,
            ElfSectionSpecialType.Got => ElfSectionFlags.None,
            ElfSectionSpecialType.Hash => ElfSectionFlags.None,
            ElfSectionSpecialType.Init => ElfSectionFlags.Alloc | ElfSectionFlags.Executable,
            ElfSectionSpecialType.Interp => ElfSectionFlags.Alloc,
            ElfSectionSpecialType.Line => ElfSectionFlags.None,
            ElfSectionSpecialType.Note => ElfSectionFlags.None,
            ElfSectionSpecialType.Plt => ElfSectionFlags.None,
            ElfSectionSpecialType.Relocation => ElfSectionFlags.None,
            ElfSectionSpecialType.RelocationAddends => ElfSectionFlags.None,
            ElfSectionSpecialType.ReadOnlyData => ElfSectionFlags.Alloc,
            ElfSectionSpecialType.ReadOnlyData1 => ElfSectionFlags.Alloc,
            ElfSectionSpecialType.SectionHeaderStringTable => ElfSectionFlags.None,
            ElfSectionSpecialType.StringTable => ElfSectionFlags.None,
            ElfSectionSpecialType.SymbolTable => ElfSectionFlags.None,
            ElfSectionSpecialType.Text => ElfSectionFlags.Alloc | ElfSectionFlags.Executable,
            _ => throw new InvalidOperationException($"Invalid Enum {sectionSpecialType.GetType()}.{sectionSpecialType}")
        };
    }
}