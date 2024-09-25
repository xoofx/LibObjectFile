// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Collections.Generic;

namespace LibObjectFile.PE;

public class PEImportAddressTable : PESectionData
{
    internal readonly PEImportFunctionTable FunctionTable;

    public PEImportAddressTable()
    {
        FunctionTable = new PEImportFunctionTable();
    }

    public override bool HasChildren => false;

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