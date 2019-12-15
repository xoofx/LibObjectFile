// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
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

        public bool IsLittleEndian { get; set; }

        public DwarfAddressSize AddressSize { get; set; }

        public DwarfAbbreviationTable AbbreviationTable
        {
            get => _abbreviationTable;
            set => AttachChild(this, value, ref _abbreviationTable, false);
        }
        
        public DwarfStringTable StringTable
        {
            get => _stringTable;
            set => AttachChild(this, value, ref _stringTable, false);
        }

        public DwarfLineSection LineSection
        {
            get => _lineSection;
            set => AttachChild(this, value, ref _lineSection, false);
        }

        public DwarfAddressRangeTable AddressRangeTable
        {
            get => _addressRangeTable;
            set => AttachChild(this, value, ref _addressRangeTable, false);
        }

        public DwarfInfoSection InfoSection
        {
            get => _infoSection;
            set => AttachChild(this, value, ref _infoSection, false);
        }
        
        protected override void Read(DwarfReader reader)
        {
            throw new NotImplementedException();
        }

        public override void Verify(DiagnosticBag diagnostics)
        {
            base.Verify(diagnostics);

            LineSection.Verify(diagnostics);
            AddressRangeTable.Verify(diagnostics);
            InfoSection.Verify(diagnostics);
            StringTable.Verify(diagnostics);
        }
        
        public void UpdateLayout(DwarfLayoutConfig config, DiagnosticBag diagnostics)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (diagnostics == null) throw new ArgumentNullException(nameof(diagnostics));

            var layoutContext = new DwarfLayoutContext(this, config, diagnostics);

            LineSection.Offset = 0;
            LineSection.UpdateLayoutInternal(layoutContext);
            if (layoutContext.HasErrors)
            {
                return;
            }

            // Reset the abbreviation table
            // TODO: Make this configurable via the DwarfWriterContext
            AbbreviationTable.Offset = 0;
            AbbreviationTable.Reset();

            InfoSection.Offset = 0;
            InfoSection.UpdateLayoutInternal(layoutContext);
            if (layoutContext.HasErrors)
            {
                return;
            }

            // Update AddressRangeTable layout after Info
            AddressRangeTable.Offset = 0;
            AddressRangeTable.UpdateLayoutInternal(layoutContext);
            if (layoutContext.HasErrors)
            {
                return;
            }

            // Update string table right after updating the layout of Info
            StringTable.Offset = 0;
            StringTable.UpdateLayoutInternal(layoutContext);
            if (layoutContext.HasErrors)
            {
                return;
            }

            // Update the abbrev table right after we have computed the entire layout of Info
            AbbreviationTable.Offset = 0;
            AbbreviationTable.UpdateLayoutInternal(layoutContext);
        }

        public void Write(DwarfWriterContext writerContext)
        {
            if (writerContext == null) throw new ArgumentNullException(nameof(writerContext));

            var diagnostics = new DiagnosticBag();

            // Verify correctness
            Verify(diagnostics);
            CheckErrors(diagnostics);

            // Update the layout of all section and tables
            UpdateLayout(writerContext.LayoutConfig, diagnostics);
            CheckErrors(diagnostics);

            // Write all section and stables
            var writer = new DwarfWriter(this, writerContext, diagnostics);
            writer.AddressSize = writerContext.AddressSize;

            writer.Stream = writerContext.DebugAbbrevStream.Stream;
            if (writer.Stream != null)
            {
                AbbreviationTable.WriteInternal(writer);
            }

            writer.Stream = writerContext.DebugStringStream.Stream;
            if (writer.Stream != null)
            {
                StringTable.WriteInternal(writer);
            }

            writer.Stream = writerContext.DebugLineStream.Stream;
            if (writer.Stream != null)
            {
                LineSection.WriteInternal(writer);
            }

            writer.Stream = writerContext.DebugAddressRangeStream.Stream;
            if (writer.Stream != null)
            {
                AddressRangeTable.WriteInternal(writer);
            }

            writer.Stream = writerContext.DebugInfoStream.Stream;
            if (writer.Stream != null)
            {
                InfoSection.WriteInternal(writer);
            }

            CheckErrors(diagnostics);
        }

        public static DwarfFile Read(DwarfReaderContext readerContext)
        {
            if (readerContext == null) throw new ArgumentNullException(nameof(readerContext));

            var dwarf = new DwarfFile()
            {
                IsLittleEndian = readerContext.IsLittleEndian
            };
            var reader = new DwarfReader(readerContext, dwarf, new DiagnosticBag());

            reader.Log = readerContext.DebugAbbrevStream.Printer;
            reader.Stream = readerContext.DebugAbbrevStream.Stream;
            if (reader.Stream != null)
            {
                reader.Log = readerContext.DebugAbbrevStream.Printer;
                dwarf.AbbreviationTable.ReadInternal(reader);
            }

            reader.Log = readerContext.DebugStringStream.Printer;
            reader.Stream = readerContext.DebugStringStream.Stream;
            if (reader.Stream != null)
            {
                reader.Log = readerContext.DebugStringStream.Printer;
                dwarf.StringTable.ReadInternal(reader);
            }

            reader.Log = readerContext.DebugLineStream.Printer;
            reader.Stream = readerContext.DebugLineStream.Stream;
            if (reader.Stream != null)
            {
                reader.Log = readerContext.DebugLineStream.Printer;
                dwarf.LineSection.ReadInternal(reader);
            }

            reader.Log = readerContext.DebugAddressRangeStream.Printer;
            reader.Stream = readerContext.DebugAddressRangeStream.Stream;
            if (reader.Stream != null)
            {
                dwarf.AddressRangeTable.ReadInternal(reader);
            }

            reader.Log = readerContext.DebugInfoStream.Printer;
            reader.Stream = readerContext.DebugInfoStream.Stream;
            if (reader.Stream != null)
            {
                reader.DefaultUnitKind = DwarfUnitKind.Compile;
                dwarf.InfoSection.ReadInternal(reader);
            }

            return dwarf;
        }

        public static DwarfFile ReadFromElf(ElfObjectFile elf)
        {
            var readerContext = DwarfReaderContext.FromElf(elf);
            return Read(readerContext);
        }

        private static void CheckErrors(DiagnosticBag diagnostics)
        {
            if (diagnostics.HasErrors)
            {
                throw new ObjectFileException("Unexpected errors while verifying and updating the layout", diagnostics);
            }
        }

        protected override void UpdateLayout(DwarfLayoutContext layoutContext)
        {
        }

        protected override void Write(DwarfWriter writer)
        {
        }
    }
}