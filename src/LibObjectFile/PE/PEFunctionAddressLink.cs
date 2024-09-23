// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Diagnostics;

namespace LibObjectFile.PE;

#pragma warning disable CS0649
[DebuggerDisplay("{ToString(),nq}")]
public struct PEFunctionAddressLink : IRVALink
{
    public PEFunctionAddressLink(PEObject? container, RVO rvo)
    {
        Container = container;
        RVO = rvo;
    }

    public PEObject? Container { get; }

    public RVO RVO { get; }

    public RVA RVA => Container is not null ? Container.RVA + RVO : 0;

    public override string ToString() => Container is not null ? $"{Container}, Offset = {RVO}" : $"<empty>";
}