// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.MachO;

/// <summary>
/// Kind of Mach-O image, as stored in the <c>filetype</c> header field.
/// </summary>
public enum MachOFileType : uint
{
    /// <summary>Relocatable object file (<c>MH_OBJECT</c>).</summary>
    Object = 0x1,
    /// <summary>Demand paged executable (<c>MH_EXECUTE</c>).</summary>
    Execute = 0x2,
    /// <summary>Fixed VM shared library, obsolete (<c>MH_FVMLIB</c>).</summary>
    FixedVMLibrary = 0x3,
    /// <summary>Core dump (<c>MH_CORE</c>).</summary>
    Core = 0x4,
    /// <summary>Preloaded executable (<c>MH_PRELOAD</c>).</summary>
    Preload = 0x5,
    /// <summary>Dynamically bound shared library (<c>MH_DYLIB</c>).</summary>
    Dylib = 0x6,
    /// <summary>Dynamic link editor (<c>MH_DYLINKER</c>).</summary>
    Dylinker = 0x7,
    /// <summary>Dynamically bound bundle (<c>MH_BUNDLE</c>).</summary>
    Bundle = 0x8,
    /// <summary>Shared library stub for static linking only (<c>MH_DYLIB_STUB</c>).</summary>
    DylibStub = 0x9,
    /// <summary>Companion file carrying only debug sections (<c>MH_DSYM</c>).</summary>
    Dsym = 0xa,
    /// <summary>x86_64 kexts (<c>MH_KEXT_BUNDLE</c>).</summary>
    KextBundle = 0xb,
    /// <summary>Set of Mach-O images linked together, used by kernel collections (<c>MH_FILESET</c>).</summary>
    Fileset = 0xc,
}
