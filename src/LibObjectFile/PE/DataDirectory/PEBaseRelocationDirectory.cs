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

public sealed class PEBaseRelocationDirectory : PEDirectory
{
    public PEBaseRelocationDirectory() : base(ImageDataDirectoryKind.BaseRelocation, false)
    {
    }
    
    public List<PEBaseRelocationPageBlock> Blocks { get; } = new();

    public override void UpdateLayout(PEVisitorContext context)
    {
        var size = 0UL;
        foreach (var block in Blocks)
        {
            size += block.CalculateSizeOf();
        }
        Size = size;
    }

    public override unsafe void Read(PEImageReader reader)
    {
        reader.Position = Position;
        var size = (int)Size;

        var buffer = ArrayPool<byte>.Shared.Rent((int)size);
        try
        {
            var span = buffer.AsSpan(0, (int)size);
            int read = reader.Read(span);
            if (read != size)
            {
                reader.Diagnostics.Error(DiagnosticId.PE_ERR_BaseRelocationDirectoryInvalidEndOfStream, $"Unable to read the full content of the BaseRelocation directory. Expected {size} bytes, but read {read} bytes");
                return;
            }

            var allSectionData = reader.File.GetAllSectionData();

            int blockIndex = 0;
            while (span.Length > 0)
            {
                var location = MemoryMarshal.Read<ImageBaseRelocation>(span);
                span = span.Slice(sizeof(ImageBaseRelocation));

                if (!reader.File.TryFindSection(location.PageVirtualAddress, out var section))
                {
                    reader.Diagnostics.Error(DiagnosticId.PE_ERR_BaseRelocationDirectoryInvalidVirtualAddress, $"Unable to find the section containing the virtual address {location.PageVirtualAddress} in block #{blockIndex}");
                    continue;
                }

                var sizeOfRelocations = (int)location.SizeOfBlock - sizeof(ImageBaseRelocation);

                var relocSpan = MemoryMarshal.Cast<byte, PEBaseRelocation>(span.Slice(0, sizeOfRelocations));

                // Remove padding zeros at the end of the block
                if (relocSpan.Length > 0 && relocSpan[^1].IsZero)
                {
                    relocSpan = relocSpan.Slice(0, relocSpan.Length - 1);
                }
                
                // Create a block
                var block = new PEBaseRelocationPageBlock(new(section, location.PageVirtualAddress - section.VirtualAddress));
                Blocks.Add(block);

                PEBaseRelocationPageBlockPart? currentBlockPart = null;
                var currentSectionDataIndex = 0;

                // Iterate on all relocations
                foreach (var relocation in relocSpan)
                {
                    if (relocation.IsZero)
                    {
                        continue;
                    }

                    var va = location.PageVirtualAddress + relocation.OffsetInBlockPart;

                    // Find the section data containing the virtual address
                    if (!TryFindSectionData(allSectionData, currentSectionDataIndex, va, out var newSectionDataIndex))
                    {
                        reader.Diagnostics.Error(DiagnosticId.PE_ERR_BaseRelocationDirectoryInvalidVirtualAddress, $"Unable to find the section data containing the virtual address 0x{va:X4}");
                        continue;
                    }
                    
                    var sectionData = allSectionData[newSectionDataIndex];
                    var offsetInSectionData = va - sectionData.VirtualAddress;
                    currentSectionDataIndex = newSectionDataIndex;

                    // Create a new block part if the section data is different, or it is the first relocation
                    if (currentBlockPart is null || currentBlockPart.SectionDataRVALink.Element != sectionData)
                    {
                        currentBlockPart = new PEBaseRelocationPageBlockPart(new(sectionData, offsetInSectionData));
                        block.Parts.Add(currentBlockPart);
                    }

                    var newRelocation = new PEBaseRelocation(relocation.Type, (ushort)(offsetInSectionData - currentBlockPart.SectionDataRVALink.OffsetInElement));

                    currentBlockPart.Relocations.Add(newRelocation);
                }

                // Move to the next block
                span = span.Slice(sizeOfRelocations);
                blockIndex++;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private bool TryFindSectionData(List<PESectionData> allSectionData, int startIndex, uint virtualAddress, out int indexFound)
    {
        var span = CollectionsMarshal.AsSpan(allSectionData);
        for (int i = startIndex; i < span.Length; i++)
        {
            ref var sectionData = ref span[i];
            if (sectionData.ContainsVirtual(virtualAddress))
            {
                indexFound = i;
                return true;
            }
        }

        indexFound = -1;
        return false;
    }

    public override void Write(PEImageWriter writer)
    {
        throw new NotImplementedException();
    }

#pragma warning disable CS0649
    private struct ImageBaseRelocation
    {
        public RVA PageVirtualAddress;
        public uint SizeOfBlock;
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
}