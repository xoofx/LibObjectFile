// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using LibObjectFile.Diagnostics;

namespace LibObjectFile.PE;

public sealed class PEExportOrdinalTable : PESectionData
{
    public PEExportOrdinalTable()
    {
    }
    
    internal PEExportOrdinalTable(int count)
    {
        CollectionsMarshal.SetCount(Values, count);
    }

    public override bool HasChildren => false;

    public List<ushort> Values { get; } = new();

    
    protected override void UpdateLayoutCore(PELayoutContext context)
    {
        Size = (ulong)Values.Count * sizeof(ushort);
    }

    // Special method that can read from a known size
    public override void Read(PEImageReader reader)
    {
        reader.Position = Position;
        var span = CollectionsMarshal.AsSpan(Values);

        int read = reader.Read(MemoryMarshal.AsBytes(span));
        if (read != Values.Count * sizeof(ushort))
        {
            reader.Diagnostics.Error(DiagnosticId.CMN_ERR_UnexpectedEndOfFile, $"Unable to read Export Ordinal Table");
            return;
        }
    }

    public override void Write(PEImageWriter writer)
    {
        var span = CollectionsMarshal.AsSpan(Values);
        writer.Write(MemoryMarshal.AsBytes(span));
    }
}