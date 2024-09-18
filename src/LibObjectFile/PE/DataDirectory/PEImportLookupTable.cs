﻿// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;

namespace LibObjectFile.PE;

public sealed class PEImportLookupTable : PEObject
{
    internal readonly PEImportFunctionTable FunctionTable;

    public PEImportLookupTable()
    {
        FunctionTable = new PEImportFunctionTable();
    }

    public new PEImportDirectoryEntry? Parent
    {
        get => (PEImportDirectoryEntry?)base.Parent;
        set => base.Parent = value;
    }

    public List<PEImportFunctionEntry> Entries => FunctionTable.Entries;
    
    public override void UpdateLayout(DiagnosticBag diagnostics)
    {
        var peFile = Parent?.Parent?.Parent?.Parent;
        if (peFile is null)
        {
            diagnostics.Error(DiagnosticId.PE_ERR_ImportLookupTableInvalidParent, "The parent of the Import Lookup Table is null.");
            return;
        }

        Size = FunctionTable.CalculateSize(peFile, diagnostics);
    }

    protected override void Read(PEImageReader reader)
    {
        FunctionTable.Read(reader, Position);
        UpdateLayout(reader.Diagnostics);
    }

    protected override void Write(PEImageWriter writer) => FunctionTable.Write(writer);
    
    protected override void ValidateParent(ObjectFileNodeBase parent)
    {
        if (parent is not PEImportDirectoryEntry)
        {
            throw new ArgumentException($"Invalid parent type [{parent?.GetType()}] for [{GetType()}]");
        }
    }
}