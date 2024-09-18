﻿// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using LibObjectFile.Diagnostics;

namespace LibObjectFile.PE;

public class PEImportAddressTable : PEObject
{
    internal readonly PEImportFunctionTable FunctionTable;

    public PEImportAddressTable()
    {
        FunctionTable = new PEImportFunctionTable();
    }

    public new PEImportAddressTableDirectory? Parent => (PEImportAddressTableDirectory?)base.Parent;
    
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

    protected override void ValidateParent(ObjectFileElement parent)
    {
        if (parent is not PEImportAddressTableDirectory)
        {
            throw new ArgumentException($"Invalid parent type [{parent?.GetType()}] for [{GetType()}]");
        }
    }
}