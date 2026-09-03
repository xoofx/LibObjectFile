// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.MachO;

/// <summary>
/// Type of a load command, as stored in the <c>cmd</c> field of a Mach-O load command.
/// </summary>
/// <remarks>
/// The high bit <see cref="RequiredByDynamicLinker"/> is part of the stored value, not a
/// separate flag. It marks commands that dyld must understand to load the image at all, so a
/// loader that does not recognise one has to refuse the file rather than skip the command.
/// </remarks>
public enum MachOLoadCommandType : uint
{
    /// <summary>Set on commands that dyld is required to understand rather than skip.</summary>
    RequiredByDynamicLinker = 0x80000000,

    /// <summary>32-bit segment of the file mapped into memory (<c>LC_SEGMENT</c>).</summary>
    Segment = 0x1,
    /// <summary>Symbol table and string table location (<c>LC_SYMTAB</c>).</summary>
    SymbolTable = 0x2,
    /// <summary>Gdb symbol table info, obsolete (<c>LC_SYMSEG</c>).</summary>
    SymbolSegment = 0x3,
    /// <summary>Thread state, without a stack (<c>LC_THREAD</c>).</summary>
    Thread = 0x4,
    /// <summary>Thread state and stack, used as the entry point before <c>LC_MAIN</c> (<c>LC_UNIXTHREAD</c>).</summary>
    UnixThread = 0x5,
    /// <summary>Load a fixed VM shared library, obsolete (<c>LC_LOADFVMLIB</c>).</summary>
    LoadFixedVMLibrary = 0x6,
    /// <summary>Fixed VM shared library identification, obsolete (<c>LC_IDFVMLIB</c>).</summary>
    IdFixedVMLibrary = 0x7,
    /// <summary>Object identification, obsolete (<c>LC_IDENT</c>).</summary>
    Identification = 0x8,
    /// <summary>Fixed VM file inclusion, obsolete (<c>LC_FVMFILE</c>).</summary>
    FixedVMFile = 0x9,
    /// <summary>Prepage command, obsolete (<c>LC_PREPAGE</c>).</summary>
    Prepage = 0xa,
    /// <summary>Dynamic link editor symbol table info (<c>LC_DYSYMTAB</c>).</summary>
    DynamicSymbolTable = 0xb,
    /// <summary>Load a dynamically linked shared library (<c>LC_LOAD_DYLIB</c>).</summary>
    LoadDylib = 0xc,
    /// <summary>Identification of a dynamically linked shared library (<c>LC_ID_DYLIB</c>).</summary>
    IdDylib = 0xd,
    /// <summary>Load a dynamic linker (<c>LC_LOAD_DYLINKER</c>).</summary>
    LoadDylinker = 0xe,
    /// <summary>Dynamic linker identification (<c>LC_ID_DYLINKER</c>).</summary>
    IdDylinker = 0xf,
    /// <summary>Modules prebound for a dynamically linked shared library (<c>LC_PREBOUND_DYLIB</c>).</summary>
    PreboundDylib = 0x10,
    /// <summary>Image routines (<c>LC_ROUTINES</c>).</summary>
    Routines = 0x11,
    /// <summary>Sub framework (<c>LC_SUB_FRAMEWORK</c>).</summary>
    SubFramework = 0x12,
    /// <summary>Sub umbrella (<c>LC_SUB_UMBRELLA</c>).</summary>
    SubUmbrella = 0x13,
    /// <summary>Sub client (<c>LC_SUB_CLIENT</c>).</summary>
    SubClient = 0x14,
    /// <summary>Sub library (<c>LC_SUB_LIBRARY</c>).</summary>
    SubLibrary = 0x15,
    /// <summary>Two-level namespace lookup hints (<c>LC_TWOLEVEL_HINTS</c>).</summary>
    TwoLevelHints = 0x16,
    /// <summary>Prebind checksum (<c>LC_PREBIND_CKSUM</c>).</summary>
    PrebindChecksum = 0x17,
    /// <summary>Load a weak dylib, tolerated as missing at runtime (<c>LC_LOAD_WEAK_DYLIB</c>).</summary>
    LoadWeakDylib = 0x18 | RequiredByDynamicLinker,
    /// <summary>64-bit segment of the file mapped into memory (<c>LC_SEGMENT_64</c>).</summary>
    Segment64 = 0x19,
    /// <summary>64-bit image routines (<c>LC_ROUTINES_64</c>).</summary>
    Routines64 = 0x1a,
    /// <summary>The image UUID (<c>LC_UUID</c>).</summary>
    Uuid = 0x1b,
    /// <summary>Runpath additions used to resolve <c>@rpath</c> (<c>LC_RPATH</c>).</summary>
    RPath = 0x1c | RequiredByDynamicLinker,
    /// <summary>Local of code signature (<c>LC_CODE_SIGNATURE</c>).</summary>
    CodeSignature = 0x1d,
    /// <summary>Local of info to split segments (<c>LC_SEGMENT_SPLIT_INFO</c>).</summary>
    SegmentSplitInfo = 0x1e,
    /// <summary>Load and re-export a dylib (<c>LC_REEXPORT_DYLIB</c>).</summary>
    ReexportDylib = 0x1f | RequiredByDynamicLinker,
    /// <summary>Delay load of a dylib until first use (<c>LC_LAZY_LOAD_DYLIB</c>).</summary>
    LazyLoadDylib = 0x20,
    /// <summary>Encrypted segment information (<c>LC_ENCRYPTION_INFO</c>).</summary>
    EncryptionInfo = 0x21,
    /// <summary>Compressed dyld information (<c>LC_DYLD_INFO</c>).</summary>
    DyldInfo = 0x22,
    /// <summary>Compressed dyld information only, with no classic relocations (<c>LC_DYLD_INFO_ONLY</c>).</summary>
    DyldInfoOnly = 0x22 | RequiredByDynamicLinker,
    /// <summary>Load an upward dylib (<c>LC_LOAD_UPWARD_DYLIB</c>).</summary>
    LoadUpwardDylib = 0x23 | RequiredByDynamicLinker,
    /// <summary>Build for macOS minimum OS version (<c>LC_VERSION_MIN_MACOSX</c>).</summary>
    VersionMinMacOSX = 0x24,
    /// <summary>Build for iPhoneOS minimum OS version (<c>LC_VERSION_MIN_IPHONEOS</c>).</summary>
    VersionMinIPhoneOS = 0x25,
    /// <summary>Compressed table of function start addresses (<c>LC_FUNCTION_STARTS</c>).</summary>
    FunctionStarts = 0x26,
    /// <summary>String for dyld to treat like an environment variable (<c>LC_DYLD_ENVIRONMENT</c>).</summary>
    DyldEnvironment = 0x27,
    /// <summary>Replacement for <c>LC_UNIXTHREAD</c> as the entry point (<c>LC_MAIN</c>).</summary>
    Main = 0x28 | RequiredByDynamicLinker,
    /// <summary>Table of non-instructions in <c>__text</c> (<c>LC_DATA_IN_CODE</c>).</summary>
    DataInCode = 0x29,
    /// <summary>Source version used to build the binary (<c>LC_SOURCE_VERSION</c>).</summary>
    SourceVersion = 0x2a,
    /// <summary>Code signing designated requirements copied from linked dylibs (<c>LC_DYLIB_CODE_SIGN_DRS</c>).</summary>
    DylibCodeSignDrs = 0x2b,
    /// <summary>64-bit encrypted segment information (<c>LC_ENCRYPTION_INFO_64</c>).</summary>
    EncryptionInfo64 = 0x2c,
    /// <summary>Linker options embedded in object files (<c>LC_LINKER_OPTION</c>).</summary>
    LinkerOption = 0x2d,
    /// <summary>Optimization hints in object files (<c>LC_LINKER_OPTIMIZATION_HINT</c>).</summary>
    LinkerOptimizationHint = 0x2e,
    /// <summary>Build for tvOS minimum OS version (<c>LC_VERSION_MIN_TVOS</c>).</summary>
    VersionMinTvOS = 0x2f,
    /// <summary>Build for watchOS minimum OS version (<c>LC_VERSION_MIN_WATCHOS</c>).</summary>
    VersionMinWatchOS = 0x30,
    /// <summary>Arbitrary data included within a Mach-O file (<c>LC_NOTE</c>).</summary>
    Note = 0x31,
    /// <summary>Build for platform minimum OS version, replacing the per-platform commands (<c>LC_BUILD_VERSION</c>).</summary>
    BuildVersion = 0x32,
    /// <summary>Exported symbols trie, split out of the dyld info (<c>LC_DYLD_EXPORTS_TRIE</c>).</summary>
    DyldExportsTrie = 0x33 | RequiredByDynamicLinker,
    /// <summary>Chained fixups, replacing the rebase and bind opcode streams (<c>LC_DYLD_CHAINED_FIXUPS</c>).</summary>
    DyldChainedFixups = 0x34 | RequiredByDynamicLinker,
    /// <summary>Entry in a fileset, used by the kernel collections (<c>LC_FILESET_ENTRY</c>).</summary>
    FilesetEntry = 0x35 | RequiredByDynamicLinker,
    /// <summary>Atom information for live linking (<c>LC_ATOM_INFO</c>).</summary>
    AtomInfo = 0x36,
}
