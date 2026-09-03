// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Runtime.InteropServices;

namespace LibObjectFile.MachO.Internal;

#pragma warning disable CS0649

/// <summary>
/// The header of a universal binary (<c>fat_header</c>), followed by <c>NumberOfArchitectures</c>
/// architecture entries.
/// </summary>
/// <remarks>
/// Unlike every other structure in the format, the fat header and its architecture entries are
/// always stored big-endian regardless of the architectures they contain, so they cannot be read
/// by a straight copy on a little-endian host.
/// </remarks>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
internal struct RawFatHeader
{
    public uint Magic;
    public uint NumberOfArchitectures;
}
