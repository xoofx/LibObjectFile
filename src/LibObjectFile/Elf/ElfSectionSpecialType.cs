// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.Elf;

/// <summary>
/// Defines special sections.
/// </summary>
public enum ElfSectionSpecialType
{
    /// <summary>
    /// No special section type.
    /// </summary>
    None,

    /// <summary>
    /// Uninitialized data section. Default section name is: .bss
    /// </summary>
    Bss,

    /// <summary>
    /// Comment section. Default section name is: .comment
    /// </summary>
    Comment,

    /// <summary>
    /// Initialized data section. Default section name is: .data
    /// </summary>
    Data,

    /// <summary>
    /// Initialized data section (alternative). Default section name is: .data1
    /// </summary>
    Data1,

    /// <summary>
    /// Debugging information section. Default section name is: .debug
    /// </summary>
    Debug,

    /// <summary>
    /// Dynamic linking information section. Default section name is: .dynamic
    /// </summary>
    Dynamic,

    /// <summary>
    /// Dynamic string table section. Default section name is: .dynstr
    /// </summary>
    DynamicStringTable,

    /// <summary>
    /// Dynamic symbol table section. Default section name is: .dynsym
    /// </summary>
    DynamicSymbolTable,

    /// <summary>
    /// Termination function section. Default section name is: .fini
    /// </summary>
    Fini,

    /// <summary>
    /// Global offset table section. Default section name is: .got
    /// </summary>
    Got,

    /// <summary>
    /// Symbol hash table section. Default section name is: .hash
    /// </summary>
    Hash,

    /// <summary>
    /// Initialization function section. Default section name is: .init
    /// </summary>
    Init,

    /// <summary>
    /// Interpreter section. Default section name is: .interp
    /// </summary>
    Interp,

    /// <summary>
    /// Line number information section. Default section name is: .line
    /// </summary>
    Line,

    /// <summary>
    /// Note section. Default section name is: .note
    /// </summary>
    Note,

    /// <summary>
    /// Procedure linkage table section. Default section name is: .plt
    /// </summary>
    Plt,

    /// <summary>
    /// Relocation entries without addends section. Default section name is: .rel
    /// </summary>
    Relocation,

    /// <summary>
    /// Relocation entries with addends section. Default section name is: .rela
    /// </summary>
    RelocationAddends,

    /// <summary>
    /// Read-only data section. Default section name is: .rodata
    /// </summary>
    ReadOnlyData,

    /// <summary>
    /// Read-only data section (alternative). Default section name is: .rodata1
    /// </summary>
    ReadOnlyData1,

    /// <summary>
    /// Section header string table section. Default section name is: .shstrtab
    /// </summary>
    SectionHeaderStringTable,

    /// <summary>
    /// String table section. Default section name is: .strtab
    /// </summary>
    StringTable,

    /// <summary>
    /// Symbol table section. Default section name is: .symtab
    /// </summary>
    SymbolTable,

    /// <summary>
    /// Executable code section. Default section name is: .text
    /// </summary>
    Text,
}