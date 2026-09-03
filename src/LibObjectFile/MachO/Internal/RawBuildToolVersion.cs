// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Runtime.InteropServices;

namespace LibObjectFile.MachO.Internal;

#pragma warning disable CS0649

/// <summary>
/// One tool entry of a build version command (<c>build_tool_version</c>).
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
internal struct RawBuildToolVersion
{
    public uint Tool;
    public uint Version;
}
