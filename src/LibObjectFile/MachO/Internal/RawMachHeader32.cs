// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Runtime.InteropServices;

namespace LibObjectFile.MachO.Internal;

#pragma warning disable CS0649

/// <summary>
/// The 32-bit Mach-O file header (<c>mach_header</c>).
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
internal struct RawMachHeader32
{
    public uint Magic;
    public uint CpuType;
    public uint CpuSubType;
    public uint FileType;
    public uint NumberOfCommands;
    public uint SizeOfCommands;
    public uint Flags;
}
