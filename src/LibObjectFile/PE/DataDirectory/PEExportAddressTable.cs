// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Xml.Linq;
using LibObjectFile.Collections;
using LibObjectFile.Diagnostics;
using static System.Collections.Specialized.BitVector32;

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

    public override unsafe void UpdateLayout(PELayoutContext context)
    {
        Size = (ulong)(Values.Count * sizeof(RVA));
    }

    public override unsafe void Read(PEImageReader reader)
    {
        using var pooledSpan = PooledSpan<RVA>.Create(Values.Count, out var spanRva);
        var span = pooledSpan.AsBytes;

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
            if (!reader.File.TryFindSection(rva, out var section))
            {
                reader.Diagnostics.Error(DiagnosticId.PE_ERR_ExportAddressTableInvalidRVA, $"Unable to find the section data for RVA {rva}");
                return;
            }

            var found = section.TryFindSectionData(rva, out var sectionData);

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
        throw new NotImplementedException();
    }
}