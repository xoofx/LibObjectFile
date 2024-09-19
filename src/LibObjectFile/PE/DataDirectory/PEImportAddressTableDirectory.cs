// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using LibObjectFile.Collections;

namespace LibObjectFile.PE;

public sealed class PEImportAddressTableDirectory : PEDirectory
{
    private readonly ObjectList<PEVirtualObject> _content;

    public PEImportAddressTableDirectory() : base(ImageDataDirectoryKind.ImportAddressTable, true)
    {
        _content = CreateObjectList<PEVirtualObject>(this);
    }

    public ObjectList<PEVirtualObject> Content => _content;

    public override void UpdateLayout(PEVisitorContext context)
    {
        ulong size = 0;
        var va = VirtualAddress;
        foreach (var table in _content)
        {
            table.VirtualAddress = va;
            // Update layout will update virtual address
            table.UpdateLayout(context);
            va += (uint)table.Size;
            size += table.Size;
        }
        Size = size;
    }

    protected override bool TryFindByVirtualAddressInChildren(RVA virtualAddress, out PEVirtualObject? result)
    {
        var content = CollectionsMarshal.AsSpan(_content.UnsafeList);
        foreach (var table in content)
        {
            if (table.TryFindByVirtualAddress(virtualAddress, out result))
            {
                return true;
            }
        }

        result = null;
        return false;
    }
    
    protected override void UpdateVirtualAddressInChildren()
    {
        var va = VirtualAddress;
        foreach (var table in _content)
        {
            table.UpdateVirtualAddress(va);
            va += (uint)table.Size;
        }
    }

    public override void Read(PEImageReader reader) => throw new NotSupportedException(); // Not called directly for this object, we are calling on tables directly

    public override void Write(PEImageWriter writer) => throw new NotSupportedException(); // Not called directly for this object, we are calling on tables directly
}