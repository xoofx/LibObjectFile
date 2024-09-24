// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Diagnostics;

namespace LibObjectFile.PE;

/// <summary>
/// A link to a section data.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct PESectionDataLink(PESectionData? Container, RVO RVO) : IPELink<PESectionData>
{
    public override string ToString() => this.ToDisplayTextWithRVA();
}