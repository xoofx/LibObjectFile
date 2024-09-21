// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Diagnostics;

namespace LibObjectFile.PE;

#pragma warning disable CS0649
[DebuggerDisplay("{ToString(),nq}")]
public struct PEFunctionAddressLink : RVALink
{
    public PEFunctionAddressLink(PEVirtualObject? container, uint offsetInSection)
    {
        Container = container;
        Offset = offsetInSection;
    }

    public PEVirtualObject? Container { get; }

    public uint Offset { get; }

    public RVA RVA => Container is not null ? Container.VirtualAddress + Offset : 0;

    public override string ToString() => Container is not null ? $"{Container}, Offset = {Offset}" : $"<empty>";
}