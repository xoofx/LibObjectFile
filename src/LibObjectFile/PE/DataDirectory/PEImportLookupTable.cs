// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using LibObjectFile.Diagnostics;

namespace LibObjectFile.PE;

public sealed class PEImportLookupTable : PESectionData
{
    internal readonly PEImportFunctionTable FunctionTable;

    public PEImportLookupTable() : base(false)
    {
        FunctionTable = new PEImportFunctionTable();
    }

    public List<PEImportFunctionEntry> Entries => FunctionTable.Entries;

    public override void UpdateLayout(PELayoutContext context)
    {
        Size = FunctionTable.CalculateSize(context);
    }

    public override void Read(PEImageReader reader)
    {
        FunctionTable.Read(reader, Position);
        UpdateLayout(reader);
    }
    
    public override void Write(PEImageWriter writer) => FunctionTable.Write(writer);
}