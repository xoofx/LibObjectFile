// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LibObjectFile.Collections;
using LibObjectFile.Diagnostics;
using LibObjectFile.MachO.Internal;

namespace LibObjectFile.MachO;

/// <summary>
/// A segment load command (<c>LC_SEGMENT</c> or <c>LC_SEGMENT_64</c>) and the file content it maps.
/// </summary>
/// <remarks>
/// A segment describes a mapping rather than owning bytes: the bytes live in
/// <see cref="MachOFile.Content"/>, and a segment names the file range that is mapped and the
/// address it is mapped at. The <c>__TEXT</c> segment starts at file offset zero, so it covers
/// the header and the load commands that describe it as well as its own sections.
/// </remarks>
public sealed class MachOSegment : MachOLoadCommand
{
    private readonly ObjectList<MachOSection> _sections;

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public MachOSegment()
    {
        _sections = new ObjectList<MachOSection>(this);
    }

    /// <summary>
    /// Gets or sets the name of the segment, such as <c>__TEXT</c>. Truncated to 16 bytes on write.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the virtual address this segment is mapped at.
    /// </summary>
    public ulong VmAddress { get; set; }

    /// <summary>
    /// Gets or sets the number of bytes of address space this segment occupies. It may exceed
    /// <see cref="FileSize"/>, in which case the remainder is zero-filled at load time.
    /// </summary>
    public ulong VmSize { get; set; }

    /// <summary>
    /// Gets or sets the offset in the file of the bytes this segment maps.
    /// </summary>
    public ulong FileOffset { get; set; }

    /// <summary>
    /// Gets or sets the number of bytes in the file this segment maps.
    /// </summary>
    public ulong FileSize { get; set; }

    /// <summary>
    /// Gets or sets the highest protection this segment may be given.
    /// </summary>
    public MachOVmProtection MaxProtection { get; set; }

    /// <summary>
    /// Gets or sets the protection this segment is mapped with.
    /// </summary>
    public MachOVmProtection InitProtection { get; set; }

    /// <summary>
    /// Gets or sets the flags of this segment.
    /// </summary>
    public MachOSegmentFlags SegmentFlags { get; set; }

    /// <summary>
    /// Gets the sections contained in this segment.
    /// </summary>
    public ObjectList<MachOSection> Sections => _sections;

    /// <summary>
    /// Gets or sets whether this segment uses the 64-bit layout.
    /// </summary>
    public bool Is64Bit { get; set; }

    /// <summary>
    /// Gets the offset one past the last byte this segment maps in the file.
    /// </summary>
    public ulong FileEndOffset => FileOffset + FileSize;

    /// <summary>
    /// Gets the size in bytes a segment load command occupies for the given section count.
    /// </summary>
    /// <param name="is64Bit">Whether the segment uses the 64-bit layout.</param>
    /// <param name="sectionCount">The number of sections in the segment.</param>
    /// <returns>The value to store in <c>cmdsize</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="sectionCount"/> is negative, or so large that the command would not fit
    /// in the 32-bit <c>cmdsize</c> field.
    /// </exception>
    public static unsafe uint ComputeCommandSize(bool is64Bit, int sectionCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(sectionCount);

        // Computed wide so that a count large enough to overflow is rejected rather than
        // wrapping to a small, plausible-looking size.
        var total = GetFixedCommandSize(is64Bit) + (ulong)sectionCount * GetSectionSize(is64Bit);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(total, uint.MaxValue, nameof(sectionCount));

        return (uint)total;
    }

    /// <summary>
    /// Gets the size of a segment command excluding its section headers.
    /// </summary>
    /// <param name="is64Bit">Whether the segment uses the 64-bit layout.</param>
    /// <returns>The size of a <c>segment_command_64</c> or a <c>segment_command</c>.</returns>
    public static unsafe uint GetFixedCommandSize(bool is64Bit)
        => is64Bit ? (uint)sizeof(RawSegmentCommand64) : (uint)sizeof(RawSegmentCommand32);

    /// <summary>
    /// Gets the size of one section header.
    /// </summary>
    /// <param name="is64Bit">Whether the containing segment uses the 64-bit layout.</param>
    /// <returns>The size of a <c>section_64</c> or a <c>section</c>.</returns>
    public static unsafe uint GetSectionSize(bool is64Bit)
        => is64Bit ? (uint)sizeof(RawSection64) : (uint)sizeof(RawSection32);

    /// <inheritdoc />
    public override unsafe uint MinimumCommandSize
        => Is64Bit ? (uint)sizeof(RawSegmentCommand64) : (uint)sizeof(RawSegmentCommand32);

    /// <inheritdoc />
    protected override void UpdateLayoutCore(MachOVisitorContext context)
        => Size = ComputeCommandSize(Is64Bit, Sections.Count);

    /// <inheritdoc />
    /// <remarks>
    /// A section's relocations are a run of bytes elsewhere in the file, so their offset moves
    /// with them. The section's own file offset is not mapped here: it is placement, fixed by
    /// the address the section is mapped at, and cannot be changed without moving the section in
    /// memory too.
    /// </remarks>
    public override void UpdateFileOffsets(Func<uint, uint> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);

        foreach (var section in Sections)
        {
            if (section.RelocationOffset != 0) section.RelocationOffset = mapper(section.RelocationOffset);
        }
    }

    /// <inheritdoc />
    public override unsafe void Read(MachOReader reader)
    {
        if (Is64Bit)
        {
            if (!reader.TryReadData(sizeof(RawSegmentCommand64), out RawSegmentCommand64 raw))
            {
                reader.Diagnostics.Error(DiagnosticId.MACHO_ERR_TruncatedLoadCommand, $"Truncated 64-bit segment command at 0x{Position:X}");
                return;
            }

            Name = MachOName.Read(new ReadOnlySpan<byte>(raw.SegmentName, MachOName.Length));
            VmAddress = raw.VmAddress;
            VmSize = raw.VmSize;
            FileOffset = raw.FileOffset;
            FileSize = raw.FileSize;
            MaxProtection = (MachOVmProtection)raw.MaxProtection;
            InitProtection = (MachOVmProtection)raw.InitProtection;
            SegmentFlags = (MachOSegmentFlags)raw.Flags;
            ReadSections(reader, raw.NumberOfSections);
        }
        else
        {
            if (!reader.TryReadData(sizeof(RawSegmentCommand32), out RawSegmentCommand32 raw))
            {
                reader.Diagnostics.Error(DiagnosticId.MACHO_ERR_TruncatedLoadCommand, $"Truncated 32-bit segment command at 0x{Position:X}");
                return;
            }

            Name = MachOName.Read(new ReadOnlySpan<byte>(raw.SegmentName, MachOName.Length));
            VmAddress = raw.VmAddress;
            VmSize = raw.VmSize;
            FileOffset = raw.FileOffset;
            FileSize = raw.FileSize;
            MaxProtection = (MachOVmProtection)raw.MaxProtection;
            InitProtection = (MachOVmProtection)raw.InitProtection;
            SegmentFlags = (MachOSegmentFlags)raw.Flags;
            ReadSections(reader, raw.NumberOfSections);
        }
    }

    private unsafe void ReadSections(MachOReader reader, uint count)
    {
        Sections.Clear();

        // The section headers follow the fixed part inside this command, so a count that does not
        // fit would read whatever comes after it. The largest count that fits is derived by
        // division: multiplying the declared count out could wrap and pass a check it should not.
        var fixedSize = GetFixedCommandSize(Is64Bit);
        if (Size < fixedSize || count > (Size - fixedSize) / GetSectionSize(Is64Bit))
        {
            reader.Diagnostics.Error(
                DiagnosticId.MACHO_ERR_InvalidLoadCommandSize,
                $"Segment {Name} declares {count} sections, which do not fit in its cmdsize of {Size}");
            return;
        }

        for (uint i = 0; i < count; i++)
        {
            var section = new MachOSection();
            if (Is64Bit)
            {
                if (!reader.TryReadData(sizeof(RawSection64), out RawSection64 raw))
                {
                    reader.Diagnostics.Error(DiagnosticId.MACHO_ERR_TruncatedLoadCommand, $"Truncated 64-bit section header in segment {Name}");
                    return;
                }

                section.Name = MachOName.Read(new ReadOnlySpan<byte>(raw.SectionName, MachOName.Length));
                section.SegmentName = MachOName.Read(new ReadOnlySpan<byte>(raw.SegmentName, MachOName.Length));
                section.Address = raw.Address;
                section.Size = raw.Size;
                section.FileOffset = raw.Offset;
                section.Align = raw.Align;
                section.RelocationOffset = raw.RelocationOffset;
                section.NumberOfRelocations = raw.NumberOfRelocations;
                section.SetRawFlags(raw.Flags);
                section.Reserved1 = raw.Reserved1;
                section.Reserved2 = raw.Reserved2;
                section.Reserved3 = raw.Reserved3;
            }
            else
            {
                if (!reader.TryReadData(sizeof(RawSection32), out RawSection32 raw))
                {
                    reader.Diagnostics.Error(DiagnosticId.MACHO_ERR_TruncatedLoadCommand, $"Truncated 32-bit section header in segment {Name}");
                    return;
                }

                section.Name = MachOName.Read(new ReadOnlySpan<byte>(raw.SectionName, MachOName.Length));
                section.SegmentName = MachOName.Read(new ReadOnlySpan<byte>(raw.SegmentName, MachOName.Length));
                section.Address = raw.Address;
                section.Size = raw.Size;
                section.FileOffset = raw.Offset;
                section.Align = raw.Align;
                section.RelocationOffset = raw.RelocationOffset;
                section.NumberOfRelocations = raw.NumberOfRelocations;
                section.SetRawFlags(raw.Flags);
                section.Reserved1 = raw.Reserved1;
                section.Reserved2 = raw.Reserved2;
            }

            Sections.Add(section);
        }
    }

    /// <inheritdoc />
    public override unsafe void Write(MachOWriter writer)
    {
        if (Is64Bit)
        {
            var raw = new RawSegmentCommand64
            {
                Cmd = (uint)Type,
                CmdSize = ComputeCommandSize(true, Sections.Count),
                VmAddress = VmAddress,
                VmSize = VmSize,
                FileOffset = FileOffset,
                FileSize = FileSize,
                MaxProtection = (uint)MaxProtection,
                InitProtection = (uint)InitProtection,
                NumberOfSections = (uint)Sections.Count,
                Flags = (uint)SegmentFlags,
            };
            MachOName.Write(new Span<byte>(raw.SegmentName, MachOName.Length), Name);
            writer.Write(raw);
        }
        else
        {
            // A 32-bit segment stores these as 32-bit fields, and casting a value that does not
            // fit would write a different segment rather than fail.
            if (VmAddress > uint.MaxValue || VmSize > uint.MaxValue || FileOffset > uint.MaxValue || FileSize > uint.MaxValue)
            {
                writer.Diagnostics.Error(
                    DiagnosticId.MACHO_ERR_ValueTooLargeFor32Bit,
                    $"Segment {Name} has a value that does not fit a 32-bit image: VmAddress = 0x{VmAddress:X}, VmSize = 0x{VmSize:X}, FileOffset = 0x{FileOffset:X}, FileSize = 0x{FileSize:X}");
                return;
            }

            var raw = new RawSegmentCommand32
            {
                Cmd = (uint)Type,
                CmdSize = ComputeCommandSize(false, Sections.Count),
                VmAddress = (uint)VmAddress,
                VmSize = (uint)VmSize,
                FileOffset = (uint)FileOffset,
                FileSize = (uint)FileSize,
                MaxProtection = (uint)MaxProtection,
                InitProtection = (uint)InitProtection,
                NumberOfSections = (uint)Sections.Count,
                Flags = (uint)SegmentFlags,
            };
            MachOName.Write(new Span<byte>(raw.SegmentName, MachOName.Length), Name);
            writer.Write(raw);
        }

        foreach (var section in Sections)
        {
            WriteSection(writer, section);
        }
    }

    private unsafe void WriteSection(MachOWriter writer, MachOSection section)
    {
        if (Is64Bit)
        {
            var raw = new RawSection64
            {
                Address = section.Address,
                Size = section.Size,
                Offset = section.FileOffset,
                Align = section.Align,
                RelocationOffset = section.RelocationOffset,
                NumberOfRelocations = section.NumberOfRelocations,
                Flags = section.RawFlags,
                Reserved1 = section.Reserved1,
                Reserved2 = section.Reserved2,
                Reserved3 = section.Reserved3,
            };
            MachOName.Write(new Span<byte>(raw.SectionName, MachOName.Length), section.Name);
            MachOName.Write(new Span<byte>(raw.SegmentName, MachOName.Length), section.SegmentName);
            writer.Write(raw);
        }
        else
        {
            if (section.Address > uint.MaxValue || section.Size > uint.MaxValue)
            {
                writer.Diagnostics.Error(
                    DiagnosticId.MACHO_ERR_ValueTooLargeFor32Bit,
                    $"Section {section.SegmentName},{section.Name} has a value that does not fit a 32-bit image: Address = 0x{section.Address:X}, Size = 0x{section.Size:X}");
                return;
            }

            var raw = new RawSection32
            {
                Address = (uint)section.Address,
                Size = (uint)section.Size,
                Offset = section.FileOffset,
                Align = section.Align,
                RelocationOffset = section.RelocationOffset,
                NumberOfRelocations = section.NumberOfRelocations,
                Flags = section.RawFlags,
                Reserved1 = section.Reserved1,
                Reserved2 = section.Reserved2,
            };
            MachOName.Write(new Span<byte>(raw.SectionName, MachOName.Length), section.Name);
            MachOName.Write(new Span<byte>(raw.SegmentName, MachOName.Length), section.SegmentName);
            writer.Write(raw);
        }
    }

    /// <inheritdoc />
    protected override bool PrintMembers(StringBuilder builder)
    {
        builder.Append($"{Name} VmAddress = 0x{VmAddress:X}, VmSize = 0x{VmSize:X}, FileOffset = 0x{FileOffset:X}, FileSize = 0x{FileSize:X}, Sections = {Sections.Count}");
        return true;
    }
}
