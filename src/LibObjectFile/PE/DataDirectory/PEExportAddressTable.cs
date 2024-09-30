// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using LibObjectFile.Collections;
using LibObjectFile.Diagnostics;

namespace LibObjectFile.PE;

public sealed class PEExportAddressTable : PESectionData
{
    public PEExportAddressTable()
    {
    }

    public PEExportAddressTable(int count)
    {
        CollectionsMarshal.SetCount(Values, count);
    }

    public override bool HasChildren => false;

    public List<PEExportFunctionEntry> Values { get; } = new();

    protected override unsafe void UpdateLayoutCore(PELayoutContext context)
    {
        Size = (ulong)(Values.Count * sizeof(RVA));
    }

    public override unsafe void Read(PEImageReader reader)
    {
        using var tempSpan = TempSpan<RVA>.Create(Values.Count, out var spanRva);
        var span = tempSpan.AsBytes;

        reader.Position = Position;
        int read = reader.Read(span);
        if (read != span.Length)
        {
            reader.Diagnostics.Error(DiagnosticId.CMN_ERR_UnexpectedEndOfFile, $"Unable to read Export Address Table");
            return;
        }

        for (int i = 0; i < Values.Count; i++)
        {
            var rva = spanRva[i];
            if (rva == 0)
            {
                Values[i] = new PEExportFunctionEntry();
                continue;
            }

            if (!reader.File.TryFindSection(rva, out var section))
            {
                reader.Diagnostics.Error(DiagnosticId.PE_ERR_ExportAddressTableInvalidRVA, $"Unable to find the section data for RVA {rva}");
                return;
            }

            var found = section.TryFindSectionDataByRVA(rva, out var sectionData);

            if (section.Name == PESectionName.EData)
            {
                var streamSectionData = sectionData as PEStreamSectionData;
                if (streamSectionData is null)
                {
                    reader.Diagnostics.Error(DiagnosticId.PE_ERR_ExportAddressTableInvalidRVA, $"Invalid forwarder RVA {rva} for Export Address Table");
                    return;
                }

                Values[i] = new PEExportFunctionEntry(new PEAsciiStringLink(streamSectionData, rva - streamSectionData.RVA));
            }
            else
            {
                if (found)
                {
                    Values[i] = new PEExportFunctionEntry(new PEFunctionAddressLink(sectionData, rva - sectionData!.RVA));
                }
                else
                {
                    Values[i] = new PEExportFunctionEntry(new PEFunctionAddressLink(section, rva - section.RVA));
                }
            }
        }
    }

    public override void Write(PEImageWriter writer)
    {
        using var tempSpan = TempSpan<RVA>.Create(Values.Count, out var spanRva);

        for (int i = 0; i < Values.Count; i++)
        {
            var value = Values[i];
            spanRva[i] = value.IsEmpty ? default : value.IsForwarderRVA ? value.ForwarderRVA.RVA() : value.ExportRVA.RVA();
        }

        writer.Write(tempSpan);
    }
}