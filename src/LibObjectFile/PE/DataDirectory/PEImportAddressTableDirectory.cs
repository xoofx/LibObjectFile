// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using LibObjectFile.Collections;

namespace LibObjectFile.PE;

public sealed class PEImportAddressTableDirectory : PEDirectory
{
    private readonly ObjectList<PEImportAddressTable> _tables;

    public PEImportAddressTableDirectory() : base(ImageDataDirectoryKind.ImportAddressTable)
    {
        _tables = new ObjectList<PEImportAddressTable>(this);
    }

    public ObjectList<PEImportAddressTable> Tables => _tables;

    public override void UpdateLayout(PEVisitorContext context)
    {
        ulong size = 0;
        foreach (var table in _tables)
        {
            table.UpdateLayout(context);
            size += table.Size;
        }
        Size = size;
    }

    public override void Read(PEImageReader reader) => throw new NotSupportedException(); // Not called directly for this object, we are calling on tables directly

    public override void Write(PEImageWriter writer) => throw new NotSupportedException(); // Not called directly for this object, we are calling on tables directly
}