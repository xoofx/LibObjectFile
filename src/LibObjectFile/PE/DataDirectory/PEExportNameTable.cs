// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using LibObjectFile.Collections;
using LibObjectFile.Diagnostics;

namespace LibObjectFile.PE;

public sealed class PEExportNameTable : PESectionData
{
    public PEExportNameTable()
    {
    }

    public PEExportNameTable(int count)
    {
        CollectionsMarshal.SetCount(Values, count);
    }

    public override bool HasChildren => false;

    public List<PEAsciiStringLink> Values { get; } = new();

    protected override unsafe void UpdateLayoutCore(PELayoutContext context)
    {
        Size = (ulong)(Values.Count * sizeof(RVA));
    }

    public override unsafe void Read(PEImageReader reader)
    {
        reader.Position = Position;
        using var tempSpan = TempSpan<RVA>.Create(Values.Count, out var spanRva);
        var span = tempSpan.AsBytes;

        int read = reader.Read(span);
        if (read != span.Length)
        {
            reader.Diagnostics.Error(DiagnosticId.CMN_ERR_UnexpectedEndOfFile, $"Unable to read Export Name Table");
            return;
        }

        for (int i = 0; i < Values.Count; i++)
        {
            var rva = spanRva[i];
            if (!reader.File.TryFindByRVA(rva, out var sectionData))
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

            Values[i] = new PEAsciiStringLink(streamSectionData, rva - streamSectionData.RVA);
        }
    }

    public override void Write(PEImageWriter writer)
    {
        using var tempSpan = TempSpan<RVA>.Create(Values.Count, out var spanRva);

        for (int i = 0; i < Values.Count; i++)
        {
            var value = Values[i];
            spanRva[i] = value.RVA();
        }

        writer.Write(tempSpan.AsBytes);
    }
}