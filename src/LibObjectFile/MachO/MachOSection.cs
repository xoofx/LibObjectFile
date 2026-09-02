// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Text;

namespace LibObjectFile.MachO;

/// <summary>
/// A section inside a <see cref="MachOSegment"/>.
/// </summary>
/// <remarks>
/// A section records an absolute file offset rather than one relative to its segment, and it
/// repeats the name of the segment containing it. Sections whose <see cref="SectionType"/> is a
/// zero-fill kind occupy address space but no file space, and their <see cref="FileOffset"/>
/// does not point at bytes belonging to them.
/// </remarks>
public sealed class MachOSection : MachOObject
{
    /// <summary>
    /// Gets or sets the name of the section, such as <c>__text</c>. Truncated to 16 bytes on write.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the segment containing this section, such as <c>__TEXT</c>.
    /// </summary>
    public string SegmentName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the virtual address of this section.
    /// </summary>
    public ulong Address { get; set; }

    /// <summary>
    /// Gets or sets the file offset of this section. Meaningless for zero-fill sections.
    /// </summary>
    public uint FileOffset { get; set; }

    /// <summary>
    /// Gets or sets the alignment of this section as a power of two.
    /// </summary>
    public uint Align { get; set; }

    /// <summary>
    /// Gets or sets the file offset of the relocation entries for this section.
    /// </summary>
    public uint RelocationOffset { get; set; }

    /// <summary>
    /// Gets or sets the number of relocation entries for this section.
    /// </summary>
    public uint NumberOfRelocations { get; set; }

    /// <summary>
    /// Gets or sets the type of this section, held in the low byte of the raw flags.
    /// </summary>
    public MachOSectionType SectionType { get; set; }

    /// <summary>
    /// Gets or sets the attributes of this section, held in the upper three bytes of the raw flags.
    /// </summary>
    public MachOSectionAttributes Attributes { get; set; }

    /// <summary>
    /// Gets or sets the first reserved field. It holds the indirect symbol table index for
    /// stub and symbol pointer sections, and the ordinal for others.
    /// </summary>
    public uint Reserved1 { get; set; }

    /// <summary>
    /// Gets or sets the second reserved field. It holds the stub size for
    /// <see cref="MachOSectionType.SymbolStubs"/> sections.
    /// </summary>
    public uint Reserved2 { get; set; }

    /// <summary>
    /// Gets or sets the third reserved field, present only in 64-bit sections.
    /// </summary>
    public uint Reserved3 { get; set; }

    /// <summary>
    /// Gets a value indicating whether this section occupies no space in the file.
    /// </summary>
    public bool IsZeroFill => SectionType is MachOSectionType.ZeroFill or MachOSectionType.GBZeroFill or MachOSectionType.ThreadLocalZeroFill;

    /// <summary>
    /// Gets the raw <c>flags</c> value combining <see cref="SectionType"/> and <see cref="Attributes"/>.
    /// </summary>
    public uint RawFlags => (uint)SectionType | (uint)Attributes;

    /// <summary>
    /// Sets <see cref="SectionType"/> and <see cref="Attributes"/> from a raw <c>flags</c> value.
    /// </summary>
    /// <param name="rawFlags">The raw section flags.</param>
    public void SetRawFlags(uint rawFlags)
    {
        SectionType = (MachOSectionType)(rawFlags & 0xff);
        Attributes = (MachOSectionAttributes)(rawFlags & 0xffffff00);
    }

    /// <inheritdoc />
    protected override void PrintName(StringBuilder builder) => builder.Append(nameof(MachOSection));

    /// <inheritdoc />
    protected override bool PrintMembers(StringBuilder builder)
    {
        builder.Append($"{SegmentName},{Name} Address = 0x{Address:X}, Size = 0x{Size:X}, FileOffset = 0x{FileOffset:X}");
        return true;
    }
}
