// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using LibObjectFile.Collections;
using LibObjectFile.Diagnostics;

namespace LibObjectFile.Elf;

partial class ElfProgramHeaderTable
{
    private unsafe void Read64(ElfReader reader)
    {
        reader.Position = Position;

        var layout = reader.File.Layout;
        var programHeaderCount = layout.ProgramHeaderCount;

        using var tempSpan = TempSpan<byte>.Create((int)layout.SizeOfProgramHeaderEntry, out var span);
        if (layout.SizeOfProgramHeaderEntry < sizeof(ElfNative.Elf64_Phdr))
        {
            reader.Diagnostics.Error(DiagnosticId.ELF_ERR_InvalidProgramHeaderSize, $"Invalid program header size [{layout.SizeOfProgramHeaderEntry}] for 64bits. Expecting at least [{sizeof(ElfNative.Elf64_Phdr)}]");
            return;
        }

        AdditionalEntrySize = (uint)(layout.SizeOfProgramHeaderEntry - sizeof(ElfNative.Elf64_Phdr));

        for (int i = 0; i < programHeaderCount; i++)
        {
            var segment = ReadProgramHeader64(reader, i, span);
            reader.File.Segments.Add(segment);
        }

        UpdateLayoutCore(reader);
    }

    private void Write64(ElfWriter writer)
    {
        for (int i = 0; i < Parent!.Segments.Count; i++)
        {
            var header = Parent.Segments[i];
            WriteProgramHeader64(writer, header);
        }
    }

    private unsafe ElfSegment ReadProgramHeader64(ElfReader reader, int phdrIndex, Span<byte> buffer)
    {
        int read = reader.Read(buffer);
        if (read != buffer.Length)
        {
            reader.Diagnostics.Error(DiagnosticId.ELF_ERR_IncompleteProgramHeaderSize, $"Unable to read entirely program header [{phdrIndex}]. Not enough data (size: {reader.File.Layout.SizeOfProgramHeaderEntry}) read at offset {reader.Position} from the stream");
        }

        ref var hdr = ref MemoryMarshal.AsRef<ElfNative.Elf64_Phdr>(buffer);

        var additionalData = Array.Empty<byte>();
        if (buffer.Length > sizeof(ElfNative.Elf64_Phdr))
        {
            additionalData = buffer.Slice(sizeof(ElfNative.Elf64_Phdr)).ToArray();
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
            VirtualAddressAlignment = reader.Decode(hdr.p_align),
            AdditionalData = additionalData
        };
    }

    private void WriteProgramHeader64(ElfWriter writer, ElfSegment segment)
    {
        var hdr = new ElfNative.Elf64_Phdr();

        writer.Encode(out hdr.p_type, segment.Type.Value);
        writer.Encode(out hdr.p_offset, segment.Position);
        writer.Encode(out hdr.p_vaddr, segment.VirtualAddress);
        writer.Encode(out hdr.p_paddr, segment.PhysicalAddress);
        writer.Encode(out hdr.p_filesz, segment.Size);
        writer.Encode(out hdr.p_memsz, segment.SizeInMemory);
        writer.Encode(out hdr.p_flags, segment.Flags.Value);
        writer.Encode(out hdr.p_align, segment.VirtualAddressAlignment);

        writer.Write(hdr);

        if (segment.AdditionalData.Length > 0)
        {
            writer.Write(segment.AdditionalData);
        }
    }
}