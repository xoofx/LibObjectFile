// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Runtime.InteropServices;

namespace LibObjectFile.MachO.Internal;

#pragma warning disable CS0649

/// <summary>
/// The shared shape of the load commands that carry a single path, covering
/// <c>rpath_command</c>, <c>dylinker_command</c> and <c>sub_*_command</c>.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
internal struct RawPathCommand
{
    public uint Cmd;
    public uint CmdSize;
    public uint PathOffset;
}
