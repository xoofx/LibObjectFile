// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Runtime.InteropServices;

namespace LibObjectFile.MachO.Internal;

#pragma warning disable CS0649

/// <summary>
/// A 64-bit section header (<c>section_64</c>).
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
internal unsafe struct RawSection64
{
    public fixed byte SectionName[16];
    public fixed byte SegmentName[16];
    public ulong Address;
    public ulong Size;
    public uint Offset;
    public uint Align;
    public uint RelocationOffset;
    public uint NumberOfRelocations;
    public uint Flags;
    public uint Reserved1;
    public uint Reserved2;
    public uint Reserved3;
}
