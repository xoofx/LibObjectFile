// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace LibObjectFile.PE;

#pragma warning disable CS0649
/// <summary>
/// A base relocation in a Portable Executable (PE) image.
/// </summary>
public readonly struct PEBaseRelocation : IEquatable<PEBaseRelocation>
{
    public const ushort MaxVirtualOffset = (1 << 12) - 1;
    private const ushort TypeMask = unchecked((ushort)(~MaxVirtualOffset));
    private const ushort VirtualOffsetMask = MaxVirtualOffset;

    private readonly ushort _value;
    
    public PEBaseRelocation(BaseRelocationType type, ushort virtualOffset)
    {
        if (virtualOffset > MaxVirtualOffset)
        {
            ThrowVirtualOffsetOutOfRange();
        }
        _value = (ushort)((ushort)type | virtualOffset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal PEBaseRelocation(ushort value) => _value = value;

    /// <summary>
    /// Gets a value indicating whether the base relocation is zero (used for padding).
    /// </summary>
    public bool IsZero => _value == 0;

    /// <summary>
    /// Gets the type of the base relocation.
    /// </summary>
    public BaseRelocationType Type => (BaseRelocationType)(_value & TypeMask);

    /// <summary>
    /// Gets the virtual offset of the base relocation relative to the offset of the associated <see cref="PEBaseRelocationPageBlock"/>.
    /// </summary>
    public ushort OffsetInBlockPart => (ushort)(_value & VirtualOffsetMask);

    /// <inheritdoc />
    public bool Equals(PEBaseRelocation other) => _value == other._value;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is PEBaseRelocation other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => _value.GetHashCode();

    /// <summary>
    /// Compares two <see cref="PEBaseRelocation"/> objects for equality.
    /// </summary>
    /// <param name="left">The left value to compare.</param>
    /// <param name="right">The right value to compare.</param>
    /// <returns><c>true</c> if values are equal; otherwise <c>false</c>.</returns>
    public static bool operator ==(PEBaseRelocation left, PEBaseRelocation right) => left.Equals(right);

    /// <summary>
    /// Compares two <see cref="PEBaseRelocation"/> objects for inequality.
    /// </summary>
    /// <param name="left">The left value to compare.</param>
    /// <param name="right">The right value to compare.</param>
    /// <returns><c>true</c> if values are not equal; otherwise <c>false</c>.</returns>
    public static bool operator !=(PEBaseRelocation left, PEBaseRelocation right) => !left.Equals(right);

    /// <inheritdoc />
    public override string ToString()
    {
        return IsZero ? "Zero Padding" : $"{Type} {OffsetInBlockPart}";
    }

    [DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowVirtualOffsetOutOfRange()
    {
        throw new ArgumentOutOfRangeException(nameof(OffsetInBlockPart), $"The virtual offset must be less than {MaxVirtualOffset}");
    }
}