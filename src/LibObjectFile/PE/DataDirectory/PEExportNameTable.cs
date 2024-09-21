// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using LibObjectFile.Diagnostics;

namespace LibObjectFile.PE;

public class PEExportNameTable : PESectionData
{
    public PEExportNameTable() : base(false)
    {
    }

    public PEExportNameTable(int count) : base(false)
    {
        CollectionsMarshal.SetCount(Values, count);
    }

    public List<PEAsciiStringLink> Values { get; } = new();

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
                reader.Diagnostics.Error(DiagnosticId.CMN_ERR_UnexpectedEndOfFile, $"Unable to read Export Name Table");
                return;
            }

            for (int i = 0; i < Values.Count; i++)
            {
                var rva = spanRva[i];
                if (!reader.File.TryFindVirtualContainer(rva, out var sectionData))
                {
                    reader.Diagnostics.Error(DiagnosticId.PE_ERR_ExportNameTableInvalidRVA, $"Unable to find the section data for RVA {rva}");
                    return;
                }

                var streamSectionData = sectionData as PEStreamSectionData;
                if (streamSectionData is null)
                {
                    reader.Diagnostics.Error(DiagnosticId.PE_ERR_ExportNameTableInvalidRVA, $"The section data for RVA {rva} is not a stream section data");
                    return;
                }

                Values[i] = new PEAsciiStringLink(streamSectionData, rva - streamSectionData.VirtualAddress);
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