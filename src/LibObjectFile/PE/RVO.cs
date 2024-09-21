// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.PE;

/// <summary>
/// Defines a Relative Virtual Offset (RVO) that is relative to a <see cref="PEObject"/> in a Portable Executable (PE) image.
/// </summary>
/// <param name="Value">The value of the relative offset.</param>
public record struct RVO(uint Value)
{
    /// <summary>
    /// Converts a <see cref="RVO"/> to a <see cref="uint"/>.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    public static implicit operator uint(RVO value) => value.Value;

    /// <summary>
    /// Converts a <see cref="uint"/> to a <see cref="RVO"/>.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    public static implicit operator RVO(uint value) => new(value);

    /// <inheritdoc />
    public override string ToString() => $"0x{Value:X}";
}