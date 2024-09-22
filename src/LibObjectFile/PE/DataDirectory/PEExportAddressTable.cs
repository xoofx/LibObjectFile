// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using LibObjectFile.Diagnostics;

namespace LibObjectFile.PE;

public class PEExportAddressTable : PESectionData
{
    public PEExportAddressTable() : base(false)
    {
    }

    public PEExportAddressTable(int count) : base(false)
    {
        CollectionsMarshal.SetCount(Values, count);
    }

    public List<PEExportFunctionEntry> Values { get; } = new();

    public override unsafe void UpdateLayout(PELayoutContext context)
    {
        Size = (ulong)(Values.Count * sizeof(RVA));
    }

    public override unsafe void Read(PEImageReader reader)
    {
        reader.Position = Position;
        var buffer = ArrayPool<byte>.Shared.Rent(sizeof(RVA) * Values.Count);
        var span = buffer.AsSpan(0, sizeof(RVA) * Values.Count);
        var spanRva = MemoryMarshal.Cast<byte, RVA>(span);
        try
        {
            int read = reader.Read(buffer.AsSpan(0, sizeof(RVA) * Values.Count));
            if (read != sizeof(RVA) * Values.Count)
            {
                reader.Diagnostics.Error(DiagnosticId.CMN_ERR_UnexpectedEndOfFile, $"Unable to read Export Address Table");
                return;
            }
            
            for (int i = 0; i < Values.Count; i++)
            {
                var rva = spanRva[i];
                if (!reader.File.TryFindContainerByRVA(rva, out var container))
                {
                    reader.Diagnostics.Error(DiagnosticId.PE_ERR_ExportAddressTableInvalidRVA, $"Unable to find the section data for RVA {rva}");
                    return;
                }

                ObjectElement? parent = container;
                while (parent != null && parent is not PESection)
                {
                    parent = parent.Parent;
                }

                Debug.Assert(parent is not null);
                var section = (PESection)parent!;

                if (section.Name == PESectionName.EData)
                {
                    var streamSectionData = container as PEStreamSectionData;
                    if (streamSectionData is null)
                    {
                        reader.Diagnostics.Error(DiagnosticId.PE_ERR_ExportAddressTableInvalidRVA, $"Invalid forwarder RVA {rva} for Export Address Table");
                        return;
                    }

                    Values[i] = new PEExportFunctionEntry(new PEAsciiStringLink(streamSectionData, rva - streamSectionData.RVA));

                }
                else
                {
                    Values[i] = new PEExportFunctionEntry(new PEFunctionAddressLink(container, rva - container.RVA));
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public override void Write(PEImageWriter writer)
    {
        throw new NotImplementedException();
    }
}