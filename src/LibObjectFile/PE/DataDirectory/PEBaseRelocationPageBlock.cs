// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;

namespace LibObjectFile.PE;

/// <summary>
/// A block of base relocations for a page.
/// </summary>
[DebuggerDisplay("{nameof(PEBaseRelocationPageBlock)}, Section = {SectionRVALink.Element.Name}, RVA = {SectionRVALink.VirtualAddress}, Size = {SizeOf}, Parts[{Parts.Count}]")]
public sealed class PEBaseRelocationPageBlock
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PEBaseRelocationPageBlock"/> class.
    /// </summary>
    /// <param name="sectionRVALink">The section link.</param>
    public PEBaseRelocationPageBlock(RVALink<PESection> sectionRVALink)
    {
        SectionRVALink = sectionRVALink;
    }

    /// <summary>
    /// Gets or sets the linked <see cref="PESection"/> and its virtual offset within it.
    /// </summary>
    public RVALink<PESection> SectionRVALink { get; set; }
    
    /// <summary>
    /// Gets the list of relocations for this block.
    /// </summary>
    public List<PEBaseRelocationPageBlockPart> Parts { get; } = new();

    /// <summary>
    /// Gets the size of this block.
    /// </summary>
    internal uint CalculateSizeOf()
    {
        uint size = 0;
        foreach (var part in Parts)
        {
            size += part.SizeOf;
        }

        return size;
    }

    public override string ToString()
    {
        return $"{nameof(PEBaseRelocationPageBlock)}, Section = {SectionRVALink.Element?.Name}, RVA = {SectionRVALink.VirtualAddress}, Size = {CalculateSizeOf()}, Parts[{Parts.Count}]";
    }
}