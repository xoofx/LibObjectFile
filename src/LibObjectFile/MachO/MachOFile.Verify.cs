// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using LibObjectFile.Diagnostics;

namespace LibObjectFile.MachO;

partial class MachOFile
{
    /// <summary>
    /// Checks this image for inconsistencies.
    /// </summary>
    /// <returns>What was found. Empty if nothing was.</returns>
    public DiagnosticBag Verify()
    {
        var diagnostics = new DiagnosticBag();
        Verify(diagnostics);
        return diagnostics;
    }

    /// <summary>
    /// Checks this image for inconsistencies.
    /// </summary>
    /// <param name="diagnostics">Receives what was found.</param>
    /// <exception cref="ArgumentNullException"><paramref name="diagnostics"/> is null.</exception>
    public void Verify(DiagnosticBag diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
        Verify(new MachOVisitorContext(this, diagnostics));
    }

    /// <inheritdoc />
    public override void Verify(MachOVisitorContext context)
    {
        VerifyContentCoversTheFile(context);
        VerifySegments(context);
        VerifySectionContent(context);
        VerifyLoadCommands(context);

        if (IsCodeSignatureStale)
        {
            context.Diagnostics.Error(
                DiagnosticId.MACHO_ERR_StaleCodeSignature,
                "The image has been edited since it was signed, so its signature no longer covers what it contains.");
        }
    }

    /// <summary>
    /// A section header says where its bytes are and how many there are, while the bytes
    /// themselves are a separate piece of content. Nothing keeps the two in step, so a header
    /// edited on its own would describe a section that is not there, and the round-trip would
    /// still match because both were written from what they each hold.
    /// </summary>
    private void VerifySectionContent(MachOVisitorContext context)
    {
        var byPosition = new Dictionary<ulong, MachOSectionData>();
        foreach (var content in Content)
        {
            if (content is MachOSectionData data)
            {
                byPosition[data.Position] = data;
            }
        }

        foreach (var segment in Segments)
        {
            foreach (var section in segment.Sections)
            {
                if (section.IsZeroFill || section.Size == 0) continue;

                if (!byPosition.TryGetValue(section.FileOffset, out var data))
                {
                    context.Diagnostics.Error(
                        DiagnosticId.MACHO_ERR_SectionContentMismatch,
                        $"Section {section.SegmentName},{section.Name} says its bytes are at 0x{section.FileOffset:X}, but no content of this image is there");
                    continue;
                }

                if (!ReferenceEquals(data.Section, section))
                {
                    context.Diagnostics.Error(
                        DiagnosticId.MACHO_ERR_SectionContentMismatch,
                        $"The content at 0x{section.FileOffset:X} belongs to {data.Section.SegmentName},{data.Section.Name} rather than to {section.SegmentName},{section.Name}");
                }
                else if (data.Size != section.Size)
                {
                    context.Diagnostics.Error(
                        DiagnosticId.MACHO_ERR_SectionContentMismatch,
                        $"Section {section.SegmentName},{section.Name} says it is 0x{section.Size:X} bytes but its content is 0x{data.Size:X}");
                }
            }
        }
    }

    /// <summary>
    /// Every byte of the file belongs to exactly one content, so the list has to run from the
    /// start of the file with no gap and no overlap. A gap would be bytes nothing writes, and an
    /// overlap would be bytes written twice with the later one winning silently.
    /// </summary>
    private void VerifyContentCoversTheFile(MachOVisitorContext context)
    {
        ulong expected = 0;
        foreach (var content in Content)
        {
            if (content.Position != expected)
            {
                context.Diagnostics.Error(
                    DiagnosticId.MACHO_ERR_ContentNotContiguous,
                    $"{content} starts at 0x{content.Position:X} but the content before it ends at 0x{expected:X}");
                return;
            }

            expected = content.Position + content.Size;
        }
    }

    private void VerifySegments(MachOVisitorContext context)
    {
        // An object file holds its sections in one unnamed segment and gives them addresses the
        // linker has yet to assign for real, so they do not track file offsets the way a linked
        // image's do. Apple's own crt1.o breaks the invariant below.
        var isLinkedImage = FileType != MachOFileType.Object;

        foreach (var segment in Segments)
        {
            if (segment.Is64Bit != Is64Bit)
            {
                context.Diagnostics.Error(
                    DiagnosticId.MACHO_ERR_InvalidImageBitness,
                    $"Segment {segment.Name} is {(segment.Is64Bit ? "64" : "32")}-bit in a {(Is64Bit ? "64" : "32")}-bit image");
            }

            foreach (var section in segment.Sections)
            {
                if (section.IsZeroFill || section.Size == 0) continue;

                // This is the invariant the whole format rests on: a section's address is its
                // segment's address plus its distance from the segment's file offset. Break it
                // and the loader maps the section somewhere other than where the code expects.
                if (isLinkedImage && section.Address - segment.VmAddress != section.FileOffset - segment.FileOffset)
                {
                    context.Diagnostics.Error(
                        DiagnosticId.MACHO_ERR_SectionAddressMismatch,
                        $"Section {section.SegmentName},{section.Name} is at address 0x{section.Address:X} and file offset 0x{section.FileOffset:X}, which do not agree with segment {segment.Name} at address 0x{segment.VmAddress:X} and file offset 0x{segment.FileOffset:X}");
                }

                if (section.FileOffset < segment.FileOffset || section.FileOffset + section.Size > segment.FileEndOffset)
                {
                    context.Diagnostics.Error(
                        DiagnosticId.MACHO_ERR_SectionOutsideSegment,
                        $"Section {section.SegmentName},{section.Name} spans [0x{section.FileOffset:X}, 0x{section.FileOffset + section.Size:X}) which is outside segment {segment.Name} at [0x{segment.FileOffset:X}, 0x{segment.FileEndOffset:X})");
                }
            }
        }
    }

    private void VerifyLoadCommands(MachOVisitorContext context)
    {
        var alignment = MachOLoadCommand.GetSizeAlignment(Is64Bit);

        foreach (var command in LoadCommands)
        {
            if (command.Size < 8 || command.Size % alignment != 0)
            {
                context.Diagnostics.Error(
                    DiagnosticId.MACHO_ERR_InvalidCommandAlignment,
                    $"{command} has a size of {command.Size}, which must be at least 8 and a multiple of {alignment} so that dyld can walk the table");
            }
        }

        var sizeOfCommands = ComputeSizeOfCommands();
        if (sizeOfCommands > uint.MaxValue)
        {
            context.Diagnostics.Error(
                DiagnosticId.MACHO_ERR_ValueTooLargeFor32Bit,
                $"The load commands total 0x{sizeOfCommands:X} bytes, which the 32-bit sizeofcmds field cannot record");
        }

        if (AvailableLoadCommandSpace < 0)
        {
            context.Diagnostics.Error(
                DiagnosticId.MACHO_ERR_NoRoomForLoadCommands,
                $"The load commands end at {LoadCommandsEndOffset} but the content after them starts at {ContentStartOffset}");
        }
    }
}
