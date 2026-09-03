// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Runtime.InteropServices;

namespace LibObjectFile.MachO.Internal;

#pragma warning disable CS0649

/// <summary>
/// The build version load command (<c>build_version_command</c>), followed by
/// <c>ToolCount</c> tool entries.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
internal struct RawBuildVersionCommand
{
    public uint Cmd;
    public uint CmdSize;
    public uint Platform;
    public uint MinOS;
    public uint Sdk;
    public uint ToolCount;
}
