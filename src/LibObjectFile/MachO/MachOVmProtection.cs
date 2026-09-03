// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.MachO;

/// <summary>
/// Virtual memory protection applied to a segment, as stored in the <c>maxprot</c> and
/// <c>initprot</c> fields of a segment load command.
/// </summary>
[Flags]
public enum MachOVmProtection : uint
{
    /// <summary>No access (<c>VM_PROT_NONE</c>).</summary>
    None = 0,
    /// <summary>Read access (<c>VM_PROT_READ</c>).</summary>
    Read = 0x1,
    /// <summary>Write access (<c>VM_PROT_WRITE</c>).</summary>
    Write = 0x2,
    /// <summary>Execute access (<c>VM_PROT_EXECUTE</c>).</summary>
    Execute = 0x4,
}
