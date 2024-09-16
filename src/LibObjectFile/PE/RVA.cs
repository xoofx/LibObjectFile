// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.PE;

/// <summary>
/// Defines a Relative Virtual Address (RVA) in a Portable Executable (PE) image.
/// </summary>
/// <param name="Value">The value of the address</param>
public record struct RVA(uint Value)
{
    /// <summary>
    /// Converts a <see cref="RVA"/> to a <see cref="uint"/>.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    public static implicit operator uint(RVA value) => value.Value;

    /// <summary>
    /// Converts a <see cref="uint"/> to a <see cref="RVA"/>.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    public static implicit operator RVA(uint value) => new(value);

    /// <inheritdoc />
    public override string ToString() => $"{Value:X4}";
}