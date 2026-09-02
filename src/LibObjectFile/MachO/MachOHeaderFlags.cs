// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.MachO;

/// <summary>
/// Flags of a Mach-O image, as stored in the <c>flags</c> header field.
/// </summary>
[Flags]
public enum MachOHeaderFlags : uint
{
    /// <summary>No flags set.</summary>
    None = 0,
    /// <summary>The image has no undefined references (<c>MH_NOUNDEFS</c>).</summary>
    NoUndefs = 0x1,
    /// <summary>Output of an incremental link, not re-linkable (<c>MH_INCRLINK</c>).</summary>
    IncrementalLink = 0x2,
    /// <summary>The image is input for the dynamic linker and cannot be re-linked (<c>MH_DYLDLINK</c>).</summary>
    DyldLink = 0x4,
    /// <summary>Undefined references are bound by the dynamic linker when loaded (<c>MH_BINDATLOAD</c>).</summary>
    BindAtLoad = 0x8,
    /// <summary>The image has its dynamic undefined references prebound (<c>MH_PREBOUND</c>).</summary>
    Prebound = 0x10,
    /// <summary>The image has its read-only and read-write segments split (<c>MH_SPLIT_SEGS</c>).</summary>
    SplitSegs = 0x20,
    /// <summary>The shared library init routine is to be run lazily, obsolete (<c>MH_LAZY_INIT</c>).</summary>
    LazyInit = 0x40,
    /// <summary>The image uses two-level namespace bindings (<c>MH_TWOLEVEL</c>).</summary>
    TwoLevel = 0x80,
    /// <summary>The executable forces all images to use flat namespace bindings (<c>MH_FORCE_FLAT</c>).</summary>
    ForceFlat = 0x100,
    /// <summary>No multiple definitions of symbols exist, so no lookup is needed (<c>MH_NOMULTIDEFS</c>).</summary>
    NoMultiDefs = 0x200,
    /// <summary>Do not notify the prebinding agent about this executable (<c>MH_NOFIXPREBINDING</c>).</summary>
    NoFixPrebinding = 0x400,
    /// <summary>The binary is not prebound but can have its prebinding redone (<c>MH_PREBINDABLE</c>).</summary>
    Prebindable = 0x800,
    /// <summary>All two-level namespace modules of dependent libraries are bound (<c>MH_ALLMODSBOUND</c>).</summary>
    AllModsBound = 0x1000,
    /// <summary>Sections of object files were divided into subsections by symbol (<c>MH_SUBSECTIONS_VIA_SYMBOLS</c>).</summary>
    SubsectionsViaSymbols = 0x2000,
    /// <summary>The binary has been canonicalized (<c>MH_CANONICAL</c>).</summary>
    Canonical = 0x4000,
    /// <summary>The final linked image contains external weak symbols (<c>MH_WEAK_DEFINES</c>).</summary>
    WeakDefines = 0x8000,
    /// <summary>The final linked image uses weak symbols (<c>MH_BINDS_TO_WEAK</c>).</summary>
    BindsToWeak = 0x10000,
    /// <summary>Stack segments are made executable (<c>MH_ALLOW_STACK_EXECUTION</c>).</summary>
    AllowStackExecution = 0x20000,
    /// <summary>The binary is safe for use in processes with uid zero (<c>MH_ROOT_SAFE</c>).</summary>
    RootSafe = 0x40000,
    /// <summary>The binary is safe for use in processes when issetugid is true (<c>MH_SETUID_SAFE</c>).</summary>
    SetuidSafe = 0x80000,
    /// <summary>The image has no re-exported dylibs, so sub-library resolution can be skipped (<c>MH_NO_REEXPORTED_DYLIBS</c>).</summary>
    NoReexportedDylibs = 0x100000,
    /// <summary>The image is position independent and loads at a random address (<c>MH_PIE</c>).</summary>
    PositionIndependent = 0x200000,
    /// <summary>The linker should strip this dylib if it is not used (<c>MH_DEAD_STRIPPABLE_DYLIB</c>).</summary>
    DeadStrippableDylib = 0x400000,
    /// <summary>The image has thread-local variable descriptors (<c>MH_HAS_TLV_DESCRIPTORS</c>).</summary>
    HasTlvDescriptors = 0x800000,
    /// <summary>The image has no executable heap pages (<c>MH_NO_HEAP_EXECUTION</c>).</summary>
    NoHeapExecution = 0x1000000,
    /// <summary>The code was linked for use in an application extension (<c>MH_APP_EXTENSION_SAFE</c>).</summary>
    AppExtensionSafe = 0x2000000,
    /// <summary>The external symbols in the symbol table do not agree with the dyld info (<c>MH_NLIST_OUTOFSYNC_WITH_DYLDINFO</c>).</summary>
    NListOutOfSyncWithDyldInfo = 0x4000000,
    /// <summary>The image supports running in a simulator (<c>MH_SIM_SUPPORT</c>).</summary>
    SimSupport = 0x8000000,
    /// <summary>The dylib is part of the shared cache rather than a standalone file (<c>MH_DYLIB_IN_CACHE</c>).</summary>
    DylibInCache = 0x80000000,
}
