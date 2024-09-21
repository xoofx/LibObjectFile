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

    public override unsafe void UpdateLayout(PEVisitorContext context)
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
                if (!reader.File.TryFindVirtualContainer(rva, out var functionContainer))
                {
                    reader.Diagnostics.Error(DiagnosticId.PE_ERR_ExportAddressTableInvalidRVA, $"Unable to find the section data for RVA {rva}");
                    return;
                }

                if (functionContainer is PEStreamSectionData streamSectionData)
                {
                    var parent = functionContainer.Parent;
                    while (parent != null && !(parent is PESection))
                    {
                        parent = parent.Parent;
                    }

                    Debug.Assert(parent != null);
                    var section = (PESection)parent!;
                    if (section.Name == PESectionName.EData)
                    {
                        Values[i] = new PEExportFunctionEntry(new PEAsciiStringLink(streamSectionData, rva - streamSectionData.VirtualAddress));

                    }
                    else
                    {
                        Values[i] = new PEExportFunctionEntry(new PEFunctionAddressLink(streamSectionData, rva - streamSectionData.VirtualAddress));
                    }
                }
                else
                {
                    reader.Diagnostics.Error(DiagnosticId.PE_ERR_ExportAddressTableInvalidRVA, $"Invalid RVA {rva} for Export Address Table");
                    return;
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