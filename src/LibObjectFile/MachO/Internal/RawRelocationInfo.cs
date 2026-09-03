// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Runtime.InteropServices;

namespace LibObjectFile.MachO.Internal;

#pragma warning disable CS0649

/// <summary>
/// A relocation entry (<c>relocation_info</c> or <c>scattered_relocation_info</c>).
/// </summary>
/// <remarks>
/// The two forms share a size and are told apart by the top bit of the first word. C bitfields
/// have no equivalent here, so both words are kept whole and unpacked by hand, which also avoids
/// depending on how a compiler happens to lay the fields out.
/// </remarks>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
internal struct RawRelocationInfo
{
    public uint Word0;
    public uint Word1;
}
