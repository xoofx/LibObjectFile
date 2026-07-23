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
        /// Address of the section header index table for the dynamic symbol table.
        /// </summary>
        SymtabShndx = ElfNative.DT_SYMTAB_SHNDX,

        /// <summary>
        /// Total size of the RELR relative relocation table.
        /// </summary>
        RelrSz = ElfNative.DT_RELRSZ,

        /// <summary>
        /// Address of the RELR relative relocation table.
        /// </summary>
        Relr = ElfNative.DT_RELR,

        /// <summary>
        /// Size of a RELR relative relocation entry.
        /// </summary>
        RelrEnt = ElfNative.DT_RELRENT,

        /// <summary>
        /// Prelinking timestamp (GNU extension).
        /// </summary>
        GnuPrelinked = ElfNative.DT_GNU_PRELINKED,

        /// <summary>
        /// Size of the conflict section (GNU extension).
        /// </summary>
        GnuConflictSz = ElfNative.DT_GNU_CONFLICTSZ,

        /// <summary>
        /// Size of the library list (GNU extension).
        /// </summary>
        GnuLibListSz = ElfNative.DT_GNU_LIBLISTSZ,

        /// <summary>
        /// Checksum of the shared object (GNU extension).
        /// </summary>
        Checksum = ElfNative.DT_CHECKSUM,

        /// <summary>
        /// Size of the PLT padding.
        /// </summary>
        PltPadSz = ElfNative.DT_PLTPADSZ,

        /// <summary>
        /// Size of a <c>move</c> table entry.
        /// </summary>
        MoveEnt = ElfNative.DT_MOVEENT,

        /// <summary>
        /// Total size of the <c>move</c> table.
        /// </summary>
        MoveSz = ElfNative.DT_MOVESZ,

        /// <summary>
        /// Feature selection flags (DTF_*).
        /// </summary>
        Feature1 = ElfNative.DT_FEATURE_1,

        /// <summary>
        /// Flags for the entry immediately following this one.
        /// </summary>
        PosFlag1 = ElfNative.DT_POSFLAG_1,

        /// <summary>
        /// Size of the syminfo table.
        /// </summary>
        SymInSz = ElfNative.DT_SYMINSZ,

        /// <summary>
        /// Size of a syminfo table entry.
        /// </summary>
        SymInEnt = ElfNative.DT_SYMINENT,

        /// <summary>
        /// Address of the GNU-style symbol hash table.
        /// </summary>
        GnuHash = ElfNative.DT_GNU_HASH,

        /// <summary>
        /// Address of the PLT stub for TLS descriptors.
        /// </summary>
        TlsDescPlt = ElfNative.DT_TLSDESC_PLT,

        /// <summary>
        /// Address of the GOT entry for TLS descriptors.
        /// </summary>
        TlsDescGot = ElfNative.DT_TLSDESC_GOT,

        /// <summary>
        /// Address of the conflict section (GNU extension).
        /// </summary>
        GnuConflict = ElfNative.DT_GNU_CONFLICT,

        /// <summary>
        /// Address of the library list (GNU extension).
        /// </summary>
        GnuLibList = ElfNative.DT_GNU_LIBLIST,

        /// <summary>
        /// String table offset of a configuration file.
        /// </summary>
        Config = ElfNative.DT_CONFIG,

        /// <summary>
        /// String table offset of the dependency audit library list.
        /// </summary>
        DepAudit = ElfNative.DT_DEPAUDIT,

        /// <summary>
        /// String table offset of the audit library list.
        /// </summary>
        Audit = ElfNative.DT_AUDIT,

        /// <summary>
        /// Address of the PLT padding.
        /// </summary>
        PltPad = ElfNative.DT_PLTPAD,

        /// <summary>
        /// Address of the <c>move</c> table.
        /// </summary>
        MoveTab = ElfNative.DT_MOVETAB,

        /// <summary>
        /// Address of the syminfo table.
        /// </summary>
        SymInfo = ElfNative.DT_SYMINFO,

        /// <summary>
        /// Address of the symbol version table (<c>.gnu.version</c>).
        /// </summary>
        VerSym = ElfNative.DT_VERSYM,

        /// <summary>
        /// Number of <c>RELATIVE</c> entries at the start of the Rela relocation table.
        /// </summary>
        RelaCount = ElfNative.DT_RELACOUNT,

        /// <summary>
        /// Number of <c>RELATIVE</c> entries at the start of the Rel relocation table.
        /// </summary>
        RelCount = ElfNative.DT_RELCOUNT,

        /// <summary>
        /// State flags (DF_1_*).
        /// </summary>
        Flags1 = ElfNative.DT_FLAGS_1,

        /// <summary>
        /// Address of the version definition table (<c>.gnu.version_d</c>).
        /// </summary>
        VerDef = ElfNative.DT_VERDEF,

        /// <summary>
        /// Number of entries in the version definition table.
        /// </summary>
        VerDefNum = ElfNative.DT_VERDEFNUM,

        /// <summary>
        /// Address of the version needs table (<c>.gnu.version_r</c>).
        /// </summary>
        VerNeed = ElfNative.DT_VERNEED,

        /// <summary>
        /// Number of entries in the version needs table.
        /// </summary>
        VerNeedNum = ElfNative.DT_VERNEEDNUM,

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