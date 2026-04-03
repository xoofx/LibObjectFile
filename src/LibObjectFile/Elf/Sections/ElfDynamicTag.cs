// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.Elf
{
    /// <summary>
    /// Dynamic table entry tags.
    /// </summary>
    public enum ElfDynamicTag : uint
    {
        /// <summary>
        /// Null
        /// </summary>
        Null = ElfNative.DT_NULL,

        /// <summary>
        /// String table offset of needed library.
        /// </summary>
        Needed = ElfNative.DT_NEEDED,

        /// <summary>
        /// Size of relocation entries in PLT.
        /// </summary>
        PltRelSz = ElfNative.DT_PLTRELSZ,

        /// <summary>
        /// Address associated with linkage table.
        /// </summary>
        PltGot = ElfNative.DT_PLTGOT,

        /// <summary>
        /// Address of symbolic hash table.
        /// </summary>
        Hash = ElfNative.DT_HASH,

        /// <summary>
        /// Address of dynamic string table.
        /// </summary>
        StrTab = ElfNative.DT_STRTAB,

        /// <summary>
        /// Address of dynamic symbol table.
        /// </summary>
        SymTab = ElfNative.DT_SYMTAB,

        /// <summary>
        /// Address of relocation table (Rela entries).
        /// </summary>
        Rela = ElfNative.DT_RELA,

        /// <summary>
        /// Size of Rela relocation table.
        /// </summary>
        RelaSz = ElfNative.DT_RELASZ,

        /// <summary>
        /// Size of a Rela relocation entry.
        /// </summary>
        RelaEnt = ElfNative.DT_RELAENT,

        /// <summary>
        /// Total size of the string table.
        /// </summary>
        StrSz = ElfNative.DT_STRSZ,

        /// <summary>
        /// Size of a symbol table entry.
        /// </summary>
        SymEnt = ElfNative.DT_SYMENT,

        /// <summary>
        /// Address of initialization function.
        /// </summary>
        Init = ElfNative.DT_INIT,

        /// <summary>
        /// Address of termination function.
        /// </summary>
        Fini = ElfNative.DT_FINI,

        /// <summary>
        /// String table offset of a shared object's name.
        /// </summary>
        SoName = ElfNative.DT_SONAME,

        /// <summary>
        /// String table offset of library search path.
        /// </summary>
        RPath = ElfNative.DT_RPATH,

        /// <summary>
        /// Changes symbol resolution algorithm.
        /// </summary>
        Symbolic = ElfNative.DT_SYMBOLIC,

        /// <summary>
        /// Address of relocation table (Rel entries).
        /// </summary>
        Rel = ElfNative.DT_REL,

        /// <summary>
        /// Size of Rel relocation table.
        /// </summary>
        RelSz = ElfNative.DT_RELSZ,

        /// <summary>
        /// Size of a Rel relocation entry.
        /// </summary>
        RelEnt = ElfNative.DT_RELENT,

        /// <summary>
        /// Type of relocation entry used for linking.
        /// </summary>
        PltRel = ElfNative.DT_PLTREL,

        /// <summary>
        /// Reserved for debugger.
        /// </summary>
        Debug = ElfNative.DT_DEBUG,

        /// <summary>
        /// Relocations exist for non-writable segments.
        /// </summary>
        TextRel = ElfNative.DT_TEXTREL,

        /// <summary>
        /// Address of relocations associated with PLT.
        /// </summary>
        JmpRel = ElfNative.DT_JMPREL,

        /// <summary>
        /// Process all relocations before execution.
        /// </summary>
        BindNow = ElfNative.DT_BIND_NOW,

        /// <summary>
        /// Pointer to array of initialization functions.
        /// </summary>
        InitArray = ElfNative.DT_INIT_ARRAY,

        /// <summary>
        /// Pointer to array of termination functions.
        /// </summary>
        FiniArray = ElfNative.DT_FINI_ARRAY,

        /// <summary>
        /// Size of <see cref="InitArray"/>.
        /// </summary>
        InitArraySz = ElfNative.DT_INIT_ARRAYSZ,

        /// <summary>
        /// Size of <see cref="FiniArray"/>.
        /// </summary>
        FiniArraySz = ElfNative.DT_FINI_ARRAYSZ,

        /// <summary>
        /// String table offset of library search path.
        /// </summary>
        RunPath = ElfNative.DT_RUNPATH,

        /// <summary>
        /// Flags.
        /// </summary>
        Flags = ElfNative.DT_FLAGS,

        /// <summary>
        /// Values from here to <c>DT_LOOS</c> follow the rules for the interpretation of the <c>d_un</c> union.
        /// </summary>
        Encoding = ElfNative.DT_ENCODING,

        /// <summary>
        /// Pointer to array of preinit functions.
        /// </summary>
        PreInitArray = ElfNative.DT_PREINIT_ARRAY,

        /// <summary>
        /// Size of the DT_PREINIT_ARRAY array.
        /// </summary>
        PreInitArraySz = ElfNative.DT_PREINIT_ARRAYSZ,

        /// <summary>
        /// Start of environment specific tags.
        /// </summary>
        LoOs         = ElfNative.DT_LOOS,

        /// <summary>
        /// End of environment specific tags.
        /// </summary>
        HiOS         = ElfNative.DT_HIOS,

        /// <summary>
        /// Start of processor specific tags.
        /// </summary>
        LoProc       = ElfNative.DT_LOPROC,

        /// <summary>
        /// End of processor specific tags.
        /// </summary>
        HiProc       = ElfNative.DT_HIPROC
    }
}