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

        internal void Read(DwarfReaderWriter reader)
        {
            DebugStringTable?.Read(reader);
            DebugLineSection?.Read(reader);
            DebugAddressRangeTable?.Read(reader);
            DebugInfoSection?.Read(reader);
        }

        public void Write(DwarfInputOutputContext outputContext)
        {
            if (outputContext == null) throw new ArgumentNullException(nameof(outputContext));

            var diagnostics = new DiagnosticBag();

            // Verify
            DebugLineSection?.Verify(diagnostics);
            DebugAddressRangeTable?.Verify(diagnostics);
            DebugInfoSection?.Verify(diagnostics);

            if (!diagnostics.HasErrors)
            {
                DebugLineSection?.TryUpdateLayout(diagnostics);
                DebugAddressRangeTable?.TryUpdateLayout(diagnostics);
                DebugInfoSection?.TryUpdateLayout(diagnostics);
            }

            if (diagnostics.HasErrors)
            {
                throw new ObjectFileException("Unexpected errors while verifying and updating the layout", diagnostics);
            }

            var writer = new DwarfReaderWriter(outputContext, diagnostics);
            Write(writer);
        }

        public static DwarfFile Read(DwarfInputOutputContext inputContext)
        {
            if (inputContext == null) throw new ArgumentNullException(nameof(inputContext));

            var reader = new DwarfReaderWriter(inputContext, new DiagnosticBag());
            var dwarf = new DwarfFile();
            dwarf.Read(reader);
            return dwarf;
        }

        public static DwarfFile ReadFromElf(ElfObjectFile elf)
        {
            var readerContext = DwarfInputOutputContext.FromElf(elf);
            return Read(readerContext);
        }

        private void Write(DwarfReaderWriter writer)
        {
            DebugStringTable?.Write(writer);
            DebugLineSection?.Write(writer);
            DebugAddressRangeTable?.Write(writer);
            DebugInfoSection?.Write(writer);
        }
    }
}