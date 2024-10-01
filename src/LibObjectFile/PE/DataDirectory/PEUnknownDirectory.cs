// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.PE;

/// <summary>
/// Represents an unknown directory (when going beyond the known directories).
/// </summary>
public sealed class PEUnknownDirectory : PEDataDirectory
{
    internal PEUnknownDirectory(int index) : base((PEDataDirectoryKind)index)
    {
    }

    protected override uint ComputeHeaderSize(PELayoutContext context) => 0;
}