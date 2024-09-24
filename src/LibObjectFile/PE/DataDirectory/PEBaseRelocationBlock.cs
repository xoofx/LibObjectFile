// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using LibObjectFile.Diagnostics;
using LibObjectFile.PE.Internal;
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
        ArgumentNullException.ThrowIfNull(sectionLink.Container, nameof(sectionLink));
        SectionLink = sectionLink;
    }

    /// <summary>
    /// Gets or sets the linked <see cref="PESection"/> and its virtual offset within it.
    /// </summary>
    public PESectionLink SectionLink { get; set; }
    
    /// <summary>
    /// Gets the list of relocations for this block.
    /// </summary>
    public List<PEBaseRelocation> Relocations { get; } = new();

    /// <summary>
    /// Internal buffer used to read the block before transforming it into parts.
    /// </summary>
    internal Memory<byte> BlockBuffer { get; set; }

    /// <summary>
    /// Gets the size of this block.
    /// </summary>
    internal unsafe uint CalculateSizeOf()
    {
        // If we have a block buffer (when reading an image), use it directly until it is transformed into parts
        if (!BlockBuffer.IsEmpty)
        {
            return (uint)BlockBuffer.Length;
        }

        return (uint)(Relocations.Count * sizeof(ushort));
    }

    internal void ReadAndBind(PEImageReader reader)
    {
        var buffer = BlockBuffer;

        var relocSpan = MemoryMarshal.Cast<byte, RawImageBaseRelocation>(buffer.Span);

        // Remove padding zeros at the end of the block
        if (relocSpan.Length > 0 && relocSpan[^1].IsZero)
        {
            relocSpan = relocSpan.Slice(0, relocSpan.Length - 1);
        }

        var section = SectionLink.Container!;
        var blockBaseAddress = SectionLink.RVA();

        // Iterate on all relocations
        foreach (var relocation in relocSpan)
        {
            if (relocation.IsZero)
            {
                continue;
            }

            var va = blockBaseAddress + relocation.OffsetInBlockPart;

            // Find the section data containing the virtual address
            if (!section.TryFindSectionData(va, out var sectionData))
            {
                reader.Diagnostics.Error(DiagnosticId.PE_ERR_BaseRelocationDirectoryInvalidVirtualAddress, $"Unable to find the section data containing the virtual address 0x{va:X4}");
                continue;
            }

            var offsetInSectionData = va - sectionData.RVA;
            var newRelocation = new PEBaseRelocation(relocation.Type, sectionData, offsetInSectionData);
            Relocations.Add(newRelocation);
        }

        // Clear the buffer, as we don't need it anymore
        BlockBuffer = Memory<byte>.Empty;
    }

    public override string ToString()
    {
        return $"{nameof(PEBaseRelocationBlock)}, Section = {SectionLink.Container?.Name}, RVA = {SectionLink.RVA()}, Size = {CalculateSizeOf()}, Relocations[{Relocations.Count}]";
    }
}