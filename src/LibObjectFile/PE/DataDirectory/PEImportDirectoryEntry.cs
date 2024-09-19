// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using LibObjectFile.PE.Internal;

namespace LibObjectFile.PE;

public sealed class PEImportDirectoryEntry : PEObject
{
    public PEImportDirectoryEntry(ZeroTerminatedAsciiStringLink importDllNameLink, PEImportAddressTable importAddressTable, PEImportLookupTable importLookupTable)
    {
        ImportDllNameLink = importDllNameLink;
        ImportAddressTable = importAddressTable;
        ImportLookupTable = importLookupTable;
    }

    public new PEImportDirectory? Parent
    {
        get => (PEImportDirectory?)base.Parent;
        set => base.Parent = value;
    }
    
    public ZeroTerminatedAsciiStringLink ImportDllNameLink { get; set; }

    public PEImportAddressTable ImportAddressTable { get; set; }

    public PEImportLookupTable ImportLookupTable { get; set; }

    public override unsafe void UpdateLayout(PEVisitorContext context)
    {
        Size = (ulong)sizeof(RawImportDirectoryEntry);

        // Update the layout of the import lookup table
        ImportLookupTable.UpdateLayout(context);
    }

    public override void Read(PEImageReader reader) => throw new System.NotSupportedException();

    public override void Write(PEImageWriter writer) => throw new System.NotSupportedException();
}