// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Diagnostics;

namespace LibObjectFile.PE;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct PEBlobDataLink(PEObjectBase? Container, RVO RVO, uint Size) : IPELink<PEObjectBase>
{
    public override string ToString() => $"{this.ToDisplayText()}, Size = 0x{Size:X}";
}