// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.Elf;

/// <summary>
/// Contains the layout of an object available after reading an <see cref="ElfFile"/>
/// or after calling <see cref="ElfFile.UpdateLayout"/>
/// </summary>
public sealed class ElfFileLayout
{
    internal ElfFileLayout()
    {
    }

    /// <summary>
    /// Size of ELF Header.
    /// </summary>
    public ushort SizeOfElfHeader { get; internal set; }

    /// <summary>
    /// Offset of the program header table.
    /// </summary>
    public ulong OffsetOfProgramHeaderTable { get; internal set; }

    /// <summary>
    /// Size of a program header entry.
    /// </summary>
    public ushort SizeOfProgramHeaderEntry { get; internal set; }

    /// <summary>
    /// The number of header entries.
    /// </summary>
    public uint ProgramHeaderCount { get; internal set; }

    /// <summary>
    /// Offset of the section header table.
    /// </summary>
    public ulong OffsetOfSectionHeaderTable { get; internal set; }

    /// <summary>
    /// Size of a section header entry.
    /// </summary>
    public ushort SizeOfSectionHeaderEntry { get; internal set; }

    /// <summary>
    /// The number of section header entries.
    /// </summary>
    public uint SectionHeaderCount { get; internal set; }

    /// <summary>
    /// Size of the entire file
    /// </summary>
    public ulong TotalSize { get; internal set; }

    /// <summary>
    /// The index of the section string table.
    /// </summary>
    public uint SectionStringTableIndex { get; internal set; }
}