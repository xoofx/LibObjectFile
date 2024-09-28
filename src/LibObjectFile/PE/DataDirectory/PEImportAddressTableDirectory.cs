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

    public override void Write(PEImageWriter writer)
    {
    }

    protected override uint ComputeHeaderSize(PELayoutContext context) => 0;
}