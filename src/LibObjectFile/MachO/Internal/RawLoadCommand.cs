// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Runtime.InteropServices;

namespace LibObjectFile.MachO.Internal;

#pragma warning disable CS0649

/// <summary>
/// The header every load command starts with (<c>load_command</c>). <c>CmdSize</c> covers the
/// header itself, so walking the command list means advancing by that value.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
internal struct RawLoadCommand
{
    public uint Cmd;
    public uint CmdSize;
}
