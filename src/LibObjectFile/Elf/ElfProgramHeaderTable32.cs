// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using LibObjectFile.Collections;
using LibObjectFile.Diagnostics;

namespace LibObjectFile.Elf;

/// <summary>
/// The program header table.
/// </summary>
public sealed class ElfProgramHeaderTable32 : ElfProgramHeaderTable
{
    public ElfProgramHeaderTable32()
    {
        Alignment = 8;
    }

    public override unsafe void Read(ElfReader reader)
    {
        reader.Position = Position;

        var layout = reader.File.Layout;
        var programHeaderCount = layout.ProgramHeaderCount;

        using var tempSpan = TempSpan<byte>.Create((int)layout.SizeOfProgramHeaderEntry, out var span);
        if (layout.SizeOfProgramHeaderEntry < sizeof(ElfNative.Elf32_Phdr))
        {
            reader.Diagnostics.Error(DiagnosticId.ELF_ERR_InvalidProgramHeaderSize, $"Invalid program header size [{layout.SizeOfProgramHeaderEntry}] for 32bits. Expecting at least [{sizeof(ElfNative.Elf32_Phdr)}]");
            return;
        }

        AdditionalEntrySize = (uint)(layout.SizeOfProgramHeaderEntry - sizeof(ElfNative.Elf32_Phdr));

        for (int i = 0; i < programHeaderCount; i++)
        {
            var segment = ReadProgramHeader32(reader, i, span);
            reader.File.AddSegment(segment);
        }

        UpdateLayoutCore(reader);
    }

    protected override unsafe void UpdateLayoutCore(ElfVisitorContext context)
    {
        Size = (ulong)(Parent!.Segments.Count * (AdditionalEntrySize + sizeof(ElfNative.Elf32_Phdr)));
        base.UpdateLayoutCore(context);
    }

    public override void Write(ElfWriter writer)
    {
        writer.Position = Position;
        var segments = Parent!.Segments;
        for (int i = 0; i < segments.Count; i++)
        {
            var header = segments[i];
            WriteProgramHeader32(writer, header);
        }
    }

    private unsafe ElfSegment ReadProgramHeader32(ElfReader reader, int phdrIndex, Span<byte> buffer)
    {
        int read = reader.Read(buffer);
        if (read != buffer.Length)
        {
            reader.Diagnostics.Error(DiagnosticId.ELF_ERR_IncompleteProgramHeaderSize,
                $"Unable to read entirely program header [{phdrIndex}]. Not enough data (size: {reader.File.Layout.SizeOfProgramHeaderEntry}) read at offset {reader.Position} from the stream");
        }

        ref var hdr = ref MemoryMarshal.AsRef<ElfNative.Elf32_Phdr>(buffer);

        var additionalData = Array.Empty<byte>();
        if (buffer.Length > sizeof(ElfNative.Elf32_Phdr))
        {
            additionalData = buffer.Slice(sizeof(ElfNative.Elf32_Phdr)).ToArray();
        }

        return new ElfSegment
        {
            Type = new ElfSegmentType(reader.Decode(hdr.p_type)),
            Position = reader.Decode(hdr.p_offset),
            VirtualAddress = reader.Decode(hdr.p_vaddr),
            PhysicalAddress = reader.Decode(hdr.p_paddr),
            Size = reader.Decode(hdr.p_filesz),
            SizeInMemory = reader.Decode(hdr.p_memsz),
            Flags = new ElfSegmentFlags(reader.Decode(hdr.p_flags)),
            Alignment = reader.Decode(hdr.p_align),
            AdditionalData = additionalData
        };
    }

    private void WriteProgramHeader32(ElfWriter writer, ElfSegment segment)
    {
        var hdr = new ElfNative.Elf32_Phdr();

        writer.Encode(out hdr.p_type, segment.Type.Value);
        writer.Encode(out hdr.p_offset, (uint)segment.Position);
        writer.Encode(out hdr.p_vaddr, (uint)segment.VirtualAddress);
        writer.Encode(out hdr.p_paddr, (uint)segment.PhysicalAddress);
        writer.Encode(out hdr.p_filesz, (uint)segment.Size);
        writer.Encode(out hdr.p_memsz, (uint)segment.SizeInMemory);
        writer.Encode(out hdr.p_flags, segment.Flags.Value);
        writer.Encode(out hdr.p_align, (uint)segment.Alignment);

        writer.Write(hdr);

        if (segment.AdditionalData.Length > 0)
        {
            writer.Write(segment.AdditionalData);
        }
    }

    protected override void ValidateParent(ObjectElement parent) => ValidateParent(parent, ElfFileClass.Is32);
}