// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Collections;
using System.Collections.Generic;

namespace LibObjectFile.PE;

public class PEImportAddressTable : PESectionData, IEnumerable<PEImportFunctionEntry>
{
    internal readonly PEImportFunctionTable FunctionTable;

    public PEImportAddressTable()
    {
        FunctionTable = new PEImportFunctionTable();
    }

    public override bool HasChildren => false;

    public List<PEImportFunctionEntry> Entries => FunctionTable.Entries;

    protected override void UpdateLayoutCore(PELayoutContext context)
    {
        Size = FunctionTable.CalculateSize(context);
    }

    public override void Read(PEImageReader reader)
    {
        FunctionTable.Read(reader, Position);
        UpdateLayout(reader);
    }

    public override void Write(PEImageWriter writer) => FunctionTable.Write(writer);

    public void Add(PEImportFunctionEntry entry) => Entries.Add(entry);
    
    public List<PEImportFunctionEntry>.Enumerator GetEnumerator() => Entries.GetEnumerator();

    IEnumerator<PEImportFunctionEntry> IEnumerable<PEImportFunctionEntry>.GetEnumerator() => Entries.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => Entries.GetEnumerator();
    
}