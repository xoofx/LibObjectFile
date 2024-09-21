// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using LibObjectFile.Collections;

namespace LibObjectFile.PE;

public sealed class PEImportAddressTableDirectory : PEDataDirectory
{
    public PEImportAddressTableDirectory() : base(PEDataDirectoryKind.ImportAddressTable)
    {
    }

    public override void Read(PEImageReader reader)
    {
    }

    public override void Write(PEImageWriter writer) => throw new NotSupportedException(); // Not called directly for this object, we are calling on tables directly

    protected override uint ComputeHeaderSize(PEVisitorContext context) => 0;
}