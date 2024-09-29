// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace LibObjectFile.PE;

/// <summary>
/// A base relocation in a Portable Executable (PE) image.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public readonly struct PEBaseRelocation
{
    public const ushort MaxVirtualOffset = (1 << 12) - 1;
    private const ushort TypeMask = unchecked((ushort)~MaxVirtualOffset);
    private const ushort VirtualOffsetMask = MaxVirtualOffset;

    private readonly ushort _value;

    public PEBaseRelocation(ushort value)
    {
        _value = value;
    }

    public PEBaseRelocation(PEBaseRelocationType type, ushort offsetInBlock)
    {
        _value = (ushort)((ushort)type | (offsetInBlock & VirtualOffsetMask));
    }
    
    /// <summary>
    /// Gets a value indicating whether the base relocation is zero (used for padding).
    /// </summary>
    public bool IsZero => _value == 0;

    /// <summary>
    /// Gets the type of the base relocation.
    /// </summary>
    public PEBaseRelocationType Type => (PEBaseRelocationType)(_value & TypeMask);

    /// <summary>
    /// Gets the virtual offset of the base relocation relative to the offset of the associated <see cref="PEBaseRelocationBlock"/>.
    /// </summary>
    public ushort OffsetInBlock => (ushort)(_value & VirtualOffsetMask);

    public override string ToString() => Type == PEBaseRelocationType.Absolute ? $"{Type} Zero Padding" : $"{Type} OffsetInBlock = 0x{OffsetInBlock:X}";
}
