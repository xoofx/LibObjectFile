// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using LibObjectFile.Diagnostics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace LibObjectFile.PE;

/// <summary>
/// A block of base relocations for a page.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class PEBaseRelocationBlock
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PEBaseRelocationBlock"/> class.
    /// </summary>
    /// <param name="sectionLink">The section link.</param>
    public PEBaseRelocationBlock(PESectionLink sectionLink)
    {
        SectionLink = sectionLink;
    }

    /// <summary>
    /// Gets or sets the linked <see cref="PESection"/> and its virtual offset within it.
    /// </summary>
    public PESectionLink SectionLink { get; set; }
    
    /// <summary>
    /// Gets the list of relocations for this block.
    /// </summary>
    public List<PEBaseRelocationPageBlockPart> Parts { get; } = new();

    /// <summary>
    /// Internal buffer used to read the block before transforming it into parts.
    /// </summary>
    internal Memory<byte> BlockBuffer { get; set; }

    /// <summary>
    /// Gets the size of this block.
    /// </summary>
    internal uint CalculateSizeOf()
    {
        // If we have a block buffer (when reading an image), use it directly until it is transformed into parts
        if (!BlockBuffer.IsEmpty)
        {
            return (uint)BlockBuffer.Length;
        }

        uint size = 0;
        foreach (var part in Parts)
        {
            size += part.SizeOf;
        }

        return size;
    }

    internal void ReadAndBind(PEImageReader reader)
    {
        var buffer = BlockBuffer;

        var relocSpan = MemoryMarshal.Cast<byte, PEBaseRelocation>(buffer.Span);

        // Remove padding zeros at the end of the block
        if (relocSpan.Length > 0 && relocSpan[^1].IsZero)
        {
            relocSpan = relocSpan.Slice(0, relocSpan.Length - 1);
        }

        PEBaseRelocationPageBlockPart? currentBlockPart = null;
        var blockBaseAddress = SectionLink.RVA();

        var peFile = reader.File;

        // Iterate on all relocations
        foreach (var relocation in relocSpan)
        {
            if (relocation.IsZero)
            {
                continue;
            }

            var va = blockBaseAddress + relocation.OffsetInBlockPart;

            // Find the section data containing the virtual address
            if (!peFile.TryFindVirtualContainer(va, out var vObj))
            {
                reader.Diagnostics.Error(DiagnosticId.PE_ERR_BaseRelocationDirectoryInvalidVirtualAddress, $"Unable to find the section data containing the virtual address 0x{va:X4}");
                continue;
            }

            var sectionData = vObj as PESectionData;
            if (sectionData is null)
            {
                reader.Diagnostics.Error(DiagnosticId.PE_ERR_BaseRelocationDirectoryInvalidVirtualAddress, $"The section data containing the virtual address 0x{va:X4} is not a section data");
                continue;
            }

            var offsetInSectionData = va - sectionData.VirtualAddress;

            // Create a new block part if the section data is different, or it is the first relocation
            if (currentBlockPart is null || currentBlockPart.SectionDataLink.SectionData != sectionData)
            {
                currentBlockPart = new PEBaseRelocationPageBlockPart(new(sectionData, offsetInSectionData));
                Parts.Add(currentBlockPart);
            }

            var newRelocation = new PEBaseRelocation(relocation.Type, (ushort)(offsetInSectionData - currentBlockPart.SectionDataLink.Offset));
            currentBlockPart.Relocations.Add(newRelocation);
        }

        // Clear the buffer, as we don't need it anymore
        BlockBuffer = Memory<byte>.Empty;
    }

    public override string ToString()
    {
        return $"{nameof(PEBaseRelocationBlock)}, Section = {SectionLink.Section?.Name}, RVA = {SectionLink.RVA()}, Size = {CalculateSizeOf()}, Parts[{Parts.Count}]";
    }
}