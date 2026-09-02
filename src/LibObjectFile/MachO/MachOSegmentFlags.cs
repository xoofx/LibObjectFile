// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.MachO;

/// <summary>
/// Flags of a segment, as stored in the <c>flags</c> field of a segment load command.
/// </summary>
[Flags]
public enum MachOSegmentFlags : uint
{
    /// <summary>No flags set.</summary>
    None = 0,
    /// <summary>The file contents of this segment are for the high part of the VM space (<c>SG_HIGHVM</c>).</summary>
    HighVM = 0x1,
    /// <summary>The segment is the virtual memory module table, obsolete (<c>SG_FVMLIB</c>).</summary>
    FixedVMLibrary = 0x2,
    /// <summary>The segment contents are exactly the file contents and may be mapped directly (<c>SG_NORELOC</c>).</summary>
    NoReloc = 0x4,
    /// <summary>The first page is protected, and the rest becomes read-only after relocation (<c>SG_PROTECTED_VERSION_1</c>).</summary>
    ProtectedVersion1 = 0x8,
    /// <summary>The segment is read-only after the fixups have been applied (<c>SG_READ_ONLY</c>).</summary>
    ReadOnly = 0x10,
}
