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
public readonly record struct PEBaseRelocation(PEBaseRelocationType Type, PESectionData? Container, RVO RVO) : IPELink<PESectionData>
{
    /// <summary>
    /// Reads the address from the section data.
    /// </summary>
    /// <param name="file">The PE file.</param>
    /// <returns>The address read from the section data.</returns>
    /// <exception cref="InvalidOperationException">The section data link is not set or the type is not supported.</exception>
    public ulong ReadAddress(PEFile file)
    {
        if (Container is null)
        {
            throw new InvalidOperationException("The section data link is not set");
        }

        if (Type != PEBaseRelocationType.Dir64)
        {
            throw new InvalidOperationException($"The base relocation type {Type} not supported. Only Dir64 is supported for this method.");
        }
        
        if (file.IsPE32)
        {
            VA32 va = default;
            var span = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref va, 1));

            int read = Container!.ReadAt(RVO, span);
            if (read != 4)
            {
                throw new InvalidOperationException($"Unable to read the VA32 from the section data type: {Container.GetType().FullName}");
            }

            return va;
        }
        else
        {
            VA64 va = default;
            var span = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref va, 1));

            int read = Container!.ReadAt(RVO, span);
            if (read != 8)
            {
                throw new InvalidOperationException($"Unable to read the VA64 from the section data type: {Container.GetType().FullName}");
            }

            return va;
        }
    }

    public override string ToString() => $"{Type} {this.ToDisplayTextWithRVA()}";
}
