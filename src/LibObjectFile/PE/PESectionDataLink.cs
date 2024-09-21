﻿// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Diagnostics;

namespace LibObjectFile.PE;

/// <summary>
/// A link to a section data.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public readonly struct PESectionDataLink : RVALink
{
    public PESectionDataLink(PESectionData? sectionData, uint offset)
    {
        SectionData = sectionData;
        Offset = offset;
    }

    public PESectionData? SectionData { get; }

    public PEVirtualObject? Container => SectionData;

    public uint Offset { get; }

    public override string ToString() => this.ToDisplayText();
}