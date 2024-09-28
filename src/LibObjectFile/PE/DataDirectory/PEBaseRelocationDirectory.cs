// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using LibObjectFile.Diagnostics;

namespace LibObjectFile.PE;

public sealed class PEBaseRelocationDirectory : PEDataDirectory
{
    public PEBaseRelocationDirectory() : base(PEDataDirectoryKind.BaseRelocation)
    {
    }
    
    public List<PEBaseRelocationBlock> Blocks { get; } = new();

    protected override unsafe uint ComputeHeaderSize(PELayoutContext context)
    {
        var size = 0U;
        foreach (var block in Blocks)
        {
            size += (uint)(block.CalculateSizeOf() + sizeof(ImageBaseRelocation));
        }

        return size;
    }
    
    public override unsafe void Read(PEImageReader reader)
    {
        reader.Position = Position;
        var size = (int)Size;

        var array = new byte[size]; // Ideally would be nice to have this coming from ArrayPool with a ref counting
        var buffer = array.AsMemory(0, (int)size);
        int read = reader.Read(buffer.Span);
        if (read != size)
        {
            reader.Diagnostics.Error(DiagnosticId.PE_ERR_BaseRelocationDirectoryInvalidEndOfStream, $"Unable to read the full content of the BaseRelocation directory. Expected {size} bytes, but read {read} bytes");
            return;
        }

        int blockIndex = 0;
        while (buffer.Length > 0)
        {
            var location = MemoryMarshal.Read<ImageBaseRelocation>(buffer.Span);
            buffer = buffer.Slice(sizeof(ImageBaseRelocation));

            if (!reader.File.TryFindSection(location.PageRVA, out var section))
            {
                reader.Diagnostics.Error(DiagnosticId.PE_ERR_BaseRelocationDirectoryInvalidVirtualAddress, $"Unable to find the section containing the virtual address {location.PageRVA} in block #{blockIndex}");
                continue;
            }

            var sizeOfRelocations = (int)location.SizeOfBlock - sizeof(ImageBaseRelocation);
            
            // Create a block
            var block = new PEBaseRelocationBlock(new PESectionLink(section, (uint)(location.PageRVA - section.RVA)))
            {
                BlockBuffer = buffer.Slice(0, sizeOfRelocations)
            };
            Blocks.Add(block);

            // Move to the next block
            buffer = buffer.Slice(sizeOfRelocations);
            blockIndex++;
        }

        // Update the header size
        HeaderSize = ComputeHeaderSize(reader);
    }

    internal override void Bind(PEImageReader reader)
    {
        foreach (var block in Blocks)
        {
            block.ReadAndBind(reader);
        }

        HeaderSize = ComputeHeaderSize(reader);
    }

    public override void Write(PEImageWriter writer)
    {
        ImageBaseRelocation rawBlock = default;
        foreach (var block in Blocks)
        {
            rawBlock.PageRVA = block.SectionLink.RVA();
            rawBlock.SizeOfBlock = block.CalculateSizeOf();
        }
    }

    protected override bool PrintMembers(StringBuilder builder)
    {
        if (base.PrintMembers(builder))
        {
            builder.Append(", ");
        }

        builder.Append($"Blocks[{Blocks.Count}]");
        return true;
    }

    private struct ImageBaseRelocation
    {
#pragma warning disable CS0649
        public RVA PageRVA;
        public uint SizeOfBlock;
    }
}