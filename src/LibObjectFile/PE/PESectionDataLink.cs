// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Diagnostics;

namespace LibObjectFile.PE;

/// <summary>
/// A link to a section data.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public readonly struct PESectionDataLink : IRVALink
{
    public PESectionDataLink(PESectionData? sectionData, RVO rvo)
    {
        SectionData = sectionData;
        RVO = rvo;
    }

    public PESectionData? SectionData { get; }

    public PEObject? Container => SectionData;

    public RVO RVO { get; }

    public override string ToString() => this.ToDisplayText();
}