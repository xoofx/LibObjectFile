﻿// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;

namespace LibObjectFile.PE;

/// <summary>
/// A block of base relocations for a page.
/// </summary>
[DebuggerDisplay("{nameof(PEBaseRelocationPageBlock),nq} Link = {SectionDataLink}, Relocations[{Relocations.Count}]")]
public sealed class PEBaseRelocationPageBlockPart
{
    public PEBaseRelocationPageBlockPart(PESectionDataLink sectionDataLink)
    {
        SectionDataLink = sectionDataLink;
        Relocations = new List<PEBaseRelocation>();
    }

    internal PEBaseRelocationPageBlockPart(PESectionDataLink sectionDataLink, List<PEBaseRelocation> relocations)
    {
        SectionDataLink = sectionDataLink;
        Relocations = relocations;
    }

    /// <summary>
    /// Gets or sets the linked <see cref="PESectionData"/> and its virtual offset within it.
    /// </summary>
    public PESectionDataLink SectionDataLink { get; }

    /// <summary>
    /// Gets the list of relocations for this block.
    /// </summary>
    public List<PEBaseRelocation> Relocations { get; }

    /// <summary>
    /// Gets the size of this block.
    /// </summary>
    internal uint SizeOf
    {
        get
        {
            var size = sizeof(uint) + sizeof(uint) + (Relocations.Count + 1) * sizeof(ushort);
            // Align to 4 bytes
            size = (size + 3) & ~3;
            return (uint)size;
        }
    }

    public override string ToString()
    {
        return $"{nameof(PEBaseRelocationBlock)} Link = {SectionDataLink}, Relocations[{Relocations.Count}]";
    }
}