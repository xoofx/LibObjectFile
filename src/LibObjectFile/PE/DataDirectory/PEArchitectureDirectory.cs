// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.PE;

/// <summary>
/// Represents the Architecture directory.
/// </summary>
public sealed class PEArchitectureDirectory : PEDataDirectory
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PEArchitectureDirectory"/> class.
    /// </summary>
    public PEArchitectureDirectory() : base(PEDataDirectoryKind.Architecture)
    {
    }

    protected override uint ComputeHeaderSize(PELayoutContext context)
    {
        return 0;
    }

    public override void Read(PEImageReader reader)
    {
        // TBD
    }

    public override void Write(PEImageWriter writer)
    {
    }
}