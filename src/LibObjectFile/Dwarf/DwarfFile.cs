// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Diagnostics;
using LibObjectFile.Elf;

namespace LibObjectFile.Dwarf
{
    public class DwarfFile : DwarfContainer
    {
        private DwarfAbbreviationTable _abbreviationTable;
        private DwarfStringTable _stringTable;
        private DwarfLineSection _lineSection;
        private DwarfInfoSection _infoSection;
        private DwarfAddressRangeTable _addressRangeTable;

        public DwarfFile()
        {
            AbbreviationTable = new DwarfAbbreviationTable();
            StringTable = new DwarfStringTable();
            LineSection = new DwarfLineSection();
            InfoSection = new DwarfInfoSection();
            AddressRangeTable = new DwarfAddressRangeTable();
        }

        public DwarfAbbreviationTable AbbreviationTable
        {
            get => _abbreviationTable;
            set => AttachChild<DwarfContainer, DwarfAbbreviationTable>(this, value, ref _abbreviationTable);
        }
        
        public DwarfStringTable StringTable
        {
            get => _stringTable;
            set => AttachChild<DwarfContainer, DwarfStringTable>(this, value, ref _stringTable);
        }

        public DwarfLineSection LineSection
        {
            get => _lineSection;
            set => AttachChild<DwarfContainer, DwarfLineSection>(this, value, ref _lineSection);
        }

        public DwarfAddressRangeTable AddressRangeTable
        {
            get => _addressRangeTable;
            set => AttachChild<DwarfContainer, DwarfAddressRangeTable>(this, value, ref _addressRangeTable);
        }

        public DwarfInfoSection InfoSection
        {
            get => _infoSection;
            set => AttachChild<DwarfContainer, DwarfInfoSection>(this, value, ref _infoSection);
        }

        internal void Read(DwarfReader reader)
        {
            StringTable?.Read(reader);
            LineSection?.Read(reader);
            AddressRangeTable?.Read(reader);
            InfoSection?.Read(reader, reader.Context.DebugInfoStream, DwarfUnitKind.Compile);
        }

        public void Write(DwarfWriterContext writerContext)
        {
            if (writerContext == null) throw new ArgumentNullException(nameof(writerContext));

            var diagnostics = new DiagnosticBag();

            // Verify
            LineSection?.Verify(diagnostics);
            AddressRangeTable?.Verify(diagnostics);
            InfoSection?.Verify(diagnostics);

            // Update layout
            if (!diagnostics.HasErrors)
            {
                LineSection?.TryUpdateLayout(diagnostics);
                AddressRangeTable?.TryUpdateLayout(diagnostics);
            }
            CheckErrors(diagnostics);

            // Reset the abbreviation table
            // TODO: Make this configurable via the DwarfWriterContext
            AbbreviationTable?.Reset();

            var writer = new DwarfWriter(writerContext, diagnostics);
            writer.UpdateLayout(diagnostics, InfoSection);
            CheckErrors(diagnostics);

            // Update the abbrev table right after we have computed the entire layout of this 
            AbbreviationTable?.TryUpdateLayout(diagnostics);

            CheckErrors(diagnostics);

            Write(writer);
        }

        private static void CheckErrors(DiagnosticBag diagnostics)
        {
            if (diagnostics.HasErrors)
            {
                throw new ObjectFileException("Unexpected errors while verifying and updating the layout", diagnostics);
            }
        }

        public static DwarfFile Read(DwarfReaderContext readerContext)
        {
            if (readerContext == null) throw new ArgumentNullException(nameof(readerContext));

            var reader = new DwarfReader(readerContext, new DiagnosticBag());
            var dwarf = new DwarfFile();
            dwarf.Read(reader);
            return dwarf;
        }

        public static DwarfFile ReadFromElf(ElfObjectFile elf)
        {
            var readerContext = DwarfReaderContext.FromElf(elf);
            return Read(readerContext);
        }

        private void Write(DwarfWriter writer)
        {
            AbbreviationTable?.Write(writer);
            StringTable?.Write(writer);
            LineSection?.Write(writer);
            AddressRangeTable?.Write(writer);
            InfoSection?.Write(writer, writer.Context.DebugInfoStream.Stream);
        }
    }
}