// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Runtime.InteropServices;

namespace LibObjectFile.MachO.Internal;

#pragma warning disable CS0649

/// <summary>
/// A dylib load command (<c>dylib_command</c>), followed by the library path.
/// </summary>
/// <remarks>
/// <c>NameOffset</c> is measured from the start of the command, not from the end of this
/// structure, so a command whose path is preceded by padding is still valid.
/// </remarks>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
internal struct RawDylibCommand
{
    public uint Cmd;
    public uint CmdSize;
    public uint NameOffset;
    public uint Timestamp;
    public uint CurrentVersion;
    public uint CompatibilityVersion;
}
