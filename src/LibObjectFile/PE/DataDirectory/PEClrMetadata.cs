// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.PE;

public sealed class PEClrMetadata : PEDataDirectory
{
    public PEClrMetadata() : base(PEDataDirectoryKind.ClrMetadata)
    {
    }

    protected override uint ComputeHeaderSize(PEVisitorContext context)
    {
        return 0;
    }

    public override void Read(PEImageReader reader)
    {
    }

    public override void Write(PEImageWriter writer)
    {
    }
}