// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace LibObjectFile.PE.Internal;

#pragma warning disable CS0649

/// <summary>
/// An array of <see cref="RawImageDataDirectory"/> entries.
/// </summary>
[InlineArray(16)]
internal struct RawImageDataDirectoryArray
{
    private RawImageDataDirectory _e;

    /// <summary>
    /// Gets or sets the <see cref="RawImageDataDirectory"/> at the specified index.
    /// </summary>
    /// <param name="kind">The index of the <see cref="RawImageDataDirectory"/> to get or set.</param>
    /// <returns>The <see cref="RawImageDataDirectory"/> at the specified index.</returns>
    [UnscopedRef]
    public ref RawImageDataDirectory this[PEDataDirectoryKind kind] => ref this[(int)kind];
}