// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Runtime.InteropServices;

namespace LibObjectFile.MachO.Internal;

#pragma warning disable CS0649

/// <summary>
/// A 32-bit segment load command (<c>segment_command</c>), followed in the file by
/// <c>NumberOfSections</c> <see cref="RawSection32"/> entries.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
internal unsafe struct RawSegmentCommand32
{
    public uint Cmd;
    public uint CmdSize;
    public fixed byte SegmentName[16];
    public uint VmAddress;
    public uint VmSize;
    public uint FileOffset;
    public uint FileSize;
    public uint MaxProtection;
    public uint InitProtection;
    public uint NumberOfSections;
    public uint Flags;
}
