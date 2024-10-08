﻿// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.PE;

/// <summary>
/// Represents a 32-bit virtual address.
/// </summary>
public record struct VA32(uint Value)
{
    /// <summary>
    /// Implicitly converts a <see cref="VA32"/> to a <see cref="uint"/>.
    /// </summary>
    /// <param name="value">The <see cref="VA32"/> value to convert.</param>
    /// <returns>The converted <see cref="uint"/> value.</returns>
    public static implicit operator uint(VA32 value) => value.Value;

    /// <summary>
    /// Implicitly converts a <see cref="uint"/> to a <see cref="VA32"/>.
    /// </summary>
    /// <param name="value">The <see cref="uint"/> value to convert.</param>
    /// <returns>The converted <see cref="VA32"/> value.</returns>
    public static implicit operator VA32(uint value) => new(value);

    /// <summary>
    /// Gets a value indicating whether the <see cref="VA32"/> is null (value is 0).
    /// </summary>
    public bool IsNull => Value == 0;

    /// <summary>
    /// Returns a string representation of the <see cref="VA32"/> value.
    /// </summary>
    /// <returns>A string representation of the <see cref="VA32"/> value.</returns>
    public override string ToString() => $"0x{Value:X}";
}