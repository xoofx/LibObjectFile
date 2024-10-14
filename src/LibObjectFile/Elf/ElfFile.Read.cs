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
        var headerContent = (ElfHeaderContent)Content[0];
        headerContent.Read(reader);

        // The program header table is optional
        if (Layout.OffsetOfProgramHeaderTable != 0)
        {
            var  table = new ElfProgramHeaderTable
            {
                Position = Layout.OffsetOfProgramHeaderTable
            };
            Content.Add(table);
            table.Read(reader);
        }

        // The section header table is optional
        if (Layout.OffsetOfSectionHeaderTable != 0)
        {
            var  table = new ElfSectionHeaderTable
            {
                Position = Layout.OffsetOfSectionHeaderTable
            };
            Content.Add(table);
            table.Read(reader);
        }

        VerifyAndFixProgramHeadersAndSections(reader);
    }

  
    private unsafe void VerifyAndFixProgramHeadersAndSections(ElfReader reader)
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
            section.OrderInSectionHeaderTable = (uint)i;

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
            section.Link = reader.ResolveLink(section.Link, $"Invalid section Link [{{0}}] for section [{i}]");

            // Connect section Info instance
            if (section.Type != ElfSectionType.DynamicLinkerSymbolTable && section.Type != ElfSectionType.SymbolTable && (section.Flags & ElfSectionFlags.InfoLink) != 0)
            {
                section.Info = reader.ResolveLink(section.Info, $"Invalid section Info [{{0}}] for section [{i}]");
            }

            if (section != SectionHeaderStringTable && section.HasContent)
            {
                section.Read(reader);
            }
        }

        foreach (var section in Sections)
        {
            section.AfterReadInternal(reader);
        }

        // Order the content per position
        var contentList = Content.UnsafeList;
        contentList.Sort(static (left, right) =>
        {
            if (left.Position == right.Position)
            {
                if (left is ElfSection leftSection && right is ElfSection rightSection)
                {
                    return leftSection.OrderInSectionHeaderTable.CompareTo(rightSection.OrderInSectionHeaderTable);
                }
            }

            return left.Position.CompareTo(right.Position);
        });
        for (int i = 0; i < contentList.Count; i++)
        {
            contentList[i].Index = i;
        }

        // Create missing content
        ulong currentPosition = 0;
        ulong endPosition = (ulong)reader.Stream.Length;

        for (int i = 0; i < contentList.Count; i++)
        {
            var part = contentList[i];
            if (part is ElfSection section && !section.HasContent)
            {
                if (section is ElfNoBitsSection noBitsSection)
                {
                    noBitsSection.PositionOffsetFromPreviousContent = part.Position - contentList[i - 1].Position;
                }
                continue;
            }

            if (part.Position > currentPosition)
            {
                var streamContent = new ElfStreamContentData(true)
                {
                    Position = currentPosition,
                    Size = part.Position - currentPosition
                };
                streamContent.Read(reader);
                Content.Insert(i, streamContent);
                currentPosition = part.Position;
                i++;
            }
            
            currentPosition += part.Size;
        }

        if (currentPosition < endPosition)
        {
            var streamContent = new ElfStreamContentData(true)
            {
                Position = currentPosition,
                Size = endPosition - currentPosition
            };
            streamContent.Read(reader);
            Content.Add(streamContent);
        }

        for (int i = 0; i < contentList.Count; i++)
        {
            contentList[i].Index = i;
        }

        foreach (var segment in Segments)
        {
            if (segment.SizeInMemory == 0) continue;

            var startSegmentPosition = segment.Position;
            var endSegmentPosition = segment.Position + segment.Size;
            ElfContent? startContent = null;
            ElfContent? endContent = null;

            foreach (var content in Content)
            {
                if (content.Contains(startSegmentPosition, 0))
                {
                    startContent = content;
                }

                if (content.Contains(endSegmentPosition, 0))
                {
                    endContent = content;
                    break;
                }
            }

            if (startContent == null || endContent == null)
            {
                reader.Diagnostics.Error(DiagnosticId.ELF_ERR_InvalidSegmentRange, $"Unable to find the range of content for segment [{segment.Index}]");
            }
            else
            {
                segment.Range = new ElfContentRange(startContent, startSegmentPosition - startContent.Position, endContent, endContent.Size - (endSegmentPosition - endContent.Position));
            }
        }
    }
}