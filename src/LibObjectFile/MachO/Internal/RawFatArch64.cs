// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Runtime.InteropServices;

namespace LibObjectFile.MachO.Internal;

#pragma warning disable CS0649

/// <summary>
/// One slice of a universal binary using 64-bit offsets (<c>fat_arch_64</c>), used when a slice
/// starts beyond 4GB. Stored big-endian; see <see cref="RawFatHeader"/>.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
internal struct RawFatArch64
{
    public uint CpuType;
    public uint CpuSubType;
    public ulong Offset;
    public ulong Size;
    public uint Align;
    public uint Reserved;
}
