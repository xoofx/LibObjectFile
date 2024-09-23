// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Diagnostics;

namespace LibObjectFile.PE;

/// <summary>
/// A link to a section
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public readonly struct PESectionLink : IRVALink
{
    public PESectionLink(PESection? section, uint rvo)
    {
        Section = section;
        RVO = rvo;
    }

    public PESection? Section { get; }

    public PEObject? Container => Section;

    public RVO RVO { get; }

    public override string ToString() => this.ToDisplayText();
}