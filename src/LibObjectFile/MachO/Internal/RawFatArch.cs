// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Runtime.InteropServices;

namespace LibObjectFile.MachO.Internal;

#pragma warning disable CS0649

/// <summary>
/// One slice of a universal binary (<c>fat_arch</c>). Stored big-endian; see <see cref="RawFatHeader"/>.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
internal struct RawFatArch
{
    public uint CpuType;
    public uint CpuSubType;
    public uint Offset;
    public uint Size;
    public uint Align;
}
