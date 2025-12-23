// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using LibObjectFile.Collections;
using LibObjectFile.Diagnostics;
using System;
using System.Runtime.InteropServices;

namespace LibObjectFile.Elf;

/// <summary>
/// The section header table.
/// </summary>
public sealed partial class ElfSectionHeaderTable : ElfContentData
{
    private bool _is32;

    public unsafe ElfSectionHeaderTable()
    {
    }

    protected override void UpdateLayoutCore(ElfVisitorContext context)
    {
        Size = (ulong)(Parent!.Sections.Count * EntrySizeOf);
    }

    public override unsafe void Read(ElfReader reader)
    {
        reader.Position = Position;

        var layout = reader.File.Layout;
        using var tempSpan = TempSpan<byte>.Create((int)layout.SizeOfSectionHeaderEntry, out var span);
        if (layout.SizeOfSectionHeaderEntry < EntrySizeOf)
        {
            reader.Diagnostics.Error(DiagnosticId.ELF_ERR_InvalidProgramHeaderSize, $"Invalid program header size [{layout.SizeOfSectionHeaderEntry}] for {reader.File.FileClass} bit. Expecting at least [{EntrySizeOf}]");
            return;
        }

        int i = 0;
        var sectionHeaderCount = reader.File.Layout.SectionHeaderCount;
        if (sectionHeaderCount == 0)
        {
            var nullSection = ReadSectionTableEntryWrap(reader, 0, span);

            if (nullSection.Size >= uint.MaxValue)
            {
                reader.Diagnostics.Error(DiagnosticId.ELF_ERR_InvalidSectionHeaderCount, $"Extended section count [{nullSection.Size}] exceeds {uint.MaxValue}");
                return;
            }

            reader.File.Layout.SectionHeaderCount = sectionHeaderCount = (uint)nullSection.Size;

            if (reader.File.Layout.SectionStringTableIndex == ElfNative.SHN_XINDEX)
            {
                reader.File.Layout.SectionStringTableIndex = (uint)nullSection.Link.SpecialIndex;
            }

            nullSection.Size = 0;
            nullSection.Link = default;

            reader.File.Content.Add(nullSection);
            i = 1;
        }

        for (; i < sectionHeaderCount; i++)
        {
            var section = ReadSectionTableEntryWrap(reader, (uint)i, span);
            reader.File.Content.Add(section);
        }

        UpdateLayoutCore(reader);
    }

    private unsafe ElfSection ReadSectionTableEntryWrap(ElfReader reader, uint sectionIndex, Span<byte> buffer)
    {
        int read = reader.Read(buffer);
        if (read != buffer.Length)
        {
            reader.Diagnostics.Error(DiagnosticId.ELF_ERR_IncompleteSessionHeaderSize,
                $"Unable to read entirely section header [{sectionIndex}]. Not enough data (size: {reader.File.Layout.SizeOfProgramHeaderEntry}) read at offset {reader.Position} from the stream");
        }

        var section = _is32
            ? DecodeSectionTableEntry32(reader, sectionIndex, buffer)
            : DecodeSectionTableEntry64(reader, sectionIndex, buffer);

        if (sectionIndex == 0 && section.Type != ElfSectionType.Null)
        {
            reader.Diagnostics.Error(DiagnosticId.ELF_ERR_InvalidFirstSectionExpectingUndefined, $"Invalid Section [0] {section.Type}. Expecting {ElfSectionType.Null}");
        }

        return section;
    }

    public override unsafe void Write(ElfWriter writer)
    {
        using var tempSpan = TempSpan<byte>.Create((int)EntrySizeOf, out var span);

        var sections = Parent!.Sections;
        delegate*<ElfWriter, ElfSection, Span<byte>, void> encodeSectionTableEntry = _is32 ? &EncodeSectionTableEntry32 : &EncodeSectionTableEntry64;
        for (int i = 0; i < sections.Count; i++)
        {
            var section = sections[i];
            encodeSectionTableEntry(writer, section, span);
            writer.Write(span);
        }
    }

    protected override void ValidateParent(ObjectElement parent)
    {
        base.ValidateParent(parent);
        var elf = (ElfFile)parent;
        _is32 = elf.FileClass == ElfFileClass.Is32;
        FileAlignment = _is32 ? 4u : 8u;
    }

    private static ElfSection CreateElfSection(ElfReader reader, uint sectionIndex, ElfSectionType sectionType)
    {
        // Try to offload to a delegate before creating supported sections.
        var section = reader.Options.TryCreateSection?.Invoke(sectionType, reader.Diagnostics);
        if (section != null)
        {
            return section;
        }

        return sectionType switch
        {
            ElfSectionType.Null => new ElfNullSection(),
            ElfSectionType.DynamicLinkerSymbolTable or ElfSectionType.SymbolTable => new ElfSymbolTable(sectionType == ElfSectionType.DynamicLinkerSymbolTable),
            ElfSectionType.StringTable => sectionIndex == reader.File.Layout.SectionStringTableIndex ? new ElfSectionHeaderStringTable(true) : new ElfStringTable(true),
            ElfSectionType.Relocation or ElfSectionType.RelocationAddends => new ElfRelocationTable(sectionType == ElfSectionType.RelocationAddends),
            ElfSectionType.Note => new ElfNoteTable(),
            ElfSectionType.SymbolTableSectionHeaderIndices => new ElfSymbolTableSectionHeaderIndices(),
            ElfSectionType.NoBits => new ElfNoBitsSection(),
            ElfSectionType.DynamicLinking => new ElfDynamicLinkingTable(),
            _ => new ElfStreamSection(sectionType)
        };
    }

    private unsafe int EntrySizeOf => _is32 ? sizeof(ElfNative.Elf32_Shdr) : sizeof(ElfNative.Elf64_Shdr);

    private static unsafe ElfSection DecodeSectionTableEntry32(ElfReader reader, uint sectionIndex, Span<byte> buffer)
    {
        ref var rawSection = ref MemoryMarshal.AsRef<ElfNative.Elf32_Shdr>(buffer);

        var additionalData = Array.Empty<byte>();
        if (buffer.Length > sizeof(ElfNative.Elf32_Shdr))
        {
            additionalData = buffer.Slice(sizeof(ElfNative.Elf32_Shdr)).ToArray();
        }

        var sectionType = (ElfSectionType)reader.Decode(rawSection.sh_type);
        var section = CreateElfSection(reader, sectionIndex, sectionType);

        section.Name = new ElfString(reader.Decode(rawSection.sh_name));
        section.Flags = (ElfSectionFlags)reader.Decode(rawSection.sh_flags);
        section.VirtualAddress = reader.Decode(rawSection.sh_addr);
        section.Position = reader.Decode(rawSection.sh_offset);
        section.VirtualAddressAlignment = reader.Decode(rawSection.sh_addralign);
        section.Link = new ElfSectionLink((int)reader.Decode(rawSection.sh_link));
        section.Info = new ElfSectionLink((int)reader.Decode(rawSection.sh_info));
        section.Size = reader.Decode(rawSection.sh_size);
        section.InitializeEntrySizeFromRead(reader.Diagnostics, reader.Decode(rawSection.sh_entsize), true);

        return section;
    }

    private static void EncodeSectionTableEntry32(ElfWriter writer, ElfSection section, Span<byte> buffer)
    {
        ref var rawSection = ref MemoryMarshal.AsRef<ElfNative.Elf32_Shdr>(buffer);
        writer.Encode(out rawSection.sh_name, section.Name.Index);
        writer.Encode(out rawSection.sh_type, (uint)section.Type);
        writer.Encode(out rawSection.sh_flags, (uint)section.Flags);
        writer.Encode(out rawSection.sh_addr, (uint)section.VirtualAddress);
        writer.Encode(out rawSection.sh_offset, (uint)section.Position);
        if (section.SectionIndex == 0 && writer.File.Sections.Count >= ElfNative.SHN_LORESERVE)
        {
            writer.Encode(out rawSection.sh_size, (uint)writer.File.Sections.Count);
            var shstrSectionIndex = (uint)(writer.File.SectionHeaderStringTable?.SectionIndex ?? 0);
            writer.Encode(out rawSection.sh_link, shstrSectionIndex >= ElfNative.SHN_LORESERVE ? shstrSectionIndex : 0);
        }
        else
        {
            writer.Encode(out rawSection.sh_size, (uint)section.Size);
            writer.Encode(out rawSection.sh_link, (uint)section.Link.GetIndex());
        }
        writer.Encode(out rawSection.sh_info, (uint)section.Info.GetIndex());
        writer.Encode(out rawSection.sh_addralign, (uint)section.VirtualAddressAlignment);
        writer.Encode(out rawSection.sh_entsize, (uint)section.TableEntrySize);
    }

    private static unsafe ElfSection DecodeSectionTableEntry64(ElfReader reader, uint sectionIndex, Span<byte> buffer)
    {
        ref var rawSection = ref MemoryMarshal.AsRef<ElfNative.Elf64_Shdr>(buffer);

        var sectionType = (ElfSectionType)reader.Decode(rawSection.sh_type);
        var section = CreateElfSection(reader, sectionIndex, sectionType);

        section.Name = new ElfString(reader.Decode(rawSection.sh_name));
        section.Flags = (ElfSectionFlags)reader.Decode(rawSection.sh_flags);
        section.VirtualAddress = reader.Decode(rawSection.sh_addr);
        section.Position = reader.Decode(rawSection.sh_offset);
        section.VirtualAddressAlignment = reader.Decode(rawSection.sh_addralign);
        section.Link = new ElfSectionLink((int)reader.Decode(rawSection.sh_link));
        section.Info = new ElfSectionLink((int)reader.Decode(rawSection.sh_info));
        section.Size = reader.Decode(rawSection.sh_size);
        section.InitializeEntrySizeFromRead(reader.Diagnostics, reader.Decode(rawSection.sh_entsize), false);

        return section;
    }

    private static unsafe void EncodeSectionTableEntry64(ElfWriter writer, ElfSection section, Span<byte> buffer)
    {
        ref var rawSection = ref MemoryMarshal.AsRef<ElfNative.Elf64_Shdr>(buffer);
        writer.Encode(out rawSection.sh_name, section.Name.Index);
        writer.Encode(out rawSection.sh_type, (uint)section.Type);
        writer.Encode(out rawSection.sh_flags, (uint)section.Flags);
        writer.Encode(out rawSection.sh_addr, (uint)section.VirtualAddress);
        writer.Encode(out rawSection.sh_offset, (uint)section.Position);
        if (section.SectionIndex == 0 && writer.File.Sections.Count >= ElfNative.SHN_LORESERVE)
        {
            writer.Encode(out rawSection.sh_size, (uint)writer.File.Sections.Count);
            var shstrSectionIndex = (uint)(writer.File.SectionHeaderStringTable?.SectionIndex ?? 0);
            writer.Encode(out rawSection.sh_link, shstrSectionIndex >= ElfNative.SHN_LORESERVE ? shstrSectionIndex : 0);
        }
        else
        {
            writer.Encode(out rawSection.sh_size, (uint)section.Size);
            writer.Encode(out rawSection.sh_link, (uint)section.Link.GetIndex());
        }
        writer.Encode(out rawSection.sh_info, (uint)section.Info.GetIndex());
        writer.Encode(out rawSection.sh_addralign, (uint)section.VirtualAddressAlignment);
        writer.Encode(out rawSection.sh_entsize, (uint)section.TableEntrySize);
    }
}