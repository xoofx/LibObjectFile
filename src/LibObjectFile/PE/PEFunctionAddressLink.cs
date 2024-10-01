// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Diagnostics;

namespace LibObjectFile.PE;

#pragma warning disable CS0649
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct PEFunctionAddressLink(PEObject? Container, RVO RVO) : IPELink<PEObject>
{
    public override string ToString() => Container is not null ? $"{Container}, Offset = {RVO}" : $"<empty>";
}