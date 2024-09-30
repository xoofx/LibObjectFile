// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using LibObjectFile.Diagnostics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace LibObjectFile.PE;

/// <summary>
/// A block of base relocations for a page.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class PEBaseRelocationBlock : PESectionData
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PEBaseRelocationBlock"/> class.
    /// </summary>
    public PEBaseRelocationBlock()
    {
    }
    
    /// <inheritdoc />
    public override bool HasChildren => false;

    /// <summary>
    /// Gets or sets the linked <see cref="PESection"/> and its virtual offset within it.
    /// </summary>
    public PESectionLink SectionLink { get; set; }
    
    /// <summary>
    /// Gets the list of relocations for this block.
    /// </summary>
    public List<PEBaseRelocation> Relocations { get; } = new();

    protected override unsafe void UpdateLayoutCore(PELayoutContext layoutContext)
    {
        var count = Relocations.Count;

        // If we have an odd number of relocations, we need to add an extra 0x0
        if (count > 0 && (count & 1) != 0)
        {
            count++;
        }

        Size = (uint)(sizeof(ImageBaseRelocation) + count * sizeof(PEBaseRelocation));
    }

    /// <summary>
    /// Gets the RVA of a relocation in this block.
    /// </summary>
    /// <param name="relocation">The relocation.</param>
    /// <returns>The RVA of the relocation.</returns>
    public RVA GetRVA(PEBaseRelocation relocation) => SectionLink.RVA() + relocation.OffsetInBlock;

    /// <summary>
    /// Reads the address from the section data.
    /// </summary>
    /// <param name="file">The PE file.</param>
    /// <returns>The address read from the section data.</returns>
    /// <exception cref="InvalidOperationException">The section data link is not set or the type is not supported.</exception>
    public ulong ReadAddress(PEFile file, PEBaseRelocation relocation)
    {
        if (relocation.Type != PEBaseRelocationType.Dir64)
        {
            throw new InvalidOperationException($"The base relocation type {relocation.Type} not supported. Only Dir64 is supported for this method.");
        }

        var vaOfReloc = SectionLink.RVA() + relocation.OffsetInBlock;

        if (!file.TryFindContainerByRVA(vaOfReloc, out var container))
        {
            throw new InvalidOperationException($"Unable to find the section data containing the virtual address {vaOfReloc}");
        }

        var rvo = vaOfReloc - container!.RVA;

        if (file.IsPE32)
        {
            VA32 va32 = default;
            var span = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref va32, 1));

            int read = container!.ReadAt(rvo, span);
            if (read != 4)
            {
                throw new InvalidOperationException($"Unable to read the VA32 from the section data type: {container.GetType().FullName}");
            }

            return va32;
        }
        else
        {
            VA64 va64 = default;
            var span = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref va64, 1));

            int read = container!.ReadAt(rvo, span);
            if (read != 8)
            {
                throw new InvalidOperationException($"Unable to read the VA64 from the section data type: {container.GetType().FullName}");
            }

            return va64;
        }
    }
    
    public override unsafe void Read(PEImageReader reader)
    {
        reader.Position = Position;

        var position = reader.Position;
        if (!reader.TryReadData(sizeof(ImageBaseRelocation), out ImageBaseRelocation block))
        {
            reader.Diagnostics.Error(DiagnosticId.PE_ERR_BaseRelocationDirectoryInvalidEndOfStream, $"Unable to read BaseRelocation Block. Expected {sizeof(ImageBaseRelocation)} bytes at position {position}");
            return;
        }

        if (!reader.File.TryFindSection(block.PageRVA, out var section))
        {
            reader.Diagnostics.Error(DiagnosticId.PE_ERR_BaseRelocationDirectoryInvalidVirtualAddress, $"Unable to find the section containing the virtual address {block.PageRVA} at position {position}");
            return;
        }


        SectionLink = new PESectionLink(section, (uint)(block.PageRVA - section.RVA));

        var sizeOfRelocations = block.SizeOfBlock - sizeof(ImageBaseRelocation);
        
        var (relocationCount, remainder) = Math.DivRem(sizeOfRelocations, sizeof(PEBaseRelocation));
        if (remainder != 0)
        {
            reader.Diagnostics.Error(DiagnosticId.PE_ERR_BaseRelocationDirectoryInvalidSizeOfBlock, $"Invalid size of relocations {sizeOfRelocations} not a multiple of {sizeof(PEBaseRelocation)} bytes at position {position}");
            return;
        }
        
        // Read all relocations straight to memory
        CollectionsMarshal.SetCount(Relocations, (int)relocationCount);
        var span = CollectionsMarshal.AsSpan(Relocations);
        var spanBytes = MemoryMarshal.AsBytes(span);

        var read = reader.Read(spanBytes);
        if (read != spanBytes.Length)
        {
            reader.Diagnostics.Error(DiagnosticId.PE_ERR_BaseRelocationDirectoryInvalidEndOfStream, $"Unable to read the full content of the BaseRelocation directory. Expected {relocationCount} bytes, but read {read} bytes at position {position}");
            return;
        }

        Size = (uint)(sizeof(ImageBaseRelocation) + Relocations.Count * sizeof(PEBaseRelocation));
        Debug.Assert(Size == block.SizeOfBlock);
    }

    public override unsafe void Write(PEImageWriter writer)
    {
        var block = new ImageBaseRelocation
        {
            PageRVA = SectionLink.RVA(),
            SizeOfBlock = (uint)Size
        };

        writer.Write(block);

        var span = CollectionsMarshal.AsSpan(Relocations);
        var spanBytes = MemoryMarshal.AsBytes(span);
        writer.Write(spanBytes);

        if ((Relocations.Count & 1) != 0)
        {
            writer.WriteZero((int)sizeof(PEBaseRelocation));
        }
    }

    protected override bool PrintMembers(StringBuilder builder)
    {
        if (base.PrintMembers(builder))
        {
            builder.Append(", ");
        }

        builder.Append($"Section = {SectionLink.Container?.Name}, Block RVA = {SectionLink.RVA()}, Relocations[{Relocations.Count}]");
        return true;
    }

    protected override void ValidateParent(ObjectElement parent)
    {
        if (parent is not PEBaseRelocationDirectory)
        {
            throw new InvalidOperationException($"Invalid parent type {parent.GetType().FullName}. Expected {typeof(PEBaseRelocationDirectory).FullName}");
        }
    }

    private struct ImageBaseRelocation
    {
#pragma warning disable CS0649
        public RVA PageRVA;
        public uint SizeOfBlock;
    }
}