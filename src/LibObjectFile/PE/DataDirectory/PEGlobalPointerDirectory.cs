// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.PE;

/// <summary>
/// Represents the GlobalPointer directory.
/// </summary>
public sealed class PEGlobalPointerDirectory : PEDataDirectory
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PEGlobalPointerDirectory"/> class.
    /// </summary>
    public PEGlobalPointerDirectory() : base(PEDataDirectoryKind.GlobalPointer)
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