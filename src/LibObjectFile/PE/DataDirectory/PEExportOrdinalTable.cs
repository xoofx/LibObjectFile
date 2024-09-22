// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using LibObjectFile.Diagnostics;

namespace LibObjectFile.PE;

public class PEExportOrdinalTable : PESectionData
{
    public PEExportOrdinalTable() : base(false)
    {
    }
    
    public PEExportOrdinalTable(int count) : base(false)
    {
        CollectionsMarshal.SetCount(Values, count);
    }
    
    public List<ushort> Values { get; } = new();

    
    public override void UpdateLayout(PELayoutContext context)
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
}