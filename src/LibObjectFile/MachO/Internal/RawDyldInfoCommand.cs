// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Runtime.InteropServices;

namespace LibObjectFile.MachO.Internal;

#pragma warning disable CS0649

/// <summary>
/// The compressed dyld information load command (<c>dyld_info_command</c>).
/// </summary>
/// <remarks>
/// Each pair locates an opcode stream in <c>__LINKEDIT</c>. The streams address their targets by
/// segment index and offset within that segment, never by file offset, so relocating them only
/// requires updating the offsets recorded here.
/// </remarks>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
internal struct RawDyldInfoCommand
{
    public uint Cmd;
    public uint CmdSize;
    public uint RebaseOffset;
    public uint RebaseSize;
    public uint BindOffset;
    public uint BindSize;
    public uint WeakBindOffset;
    public uint WeakBindSize;
    public uint LazyBindOffset;
    public uint LazyBindSize;
    public uint ExportOffset;
    public uint ExportSize;
}
