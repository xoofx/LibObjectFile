// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;

namespace LibObjectFile.Dwarf
{
    public class DwarfDebugStringTable : DwarfSection
    {
        public Stream Stream { get; set; }
    }

    public class DwarfDebugAbbrevTable : DwarfSection
    {

        public Stream Stream { get; set; }


        public DwarfAbbreviation Read(ulong abbreviationOffset)
        {
            var reader = new DwarfReaderWriter(Stream, true);
            return DwarfAbbreviation.Read(reader, abbreviationOffset);
        }
    }

    public readonly struct DwarfDebugInfoReadContext
    {
        public DwarfDebugInfoReadContext(DwarfDebugStringTable stringTable, DwarfDebugAbbrevTable abbreviationTable)
        {
            StringTable = stringTable;
            AbbreviationTable = abbreviationTable;
        }
        
        public readonly DwarfDebugStringTable StringTable;

        public readonly DwarfDebugAbbrevTable AbbreviationTable;
    }
    
    public class DwarfDebugInfoSection : DwarfSection
    {
        private readonly List<DwarfUnit> _units;

        public DwarfDebugInfoSection()
        {
            _units = new List<DwarfUnit>();
        }
        
        public IReadOnlyList<DwarfUnit> Units => _units;


        public void AddUnit(DwarfUnit unit)
        {
            _units.Add<DwarfContainer, DwarfUnit>(this, unit);
        }
        
        public static bool TryRead(Stream stream, bool isLittleEndian, in DwarfDebugInfoReadContext context, out DwarfDebugInfoSection debugSection, out DiagnosticBag diagnostics)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            debugSection = new DwarfDebugInfoSection();
            var reader = new DwarfReaderWriter(stream, isLittleEndian);
            diagnostics = reader.Diagnostics;
            return debugSection.TryRead(reader, context);
        }

        private bool TryRead(DwarfReaderWriter reader, in DwarfDebugInfoReadContext context)
        {
            bool result = true;
            while (true)
            {
                if (reader.Offset == reader.Length)
                {
                    break;
                }
                // 7.5 Format of Debugging Information
                // - Each such contribution consists of a compilation unit header

                var startOffset = reader.Offset;
                if (!TryReadCompilationUnitHeader(reader, out var header, out var offsetEndOfUnit))
                {
                    reader.Offset = offsetEndOfUnit;
                    result = false;
                    continue;
                }

                var cu = new DwarfCompilationUnit
                {
                    Offset = (ulong)startOffset,
                    Is64 = reader.Is64Bit,
                    Version = header.version,
                    AddressSize = header.address_size
                };

                var abbreviation = context.AbbreviationTable.Read(header.debug_abbrev_offset);
                
                // Each debugging information entry begins with an unsigned LEB128 number containing the abbreviation code for the entry.
                cu.Root = ReadDIE(reader, cu, context, abbreviation, 0);

                AddUnit(cu);
            }

            return result;
        }

        private DwarfDIE ReadDIE(DwarfReaderWriter reader, DwarfCompilationUnit cu, in DwarfDebugInfoReadContext context, DwarfAbbreviation abbreviation, int level)
        {
            var startDIEOffset = reader.Offset;
            var abbreviationCode = reader.ReadLEB128();

            if (abbreviationCode == 0)
            {
                return null;
            }

            if (!abbreviation.TryFindByCode(abbreviationCode, out var abbreviationItem))
            {
                reader.Diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidData, $"Invalid abbreviation code {abbreviationCode}");
                return null;
            }


            var die = new DwarfDIE
            {
                Offset = startDIEOffset,
                Tag = abbreviationItem.Tag
            };

            Console.WriteLine($" <{level}><{die.Offset:x}> Abbrev Number: {abbreviationCode} ({die.Tag})");

            if (abbreviationItem.Descriptors != null)
            {
                foreach (var descriptor in abbreviationItem.Descriptors)
                {

                    var attribute = new DwarfAttribute();
                    attribute.Offset = reader.Offset;
                    attribute.Name = descriptor.Name;
                    var form = descriptor.Form;
                    var formValue = reader.ReadAttributeFormRawValue(form, new DwarfReadAttributeFormContext(cu.AddressSize, context.StringTable));
                    attribute.Value = new DwarfAttributeValue(formValue);
                    Console.WriteLine($"    <{attribute.Offset:x}>\t<{attribute.Name}\t: {attribute.Value}");

                    die.AddAttribute(attribute);
                }
            }

            if (abbreviationItem.HasChildren)
            {
                while (true)
                {
                    var child = ReadDIE(reader, cu, context, abbreviation, level+1);
                    if (child == null) break;
                    die.AddChild(child);
                }
            }

            return die;
        }

        private bool TryReadCompilationUnitHeader(DwarfReaderWriter reader, out DwarfCompilationUnitHeader header, out ulong offsetEndOfUnit)
        {
            header = new DwarfCompilationUnitHeader();

            // 1. unit_length 
            header.unit_length = reader.ReadUnitLength();

            offsetEndOfUnit = (ulong)reader.Offset + header.unit_length;

            // 2. version (uhalf) 
            header.version = reader.ReadU16();

            if (header.version <= 2 || header.version > 5)
            {
                reader.Diagnostics.Error(DiagnosticId.DWARF_ERR_VersionNotSupported, $"Version {header.version} is not supported");
                return false;
            }
            if (header.version < 5)
            {
                // 3. debug_abbrev_offset (section offset) 
                header.debug_abbrev_offset = reader.ReadNativeUInt();

                // 4. address_size (ubyte) 
                header.address_size = reader.ReadU8();
            }
            else
            {
                // 3. unit_type (ubyte)
                header.unit_type = reader.ReadU8();

                // NOTE: order of address_size/debug_abbrev_offset are different from Dwarf 4

                // 4. address_size (ubyte) 
                header.address_size = reader.ReadU8();

                // 5. debug_abbrev_offset (section offset) 
                header.debug_abbrev_offset = reader.ReadNativeUInt();

            }

            return true;
        }
        
        struct DwarfCompilationUnitHeader
        {
            public ulong unit_length;

            public ushort version;

            public byte unit_type;

            public ulong debug_abbrev_offset;

            public byte address_size;
        }

    }
}