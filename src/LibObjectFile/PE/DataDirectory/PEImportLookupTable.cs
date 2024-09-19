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

    public override void UpdateLayout(PEVisitorContext context)
    {
        UpdateSize(context.File, context.Diagnostics);
    }

    public override void Read(PEImageReader reader)
    {
        FunctionTable.Read(reader, Position);
        UpdateSize(reader.File, reader.Diagnostics);
    }

    private void UpdateSize(PEFile file, DiagnosticBag diagnostics)
    {
        Size = FunctionTable.CalculateSize(file, diagnostics);
    }

    public override void Write(PEImageWriter writer) => FunctionTable.Write(writer);
}