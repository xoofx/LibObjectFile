// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace LibObjectFile.PE;

#pragma warning disable CS0649

/// <summary>
/// An array of <see cref="ImageDataDirectory"/> entries.
/// </summary>
[InlineArray(16)]
public struct ImageDataDirectoryArray
{
    private ImageDataDirectory _e;

    /// <summary>
    /// Gets or sets the <see cref="ImageDataDirectory"/> at the specified index.
    /// </summary>
    /// <param name="kind">The index of the <see cref="ImageDataDirectory"/> to get or set.</param>
    /// <returns>The <see cref="ImageDataDirectory"/> at the specified index.</returns>
    [UnscopedRef]
    public ref ImageDataDirectory this[PEDataDirectoryKind kind] => ref this[(int)kind];
}