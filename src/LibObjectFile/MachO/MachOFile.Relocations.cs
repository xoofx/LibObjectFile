// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;

namespace LibObjectFile.MachO;

partial class MachOFile
{
    /// <summary>
    /// Reads the relocations of a section.
    /// </summary>
    /// <param name="section">The section whose relocations to read.</param>
    /// <returns>The relocations, or an empty list if the section has none.</returns>
    /// <remarks>
    /// Only an object file normally carries these. A linked image resolves them at link time and
    /// keeps just what the loader still needs, as dyld info opcodes or chained fixups.
    /// <para>
    /// A pair of entries can describe one fixup between them, with the second carrying the other
    /// half. They are returned in file order so a caller can pair them up.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="section"/> is null.</exception>
    /// <exception cref="ObjectFileException">The relocations lie outside the content of this image.</exception>
    public IReadOnlyList<MachORelocation> ReadRelocations(MachOSection section)
    {
        ArgumentNullException.ThrowIfNull(section);

        if (section.NumberOfRelocations == 0) return [];

        var bytes = ReadFileBytes(section.RelocationOffset, (ulong)section.NumberOfRelocations * MachORelocation.EntrySize, $"relocations of {section.SegmentName},{section.Name}");
        var relocations = new MachORelocation[section.NumberOfRelocations];
        for (var i = 0; i < relocations.Length; i++)
        {
            relocations[i] = MachORelocation.Decode(
                BitConverter.ToUInt32(bytes, i * (int)MachORelocation.EntrySize),
                BitConverter.ToUInt32(bytes, i * (int)MachORelocation.EntrySize + 4));
        }

        return relocations;
    }
}
