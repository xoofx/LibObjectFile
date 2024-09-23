// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

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
    /// Gets the RVA of a relocation that belongs to this part.
    /// </summary>
    /// <param name="relocation">The relocation.</param>
    /// <returns>The RVA of the relocation.</returns>
    public RVA GetRVA(PEBaseRelocation relocation)
        => SectionDataLink.Container is null ? 0 : SectionDataLink.Container.RVA + SectionDataLink.RVO + relocation.OffsetInBlockPart;

    /// <summary>
    /// Read the address from the relocation.
    /// </summary>
    /// <param name="relocation">A relocation that belongs to this block.</param>
    /// <returns>The address read from the relocation.</returns>
    /// <exception cref="InvalidOperationException">If the section data or the PE file is not found.</exception>
    public ulong ReadAddress(PEBaseRelocation relocation)
    {
        var sectionData = SectionDataLink.SectionData;
        if (sectionData is null)
        {
            throw new InvalidOperationException("Cannot read address from a relocation without a section data");
        }

        var peFile = sectionData.GetPEFile();
        if (peFile is null)
        {
            throw new InvalidOperationException("Cannot read address from a relocation without a PE file");
        }

        return ReadAddress(peFile, relocation);
    }

    /// <summary>
    /// Read the address from the relocation.
    /// </summary>
    /// <param name="peFile">The PE file containing the section data.</param>
    /// <param name="relocation">A relocation that belongs to this block.</param>
    /// <returns>The address read from the relocation.</returns>
    /// <exception cref="InvalidOperationException">If the section data or the PE file is not found.</exception>
    public ulong ReadAddress(PEFile peFile, PEBaseRelocation relocation)
    {
        ArgumentNullException.ThrowIfNull(peFile);

        var sectionData = SectionDataLink.SectionData;
        if (sectionData is null)
        {
            throw new InvalidOperationException("Cannot read address from a relocation without a section data");
        }

        var is32 = peFile.IsPE32;

        if (is32)
        {
            uint address = 0;
            Span<byte> buffer = MemoryMarshal.Cast<uint, byte>(MemoryMarshal.CreateSpan(ref address, 1));
            sectionData.ReadAt(SectionDataLink.RVO + relocation.OffsetInBlockPart, buffer);
            return address;
        }
        else
        {
            ulong address = 0;
            Span<byte> buffer = MemoryMarshal.Cast<ulong, byte>(MemoryMarshal.CreateSpan(ref address, 1));
            sectionData.ReadAt(SectionDataLink.RVO + relocation.OffsetInBlockPart, buffer);
            return address;
        }
    }

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