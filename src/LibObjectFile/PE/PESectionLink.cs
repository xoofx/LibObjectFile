// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Diagnostics;

namespace LibObjectFile.PE;

/// <summary>
/// A link to a section
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public readonly struct PESectionLink : RVALink
{
    public PESectionLink(PESection? section, uint offset)
    {
        Section = section;
        Offset = offset;
    }

    public PESection? Section { get; }

    public PEVirtualObject? Container => Section;

    public uint Offset { get; }

    public override string ToString() => this.ToDisplayText();
}