// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Runtime.InteropServices;

namespace LibObjectFile.MachO.Internal;

#pragma warning disable CS0649

/// <summary>
/// A 32-bit symbol table entry (<c>nlist</c>).
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
internal struct RawNList32
{
    public uint StringIndex;
    public byte Type;
    public byte SectionIndex;
    /// <summary>
    /// The description field. The reference declares this signed for <c>nlist</c> and unsigned
    /// for <c>nlist_64</c>; it is read unsigned in both cases because it is a set of bit fields
    /// rather than a number, and its high byte is the ordinal of the library defining the symbol.
    /// </summary>
    public ushort Description;
    public uint Value;
}
