// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Runtime.InteropServices;

namespace LibObjectFile.MachO.Internal;

#pragma warning disable CS0649

/// <summary>
/// The image identifier load command (<c>uuid_command</c>).
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
internal unsafe struct RawUuidCommand
{
    public uint Cmd;
    public uint CmdSize;
    public fixed byte Uuid[16];
}
