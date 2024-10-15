// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using LibObjectFile.Diagnostics;
using Microsoft.VisualBasic.FileIO;
using System.Diagnostics;
using System;

namespace LibObjectFile.Elf;

/// <summary>
/// Represents the content of an Elf Header. It comes always as the first content of an <see cref="ElfFile"/>.
/// </summary>
public sealed class ElfHeaderContent : ElfContentData
{
    internal ElfHeaderContent()
    {
    }

    public override void Read(ElfReader reader)
    {
        reader.Position = 0;
        ReadElfHeader(reader);
    }

    public override void Write(ElfWriter writer)
    {
        WriteHeader(writer);
    }

    protected override unsafe void UpdateLayoutCore(ElfVisitorContext context)
    {
        var file = context.File;
        var is32 = file.FileClass == ElfFileClass.Is32;
        Size = (ulong)((is32 ? (uint)sizeof(ElfNative.Elf32_Ehdr) : (uint)sizeof(ElfNative.Elf64_Ehdr)) + (uint)file.AdditionalHeaderData.Length);
        file.Layout.SizeOfElfHeader = (ushort)Size;
    }

    private void ReadElfHeader(ElfReader reader)
    {
        var file = reader.File;
        if (file.FileClass == ElfFileClass.Is32)
        {
            ReadElfHeader32(reader);
        }
        else
        {
            ReadElfHeader64(reader);
        }

        Size = file.Layout.SizeOfElfHeader;
        Debug.Assert(reader.Position == file.Layout.SizeOfElfHeader);

        //if (_sectionHeaderCount >= ElfNative.SHN_LORESERVE)
        //{
        //    Diagnostics.Error(DiagnosticId.ELF_ERR_InvalidSectionHeaderCount, $"Invalid number `{_sectionHeaderCount}` of section headers found from Elf Header. Must be < {ElfNative.SHN_LORESERVE}");
        //}
    }

    private unsafe void ReadElfHeader32(ElfReader reader)
    {
        var file = reader.File;
        if (!reader.TryReadData(sizeof(ElfNative.Elf32_Ehdr), out ElfNative.Elf32_Ehdr hdr))
        {
            reader.Diagnostics.Error(DiagnosticId.ELF_ERR_IncompleteHeader32Size, $"Unable to read entirely Elf header. Not enough data (size: {sizeof(ElfNative.Elf32_Ehdr)}) read at offset {reader.Position} from the stream");
            return;
        }

        file.FileType = (ElfFileType)reader.Decode(hdr.e_type);
        file.Arch = new ElfArchEx(reader.Decode(hdr.e_machine));
        file.Version = reader.Decode(hdr.e_version);

        file.EntryPointAddress = reader.Decode(hdr.e_entry);
        file.Layout.SizeOfElfHeader = reader.Decode(hdr.e_ehsize);
        file.Flags = reader.Decode(hdr.e_flags);

        // program headers
        file.Layout.OffsetOfProgramHeaderTable = reader.Decode(hdr.e_phoff);
        file.Layout.SizeOfProgramHeaderEntry = reader.Decode(hdr.e_phentsize);
        file.Layout.ProgramHeaderCount = reader.Decode(hdr.e_phnum);

        // entries for sections
        file.Layout.OffsetOfSectionHeaderTable = reader.Decode(hdr.e_shoff);
        file.Layout.SizeOfSectionHeaderEntry = reader.Decode(hdr.e_shentsize);
        file.Layout.SectionHeaderCount = reader.Decode(hdr.e_shnum);
        file.Layout.SectionStringTableIndex = reader.Decode(hdr.e_shstrndx);

        var sizeOfAdditionalHeaderData = file.Layout.SizeOfElfHeader - sizeof(ElfNative.Elf32_Ehdr);
        if (sizeOfAdditionalHeaderData < 0)
        {
            reader.Diagnostics.Error(DiagnosticId.ELF_ERR_InvalidElfHeaderSize, $"Invalid size of Elf header [{file.Layout.SizeOfElfHeader}] < {sizeof(ElfNative.Elf32_Ehdr)}");
            return;
        }

        // Read any additional data
        if (sizeOfAdditionalHeaderData > 0)
        {
            file.AdditionalHeaderData = new byte[sizeOfAdditionalHeaderData];
            int read = reader.Read(file.AdditionalHeaderData);
            if (read != sizeOfAdditionalHeaderData)
            {
                reader.Diagnostics.Error(DiagnosticId.ELF_ERR_IncompleteHeader32Size, $"Unable to read entirely Elf header additional data. Not enough data (size: {file.Layout.SizeOfElfHeader})");
            }
        }
    }

    private unsafe void ReadElfHeader64(ElfReader reader)
    {
        var file = reader.File;
        if (!reader.TryReadData(sizeof(ElfNative.Elf64_Ehdr), out ElfNative.Elf64_Ehdr hdr))
        {
            reader.Diagnostics.Error(DiagnosticId.ELF_ERR_IncompleteHeader64Size, $"Unable to read entirely Elf header. Not enough data (size: {sizeof(ElfNative.Elf64_Ehdr)}) read at offset {reader.Position} from the stream");
            return;
        }

        file.FileType = (ElfFileType)reader.Decode(hdr.e_type);
        file.Arch = new ElfArchEx(reader.Decode(hdr.e_machine));
        file.Version = reader.Decode(hdr.e_version);

        file.EntryPointAddress = reader.Decode(hdr.e_entry);
        file.Layout.SizeOfElfHeader = reader.Decode(hdr.e_ehsize);
        file.Flags = reader.Decode(hdr.e_flags);

        // program headers
        file.Layout.OffsetOfProgramHeaderTable = reader.Decode(hdr.e_phoff);
        file.Layout.SizeOfProgramHeaderEntry = reader.Decode(hdr.e_phentsize);
        file.Layout.ProgramHeaderCount = reader.Decode(hdr.e_phnum);

        // entries for sections
        file.Layout.OffsetOfSectionHeaderTable = reader.Decode(hdr.e_shoff);
        file.Layout.SizeOfSectionHeaderEntry = reader.Decode(hdr.e_shentsize);
        file.Layout.SectionHeaderCount = reader.Decode(hdr.e_shnum);
        file.Layout.SectionStringTableIndex = reader.Decode(hdr.e_shstrndx);


        var sizeOfAdditionalHeaderData = file.Layout.SizeOfElfHeader - sizeof(ElfNative.Elf64_Ehdr);
        if (sizeOfAdditionalHeaderData < 0)
        {
            reader.Diagnostics.Error(DiagnosticId.ELF_ERR_InvalidElfHeaderSize, $"Invalid size of Elf header [{file.Layout.SizeOfElfHeader}] < {sizeof(ElfNative.Elf64_Ehdr)}");
            return;
        }

        // Read any additional data
        if (sizeOfAdditionalHeaderData > 0)
        {
            file.AdditionalHeaderData = new byte[sizeOfAdditionalHeaderData];
            int read = reader.Read(file.AdditionalHeaderData);
            if (read != sizeOfAdditionalHeaderData)
            {
                reader.Diagnostics.Error(DiagnosticId.ELF_ERR_IncompleteHeader64Size, $"Unable to read entirely Elf header additional data. Not enough data (size: {file.Layout.SizeOfElfHeader})");
            }
        }
    }
    
    private void WriteHeader(ElfWriter writer)
    {
        var file = writer.File;
        if (file.FileClass == ElfFileClass.Is32)
        {
            WriteSectionHeader32(writer);
        }
        else
        {
            WriteSectionHeader64(writer);
        }

        if (file.AdditionalHeaderData.Length > 0)
        {
            writer.Write(file.AdditionalHeaderData);
        }
    }

    private unsafe void WriteSectionHeader32(ElfWriter writer)
    {
        var file = writer.File;
        var hdr = new ElfNative.Elf32_Ehdr();
        file.CopyIdentTo(new Span<byte>(hdr.e_ident, ElfNative.EI_NIDENT));

        writer.Encode(out hdr.e_type, (ushort)file.FileType);
        writer.Encode(out hdr.e_machine, (ushort)file.Arch.Value);
        writer.Encode(out hdr.e_version, ElfNative.EV_CURRENT);
        writer.Encode(out hdr.e_entry, (uint)file.EntryPointAddress);
        writer.Encode(out hdr.e_ehsize, file.Layout.SizeOfElfHeader);
        writer.Encode(out hdr.e_flags, (uint)file.Flags);

        // program headers
        writer.Encode(out hdr.e_phoff, (uint)file.Layout.OffsetOfProgramHeaderTable);
        writer.Encode(out hdr.e_phentsize, file.Layout.SizeOfProgramHeaderEntry);
        writer.Encode(out hdr.e_phnum, (ushort)file.Segments.Count);

        // entries for sections
        writer.Encode(out hdr.e_shoff, (uint)file.Layout.OffsetOfSectionHeaderTable);
        writer.Encode(out hdr.e_shentsize, file.Layout.SizeOfSectionHeaderEntry);
        writer.Encode(out hdr.e_shnum, file.Sections.Count >= ElfNative.SHN_LORESERVE ? (ushort)0 : (ushort)file.Sections.Count);
        uint shstrSectionIndex = (uint)(file.SectionHeaderStringTable?.SectionIndex ?? 0);
        writer.Encode(out hdr.e_shstrndx, shstrSectionIndex >= ElfNative.SHN_LORESERVE ? (ushort)ElfNative.SHN_XINDEX : (ushort)shstrSectionIndex);

        writer.Write(hdr);
    }

    private unsafe void WriteSectionHeader64(ElfWriter writer)
    {
        var file = writer.File;
        var hdr = new ElfNative.Elf64_Ehdr();
        file.CopyIdentTo(new Span<byte>(hdr.e_ident, ElfNative.EI_NIDENT));

        writer.Encode(out hdr.e_type, (ushort)file.FileType);
        writer.Encode(out hdr.e_machine, (ushort)file.Arch.Value);
        writer.Encode(out hdr.e_version, ElfNative.EV_CURRENT);
        writer.Encode(out hdr.e_entry, file.EntryPointAddress);
        writer.Encode(out hdr.e_ehsize, file.Layout.SizeOfElfHeader);
        writer.Encode(out hdr.e_flags, (uint)file.Flags);

        // program headers
        writer.Encode(out hdr.e_phoff, file.Layout.OffsetOfProgramHeaderTable);
        writer.Encode(out hdr.e_phentsize, file.Layout.SizeOfProgramHeaderEntry);
        writer.Encode(out hdr.e_phnum, (ushort)file.Segments.Count);

        // entries for sections
        writer.Encode(out hdr.e_shoff, file.Layout.OffsetOfSectionHeaderTable);
        writer.Encode(out hdr.e_shentsize, file.Layout.SizeOfSectionHeaderEntry);
        writer.Encode(out hdr.e_shnum, file.Sections.Count >= ElfNative.SHN_LORESERVE ? (ushort)0 : (ushort)file.Sections.Count);
        uint shstrSectionIndex = (uint)(file.SectionHeaderStringTable?.SectionIndex ?? 0);
        writer.Encode(out hdr.e_shstrndx, shstrSectionIndex >= ElfNative.SHN_LORESERVE ? (ushort)ElfNative.SHN_XINDEX : (ushort)shstrSectionIndex);

        writer.Write(hdr);
    }
}