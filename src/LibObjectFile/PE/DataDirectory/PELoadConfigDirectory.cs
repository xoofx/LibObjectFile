// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.PE;

public sealed class PELoadConfigDirectory : PEDataDirectory
{
    public PELoadConfigDirectory() : base(PEDataDirectoryKind.LoadConfig)
    {
    }
    protected override uint ComputeHeaderSize(PEVisitorContext context)
    {
        return 0;
    }


    public override void Read(PEImageReader reader)
    {
        // TBD
    }

    public override void Write(PEImageWriter writer)
    {
        throw new NotImplementedException();
    }
}