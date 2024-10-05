// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using LibObjectFile.Diagnostics;

namespace LibObjectFile.Elf;

partial class ElfFile
{
    public override void Read(ElfReader reader)
    {
        if (FileClass == ElfFileClass.None)
        {
            reader.Diagnostics.Error(DiagnosticId.ELF_ERR_InvalidHeaderFileClassNone, "Cannot read an ELF Class = None");
        }

        // Read the ELF header
        ReadElfHeader(reader);

        // The program header table is optional
        if (Layout.OffsetOfProgramHeaderTable != 0)
        {
            ElfProgramHeaderTable table = FileClass == ElfFileClass.Is32 ? new ElfProgramHeaderTable32() : new ElfProgramHeaderTable64();
            table.Position = Layout.OffsetOfProgramHeaderTable;
            Content.Add(table);
            table.Read(reader);
        }

        // The section header table is optional
        if (Layout.OffsetOfSectionHeaderTable != 0)
        {
            ElfSectionHeaderTable table = FileClass == ElfFileClass.Is32 ? new ElfSectionHeaderTable32() : new ElfSectionHeaderTable64();
            table.Position = Layout.OffsetOfSectionHeaderTable;
            Content.Add(table);
            table.Read(reader);
        }

        VerifyAndFixProgramHeadersAndSections(reader);
    }

    private void ReadElfHeader(ElfReader reader)
    {
        if (FileClass == ElfFileClass.Is32)
        {
            ReadElfHeader32(reader);
        }
        else
        {
            ReadElfHeader64(reader);
        }

        Debug.Assert(reader.Position == Layout.SizeOfElfHeader);

        //if (_sectionHeaderCount >= ElfNative.SHN_LORESERVE)
        //{
        //    Diagnostics.Error(DiagnosticId.ELF_ERR_InvalidSectionHeaderCount, $"Invalid number `{_sectionHeaderCount}` of section headers found from Elf Header. Must be < {ElfNative.SHN_LORESERVE}");
        //}
    }

    private unsafe void ReadElfHeader32(ElfReader reader)
    {
        if (!reader.TryReadData(sizeof(ElfNative.Elf32_Ehdr), out ElfNative.Elf32_Ehdr hdr))
        {
            reader.Diagnostics.Error(DiagnosticId.ELF_ERR_IncompleteHeader32Size, $"Unable to read entirely Elf header. Not enough data (size: {sizeof(ElfNative.Elf32_Ehdr)}) read at offset {reader.Position} from the stream");
            return;
        }

        FileType = (ElfFileType)reader.Decode(hdr.e_type);
        Arch = new ElfArchEx(reader.Decode(hdr.e_machine));
        Version = reader.Decode(hdr.e_version);

        EntryPointAddress = reader.Decode(hdr.e_entry);
        Layout.SizeOfElfHeader = reader.Decode(hdr.e_ehsize);
        Flags = reader.Decode(hdr.e_flags);

        // program headers
        Layout.OffsetOfProgramHeaderTable = reader.Decode(hdr.e_phoff);
        Layout.SizeOfProgramHeaderEntry = reader.Decode(hdr.e_phentsize);
        Layout.ProgramHeaderCount = reader.Decode(hdr.e_phnum);

        // entries for sections
        Layout.OffsetOfSectionHeaderTable = reader.Decode(hdr.e_shoff);
        Layout.SizeOfSectionHeaderEntry = reader.Decode(hdr.e_shentsize);
        Layout.SectionHeaderCount = reader.Decode(hdr.e_shnum);
        Layout.SectionStringTableIndex = reader.Decode(hdr.e_shstrndx);

        var sizeOfAdditionalHeaderData = Layout.SizeOfElfHeader - sizeof(ElfNative.Elf32_Ehdr);
        if (sizeOfAdditionalHeaderData < 0)
        {
            reader.Diagnostics.Error(DiagnosticId.ELF_ERR_InvalidElfHeaderSize, $"Invalid size of Elf header [{Layout.SizeOfElfHeader}] < {sizeof(ElfNative.Elf32_Ehdr)}");
            return;
        }

        // Read any additional data
        if (sizeOfAdditionalHeaderData > 0)
        {
            AdditionalHeaderData = new byte[sizeOfAdditionalHeaderData];
            int read = reader.Read(AdditionalHeaderData);
            if (read != sizeOfAdditionalHeaderData)
            {
                reader.Diagnostics.Error(DiagnosticId.ELF_ERR_IncompleteHeader32Size, $"Unable to read entirely Elf header additional data. Not enough data (size: {Layout.SizeOfElfHeader})");
            }
        }
    }

    private unsafe void ReadElfHeader64(ElfReader reader)
    {
        if (!reader.TryReadData(sizeof(ElfNative.Elf64_Ehdr), out ElfNative.Elf64_Ehdr hdr))
        {
            reader.Diagnostics.Error(DiagnosticId.ELF_ERR_IncompleteHeader64Size, $"Unable to read entirely Elf header. Not enough data (size: {sizeof(ElfNative.Elf64_Ehdr)}) read at offset {reader.Position} from the stream");
            return;
        }

        FileType = (ElfFileType)reader.Decode(hdr.e_type);
        Arch = new ElfArchEx(reader.Decode(hdr.e_machine));
        Version = reader.Decode(hdr.e_version);

        EntryPointAddress = reader.Decode(hdr.e_entry);
        Layout.SizeOfElfHeader = reader.Decode(hdr.e_ehsize);
        Flags = reader.Decode(hdr.e_flags);

        // program headers
        Layout.OffsetOfProgramHeaderTable = reader.Decode(hdr.e_phoff);
        Layout.SizeOfProgramHeaderEntry = reader.Decode(hdr.e_phentsize);
        Layout.ProgramHeaderCount = reader.Decode(hdr.e_phnum);

        // entries for sections
        Layout.OffsetOfSectionHeaderTable = reader.Decode(hdr.e_shoff);
        Layout.SizeOfSectionHeaderEntry = reader.Decode(hdr.e_shentsize);
        Layout.SectionHeaderCount = reader.Decode(hdr.e_shnum);
        Layout.SectionStringTableIndex = reader.Decode(hdr.e_shstrndx);


        var sizeOfAdditionalHeaderData = Layout.SizeOfElfHeader - sizeof(ElfNative.Elf64_Ehdr);
        if (sizeOfAdditionalHeaderData < 0)
        {
            reader.Diagnostics.Error(DiagnosticId.ELF_ERR_InvalidElfHeaderSize, $"Invalid size of Elf header [{Layout.SizeOfElfHeader}] < {sizeof(ElfNative.Elf64_Ehdr)}");
            return;
        }

        // Read any additional data
        if (sizeOfAdditionalHeaderData > 0)
        {
            AdditionalHeaderData = new byte[sizeOfAdditionalHeaderData];
            int read = reader.Read(AdditionalHeaderData);
            if (read != sizeOfAdditionalHeaderData)
            {
                reader.Diagnostics.Error(DiagnosticId.ELF_ERR_IncompleteHeader64Size, $"Unable to read entirely Elf header additional data. Not enough data (size: {Layout.SizeOfElfHeader})");
            }
        }
    }
    private void VerifyAndFixProgramHeadersAndSections(ElfReader reader)
    {
        var context = new ElfVisitorContext(this, reader.Diagnostics);

        // Read the section header string table before reading the sections
        if (SectionHeaderStringTable is not null)
        {
            SectionHeaderStringTable.Read(reader);
        }

        for (var i = 0; i < Sections.Count; i++)
        {
            var section = Sections[i];
            section.SectionOrder = (uint)i;

            if (section is ElfNullSection) continue;

            // Resolve the name of the section
            if (SectionHeaderStringTable != null && SectionHeaderStringTable.TryGetString(section.Name.Index, out var sectionName))
            {
                section.Name = new(sectionName, section.Name.Index);
            }
            else
            {
                if (SectionHeaderStringTable == null)
                {
                    reader.Diagnostics.Warning(DiagnosticId.ELF_ERR_InvalidStringIndexMissingStringHeaderTable, $"Unable to resolve string index [{section.Name.Index}] for section [{section.Index}] as section header string table does not exist");
                }
                else
                {
                    reader.Diagnostics.Warning(DiagnosticId.ELF_ERR_InvalidStringIndex, $"Unable to resolve string index [{section.Name.Index}] for section [{section.Index}] from section header string table");
                }
            }

            // Connect section Link instance
            section.Link = ResolveLink(section.Link, $"Invalid section Link [{{0}}] for section [{i}]");

            // Connect section Info instance
            if (section.Type != ElfSectionType.DynamicLinkerSymbolTable && section.Type != ElfSectionType.SymbolTable && (section.Flags & ElfSectionFlags.InfoLink) != 0)
            {
                section.Info = ResolveLink(section.Info, $"Invalid section Info [{{0}}] for section [{i}]");
            }

            if ((i == 0 && _isFirstSectionValidNull) || i == _sectionStringTableIndex)
            {
                continue;
            }

            if (section.HasContent)
            {
                section.Read(reader);
            }
        }

        foreach (var section in Sections)
        {
            section.AfterReadInternal(reader);
        }

        // Order the content per position
        var content = CollectionsMarshal.AsSpan(Content.UnsafeList);
        content.Sort(static (left, right) => left.Position.CompareTo(right.Position));
        for (int i = 0; i < content.Length; i++)
        {
            content[i].Index = i;
        }


        // Create missing content















        // Link segments to sections if we have an exact match.
        // otherwise record any segments that are not bound to a section.

        foreach (var segment in Segments)
        {
            if (segment.Size == 0) continue;

            var segmentEndOffset = segment.Position + segment.Size - 1;
            foreach (var section in orderedSections)
            {
                if (section.Size == 0 || !section.HasContent) continue;

                var sectionEndOffset = section.Position + section.Size - 1;
                if (segment.Position == section.Position && segmentEndOffset == sectionEndOffset)
                {
                    // Single case: segment == section
                    // If we found a section, we will bind the program header to this section
                    // and switch the offset calculation to auto
                    segment.Range = section;
                    segment.OffsetCalculationMode = ElfOffsetCalculationMode.Auto;
                    break;
                }
            }

            if (segment.Range.IsEmpty)
            {
                var offset = segment.Position;

                // If a segment offset is set to 0, we need to take into
                // account the fact that the Elf header is already being handled
                // so we should not try to create a shadow section for it
                if (offset < Layout.SizeOfElfHeader)
                {
                    offset = Layout.SizeOfElfHeader;
                }

                // Create parts for the segment
                fileParts.CreateParts(offset, segmentEndOffset);
                hasShadowSections = true;
            }
        }

        // If the previous loop has created ElfFilePart, we have to 
        // create ElfCustomShadowSection and update the ElfSegment.Range
        if (hasShadowSections)
        {
            int shadowCount = 0;
            // If we have sections and the first section is NULL valid, we can start inserting
            // shadow sections at index 1 (after null section), otherwise we can insert shadow
            // sections before.
            uint previousSectionIndex = _isFirstSectionValidNull ? 1U : 0U;

            // Create ElfCustomShadowSection for any parts in the file
            // that are referenced by a segment but doesn't have a section
            for (var i = 0; i < fileParts.Count; i++)
            {
                var part = fileParts[i];
                if (part.Section == null)
                {
                    var shadowSection = new ElfStreamContentData()
                    {
                        Name = ".shadow." + shadowCount,
                        Position = part.StartOffset,
                        Size = part.EndOffset - part.StartOffset + 1
                    };
                    shadowCount++;

                    Stream.Position = (long)shadowSection.Position;
                    shadowSection.Read(this);

                    // Insert the shadow section with this order
                    shadowSection.StreamIndex = previousSectionIndex;
                    for (int j = (int)previousSectionIndex; j < orderedSections.Count; j++)
                    {
                        var otherSection = orderedSections[j];
                        otherSection.StreamIndex++;
                    }
                    // Update ordered sections
                    orderedSections.Insert((int)previousSectionIndex, shadowSection);
                    AddSection(shadowSection);

                    fileParts[i] = new ElfFilePart(shadowSection);
                }
                else
                {
                    previousSectionIndex = part.Section.StreamIndex + 1;
                }
            }

            // Update all segment Ranges
            foreach (var segment in Segments)
            {
                if (segment.Size == 0) continue;
                if (!segment.Range.IsEmpty) continue;

                var segmentEndOffset = segment.Position + segment.Size - 1;
                for (var i = 0; i < orderedSections.Count; i++)
                {
                    var section = orderedSections[i];
                    if (section.Size == 0 || !section.HasContent) continue;

                    var sectionEndOffset = section.Position + section.Size - 1;
                    if (segment.Position >= section.Position && segment.Position <= sectionEndOffset)
                    {
                        ElfSection beginSection = section;
                        ElfSection? endSection = null;
                        for (int j = i; j < orderedSections.Count; j++)
                        {
                            var nextSection = orderedSections[j];
                            if (nextSection.Size == 0 || !nextSection.HasContent) continue;

                            sectionEndOffset = nextSection.Position + nextSection.Size - 1;

                            if (segmentEndOffset >= nextSection.Position && segmentEndOffset <= sectionEndOffset)
                            {
                                endSection = nextSection;
                                break;
                            }
                        }

                        if (endSection == null)
                        {
                            // TODO: is it a throw/assert or a log?
                            Diagnostics.Error(DiagnosticId.ELF_ERR_InvalidSegmentRange, $"Invalid range for {segment}. The range is set to empty");
                        }
                        else
                        {
                            segment.Range = new ElfSegmentRange(beginSection, segment.Position - beginSection.Position, endSection, (long)(segmentEndOffset - endSection.Position));
                        }

                        segment.OffsetCalculationMode = ElfOffsetCalculationMode.Auto;
                        break;
                    }
                }
            }
        }
    }
}