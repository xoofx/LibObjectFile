// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.PE;

/// <summary>
/// Represents a 64-bit virtual address.
/// </summary>
public record struct VA64(ulong Value)
{
    /// <summary>
    /// Implicitly converts a <see cref="VA64"/> to a <see cref="ulong"/>.
    /// </summary>
    /// <param name="value">The <see cref="VA64"/> value to convert.</param>
    /// <returns>The converted <see cref="ulong"/> value.</returns>
    public static implicit operator ulong(VA64 value) => value.Value;

    /// <summary>
    /// Implicitly converts a <see cref="ulong"/> to a <see cref="VA64"/>.
    /// </summary>
    /// <param name="value">The <see cref="ulong"/> value to convert.</param>
    /// <returns>The converted <see cref="VA64"/> value.</returns>
    public static implicit operator VA64(ulong value) => new(value);

    /// <summary>
    /// Gets a value indicating whether the <see cref="VA64"/> is null (value is 0).
    /// </summary>
    public bool IsNull => Value == 0;

    /// <summary>
    /// Returns a string representation of the <see cref="VA64"/> value.
    /// </summary>
    /// <returns>A string representation of the <see cref="VA64"/> value.</returns>
    public override string ToString() => $"0x{Value:X}";
}