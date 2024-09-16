// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Numerics;
using System.Text;

namespace LibObjectFile.PE;

/// <summary>
/// A section name in a Portable Executable (PE) image.
/// </summary>
public readonly record struct PESectionName
{
    /// <summary>
    /// Internal constructor used to bypass the validation of the section name.
    /// </summary>
    /// <param name="name">The name of the section.</param>
    /// <param name="validate">Validates the name.</param>
    internal PESectionName(string name, bool validate = true)
    {
        if (validate)
        {
            Validate(name);
        }
        Name = name;
    }

    /// <summary>
    /// Gets the name of the section.
    /// </summary>
    public string Name { get; }

    /// <inheritdoc />
    public bool Equals(PESectionName other) => Name.Equals(other.Name, StringComparison.Ordinal);

    /// <inheritdoc />
    public override int GetHashCode() => Name.GetHashCode();

    /// <inheritdoc />
    public override string ToString() => Name;

    /// <summary>
    /// Checks if the specified section name is a valid section name.
    /// </summary>
    /// <param name="name"></param>
    /// <exception cref="ArgumentException"></exception>
    /// <remarks>>
    /// A section name is valid if it contains only printable ASCII characters (0x20 to 0x7E) and has a maximum length of 8 characters.
    /// </remarks>
    public static void Validate(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        Span<byte> buffer = stackalloc byte[Encoding.ASCII.GetMaxByteCount(name.Length)];
        int total = Encoding.ASCII.GetBytes(name, buffer);
        if (total > 8)
        {
            throw new ArgumentException("Section name is too long (max 8 characters)", nameof(name));
        }

        for (int i = 0; i < total; i++)
        {
            if (buffer[i] < 0x20 || buffer[i] > 0x7E)
            {
                throw new ArgumentException("Section name contains invalid characters", nameof(name));
            }
        }
    }

    /// <summary>
    /// Converts a string to a <see cref="PESectionName"/>.
    /// </summary>
    /// <param name="name">The section name.</param>
    public static implicit operator PESectionName(string name) => new(name);

    /// <summary>
    /// Converts a <see cref="PESectionName"/> to a string.
    /// </summary>
    /// <param name="name">The section name.</param>
    public static implicit operator string(PESectionName name) => name.Name;
}