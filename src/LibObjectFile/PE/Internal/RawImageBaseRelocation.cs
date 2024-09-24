// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.PE.Internal;

/// <summary>
/// Structure representing a base relocation.
/// </summary>
internal struct RawImageBaseRelocation
{
    public const ushort MaxVirtualOffset = (1 << 12) - 1;
    private const ushort TypeMask = unchecked((ushort)~MaxVirtualOffset);
    private const ushort VirtualOffsetMask = MaxVirtualOffset;

    private readonly ushort _value;

    public RawImageBaseRelocation(ushort value)
    {
        _value = value;
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
    public ushort OffsetInBlockPart => (ushort)(_value & VirtualOffsetMask);
}