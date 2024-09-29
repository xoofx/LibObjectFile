// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.PE;

/// <summary>
/// Represents the CLR metadata directory in a PE file.
/// </summary>
public sealed class PEClrMetadata : PEDataDirectory
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PEClrMetadata"/> class.
    /// </summary>
    public PEClrMetadata() : base(PEDataDirectoryKind.ClrMetadata)
    {
    }

    /// <inheritdoc/>
    protected override uint ComputeHeaderSize(PELayoutContext context) => 0;

    /// <inheritdoc/>
    public override void Read(PEImageReader reader)
    {
    }

    /// <inheritdoc/>
    public override void Write(PEImageWriter writer)
    {
    }
}
