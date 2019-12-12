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
        private DwarfDebugAbbrevTable _debugAbbrevTable;
        private DwarfDebugStringTable _debugStringTable;
        private DwarfDebugLineSection _debugLineSection;
        private DwarfDebugInfoSection _debugInfoSection;
        private DwarfDebugAddressRangeTable _debugAddressRangeTable;

        public DwarfFile()
        {
            DebugAbbrevTable = new DwarfDebugAbbrevTable();
            DebugStringTable = new DwarfDebugStringTable();
            DebugLineSection = new DwarfDebugLineSection();
            DebugInfoSection = new DwarfDebugInfoSection();
            DebugAddressRangeTable = new DwarfDebugAddressRangeTable();
        }

        public DwarfDebugAbbrevTable DebugAbbrevTable
        {
            get => _debugAbbrevTable;
            set => AttachChild<DwarfContainer, DwarfDebugAbbrevTable>(this, value, ref _debugAbbrevTable);
        }
        
        public DwarfDebugStringTable DebugStringTable
        {
            get => _debugStringTable;
            set => AttachChild<DwarfContainer, DwarfDebugStringTable>(this, value, ref _debugStringTable);
        }

        public DwarfDebugLineSection DebugLineSection
        {
            get => _debugLineSection;
            set => AttachChild<DwarfContainer, DwarfDebugLineSection>(this, value, ref _debugLineSection);
        }

        public DwarfDebugAddressRangeTable DebugAddressRangeTable
        {
            get => _debugAddressRangeTable;
            set => AttachChild<DwarfContainer, DwarfDebugAddressRangeTable>(this, value, ref _debugAddressRangeTable);
        }

        public DwarfDebugInfoSection DebugInfoSection
        {
            get => _debugInfoSection;
            set => AttachChild<DwarfContainer, DwarfDebugInfoSection>(this, value, ref _debugInfoSection);
        }

        internal void Read(DwarfReader reader)
        {
            DebugStringTable?.Read(reader);
            DebugLineSection?.Read(reader);
            DebugAddressRangeTable?.Read(reader);
            DebugInfoSection?.Read(reader, reader.Context.DebugInfoStream, DwarfUnitKind.Compile);
        }

        public void Write(DwarfWriterContext writerContext)
        {
            if (writerContext == null) throw new ArgumentNullException(nameof(writerContext));

            var diagnostics = new DiagnosticBag();

            // Verify
            DebugLineSection?.Verify(diagnostics);
            DebugAddressRangeTable?.Verify(diagnostics);
            DebugInfoSection?.Verify(diagnostics);

            // Update layout
            if (!diagnostics.HasErrors)
            {
                DebugLineSection?.TryUpdateLayout(diagnostics);
                DebugAddressRangeTable?.TryUpdateLayout(diagnostics);
            }
            CheckErrors(diagnostics);

            // Reset the abbreviation table
            // TODO: Make this configurable via the DwarfWriterContext
            DebugAbbrevTable?.Reset();

            var writer = new DwarfWriter(writerContext, diagnostics);
            writer.UpdateLayout(diagnostics, DebugInfoSection);
            CheckErrors(diagnostics);

            // Update the abbrev table right after we have computed the entire layout of this 
            DebugAbbrevTable?.TryUpdateLayout(diagnostics);

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
            DebugAbbrevTable?.Write(writer);
            DebugStringTable?.Write(writer);
            DebugLineSection?.Write(writer);
            DebugAddressRangeTable?.Write(writer);
            DebugInfoSection?.Write(writer, writer.Context.DebugInfoStream.Stream);
        }
    }
}