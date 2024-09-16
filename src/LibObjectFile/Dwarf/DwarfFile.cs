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
        private DwarfLocationSection _locationSection;

        public DwarfFile()
        {
            AssignChild(this, new DwarfAbbreviationTable(), out _abbreviationTable);
            AssignChild(this, new DwarfStringTable(), out _stringTable);
            AssignChild(this, new DwarfLineSection(), out _lineSection);
            AssignChild(this, new DwarfInfoSection(), out _infoSection);
            AssignChild(this, new DwarfAddressRangeTable(), out _addressRangeTable);
            AssignChild(this, new DwarfLocationSection(), out _locationSection);
        }

        public DwarfAbbreviationTable AbbreviationTable
        {
            get => _abbreviationTable;
            set => AttachChild(this, value, ref _abbreviationTable);
        }
        
        public DwarfStringTable StringTable
        {
            get => _stringTable;
            set => AttachChild(this, value, ref _stringTable);
        }

        public DwarfLineSection LineSection
        {
            get => _lineSection;
            set => AttachChild(this, value, ref _lineSection);
        }

        public DwarfAddressRangeTable AddressRangeTable
        {
            get => _addressRangeTable;
            set => AttachChild(this, value, ref _addressRangeTable);
        }

        public DwarfInfoSection InfoSection
        {
            get => _infoSection;
            set => AttachChild(this, value, ref _infoSection);
        }

        public DwarfLocationSection LocationSection
        {
            get => _locationSection;
            set => AttachChild(this, value, ref _locationSection);
        }

        protected override void Read(DwarfReader reader)
        {
            throw new NotImplementedException();
        }

        public override void Verify(DiagnosticBag diagnostics)
        {
            base.Verify(diagnostics);

            LineSection.Verify(diagnostics);
            AbbreviationTable.Verify(diagnostics);
            AddressRangeTable.Verify(diagnostics);
            StringTable.Verify(diagnostics);
            InfoSection.Verify(diagnostics);
        }
        
        public void UpdateLayout(DwarfLayoutConfig config, DiagnosticBag diagnostics)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (diagnostics == null) throw new ArgumentNullException(nameof(diagnostics));

            var layoutContext = new DwarfLayoutContext(this, config, diagnostics);

            LineSection.Position = 0;
            LineSection.UpdateLayoutInternal(layoutContext);
            if (layoutContext.HasErrors)
            {
                return;
            }

            // Reset the abbreviation table
            // TODO: Make this configurable via the DwarfWriterContext
            AbbreviationTable.Position = 0;
            AbbreviationTable.Reset();

            InfoSection.Position = 0;
            InfoSection.UpdateLayoutInternal(layoutContext);
            if (layoutContext.HasErrors)
            {
                return;
            }

            // Update AddressRangeTable layout after Info
            AddressRangeTable.Position = 0;
            AddressRangeTable.UpdateLayoutInternal(layoutContext);
            if (layoutContext.HasErrors)
            {
                return;
            }

            // Update string table right after updating the layout of Info
            StringTable.Position = 0;
            StringTable.UpdateLayoutInternal(layoutContext);
            if (layoutContext.HasErrors)
            {
                return;
            }

            // Update the abbrev table right after we have computed the entire layout of Info
            AbbreviationTable.Position = 0;
            AbbreviationTable.UpdateLayoutInternal(layoutContext);

            LocationSection.Position = 0;
            LocationSection.UpdateLayoutInternal(layoutContext);
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
            var writer = new DwarfWriter(this, writerContext.IsLittleEndian, diagnostics);
            writer.AddressSize = writerContext.AddressSize;
            writer.EnableRelocation = writerContext.EnableRelocation;
            
            writer.Log = writerContext.DebugLinePrinter;
            if (writerContext.DebugLineStream != null)
            {
                writer.Stream = writerContext.DebugLineStream;
                writer.Stream.Position = 0;
                writer.Stream.SetLength(0);
                writer.CurrentSection = LineSection;
                LineSection.Relocations.Clear();
                LineSection.WriteInternal(writer);
            }

            writer.Log = null;
            if (writerContext.DebugAbbrevStream != null)
            {
                writer.Stream = writerContext.DebugAbbrevStream;
                writer.Stream.Position = 0;
                writer.Stream.SetLength(0);
                writer.CurrentSection = AbbreviationTable;
                AbbreviationTable.WriteInternal(writer);
            }

            if (writerContext.DebugAddressRangeStream != null)
            {
                writer.Stream = writerContext.DebugAddressRangeStream;
                writer.Stream.Position = 0;
                writer.Stream.SetLength(0);
                writer.CurrentSection = AddressRangeTable;
                AddressRangeTable.Relocations.Clear();
                AddressRangeTable.WriteInternal(writer);
            }
            
            if (writerContext.DebugStringStream != null)
            {
                writer.Stream = writerContext.DebugStringStream;
                writer.Stream.Position = 0;
                writer.Stream.SetLength(0);
                writer.CurrentSection = StringTable;
                StringTable.WriteInternal(writer);
            }

            if (writerContext.DebugInfoStream != null)
            {
                writer.Stream = writerContext.DebugInfoStream;
                writer.Stream.Position = 0;
                writer.Stream.SetLength(0);
                writer.CurrentSection = InfoSection;
                InfoSection.Relocations.Clear();
                InfoSection.WriteInternal(writer);
            }

            if (writerContext.DebugLocationStream != null)
            {
                writer.Stream = writerContext.DebugLocationStream;
                writer.Stream.Position = 0;
                writer.Stream.SetLength(0);
                writer.CurrentSection = LocationSection;
                LocationSection.Relocations.Clear();
                LocationSection.WriteInternal(writer);
            }

            CheckErrors(diagnostics);
        }
        
        public void WriteToElf(DwarfElfContext elfContext, DwarfLayoutConfig? layoutConfig = null)
        {
            if (elfContext == null) throw new ArgumentNullException(nameof(elfContext));

            var diagnostics = new DiagnosticBag();

            layoutConfig ??= new DwarfLayoutConfig();
            
            // Verify correctness
            Verify(diagnostics);
            CheckErrors(diagnostics);

            // Update the layout of all section and tables
            UpdateLayout(layoutConfig, diagnostics);
            CheckErrors(diagnostics);

            // Setup the output based on actual content of Dwarf infos
            var writer = new DwarfWriter(this, elfContext.IsLittleEndian, diagnostics)
            {
                AddressSize = elfContext.AddressSize, 
                EnableRelocation = elfContext.Elf.FileType == ElfFileType.Relocatable
            };

            // Pre-create table/sections to create symbols as well
            if (StringTable.Size > 0) elfContext.GetOrCreateStringTable();
            if (AbbreviationTable.Size > 0) elfContext.GetOrCreateAbbreviationTable();
            if (LineSection.Size > 0) elfContext.GetOrCreateLineSection();
            if (AddressRangeTable.Size > 0) elfContext.GetOrCreateAddressRangeTable();
            if (InfoSection.Size > 0) elfContext.GetOrCreateInfoSection();

            // String table
            if (StringTable.Size > 0)
            {
                writer.Stream = elfContext.GetOrCreateStringTable().Stream!;
                writer.Stream.Position = 0;
                writer.Stream.SetLength(0);
                writer.CurrentSection = StringTable;
                StringTable.WriteInternal(writer);
            }
            else
            {
                elfContext.RemoveStringTable();
            }

            // Abbreviation table
            if (AbbreviationTable.Size > 0)
            {
                writer.Stream = elfContext.GetOrCreateAbbreviationTable().Stream!; 
                writer.Stream.Position = 0;
                writer.Stream.SetLength(0);
                writer.CurrentSection = AbbreviationTable;
                AbbreviationTable.WriteInternal(writer);
            }
            else
            {
                elfContext.RemoveAbbreviationTable();
            }

            // Line table
            if (LineSection.Size > 0)
            {
                writer.Stream = elfContext.GetOrCreateLineSection().Stream!;
                writer.Stream.Position = 0;
                writer.Stream.SetLength(0);
                writer.CurrentSection = LineSection;
                LineSection.Relocations.Clear();
                LineSection.WriteInternal(writer);
                if (writer.EnableRelocation && LineSection.Relocations.Count > 0)
                {
                    LineSection.CopyRelocationsTo(elfContext, elfContext.GetOrCreateRelocLineSection());
                }
                else
                {
                    elfContext.RemoveRelocLineTable();
                }
            }
            else
            {
                elfContext.RemoveLineTable();
            }

            // AddressRange table
            if (AddressRangeTable.Size > 0)
            {
                writer.Stream = elfContext.GetOrCreateAddressRangeTable().Stream!;
                writer.Stream.Position = 0;
                writer.Stream.SetLength(0);
                writer.CurrentSection = AddressRangeTable;
                AddressRangeTable.Relocations.Clear();
                AddressRangeTable.WriteInternal(writer);

                if (writer.EnableRelocation && AddressRangeTable.Relocations.Count > 0)
                {
                    AddressRangeTable.CopyRelocationsTo(elfContext, elfContext.GetOrCreateRelocAddressRangeTable());
                }
                else
                {
                    elfContext.RemoveAddressRangeTable();
                }
            }
            else
            {
                elfContext.RemoveAddressRangeTable();
            }

            // InfoSection
            if (InfoSection.Size > 0)
            {
                writer.Stream = elfContext.GetOrCreateInfoSection().Stream!;
                writer.Stream.Position = 0;
                writer.Stream.SetLength(0);
                writer.CurrentSection = InfoSection;
                InfoSection.Relocations.Clear();
                InfoSection.WriteInternal(writer);

                if (writer.EnableRelocation && InfoSection.Relocations.Count > 0)
                {
                    InfoSection.CopyRelocationsTo(elfContext, elfContext.GetOrCreateRelocInfoSection());
                }
                else
                {
                    elfContext.RemoveRelocInfoSection();
                }
            }
            else
            {
                elfContext.RemoveInfoSection();
            }

            // LocationSection
            if (LocationSection.Size > 0)
            {
                writer.Stream = elfContext.GetOrCreateLocationSection().Stream!;
                writer.Stream.Position = 0;
                writer.Stream.SetLength(0);
                writer.CurrentSection = LocationSection;
                LocationSection.Relocations.Clear();
                LocationSection.WriteInternal(writer);

                if (writer.EnableRelocation && LocationSection.Relocations.Count > 0)
                {
                    LocationSection.CopyRelocationsTo(elfContext, elfContext.GetOrCreateRelocLocationSection());
                }
                else
                {
                    elfContext.RemoveRelocLocationSection();
                }
            }
            else
            {
                elfContext.RemoveLocationSection();
            }

            CheckErrors(diagnostics);
        }

        public static DwarfFile Read(DwarfReaderContext readerContext)
        {
            if (readerContext == null) throw new ArgumentNullException(nameof(readerContext));

            var dwarf = new DwarfFile();
            var reader = new DwarfReader(readerContext, dwarf, new DiagnosticBag());

            reader.Log = null;
            if (readerContext.DebugAbbrevStream != null)
            {
                reader.Stream = readerContext.DebugAbbrevStream;
                reader.CurrentSection = dwarf.AbbreviationTable;
                dwarf.AbbreviationTable.ReadInternal(reader);
            }

            if (readerContext.DebugStringStream != null)
            {
                reader.Stream = readerContext.DebugStringStream;
                reader.CurrentSection = dwarf.StringTable;
                dwarf.StringTable.ReadInternal(reader);
            }

            reader.Log = readerContext.DebugLinePrinter;
            if (readerContext.DebugLineStream != null)
            {
                reader.Stream = readerContext.DebugLineStream;
                reader.CurrentSection = dwarf.LineSection;
                dwarf.LineSection.ReadInternal(reader);
            }
            reader.Log = null;

            if (readerContext.DebugAddressRangeStream != null)
            {
                reader.Stream = readerContext.DebugAddressRangeStream;
                reader.CurrentSection = dwarf.AddressRangeTable;
                dwarf.AddressRangeTable.ReadInternal(reader);
            }

            reader.Log = null;
            if (readerContext.DebugLocationStream != null)
            {
                reader.Stream = readerContext.DebugLocationStream;
                reader.CurrentSection = dwarf.LocationSection;
                dwarf.LocationSection.ReadInternal(reader);
            }

            reader.Log = null;
            if (readerContext.DebugInfoStream != null)
            {
                reader.Stream = readerContext.DebugInfoStream;
                reader.DefaultUnitKind = DwarfUnitKind.Compile;
                reader.CurrentSection = dwarf.InfoSection;
                dwarf.InfoSection.ReadInternal(reader);
            }

            CheckErrors(reader.Diagnostics);

            return dwarf;
        }

        public static DwarfFile ReadFromElf(DwarfElfContext elfContext)
        {
            if (elfContext == null) throw new ArgumentNullException(nameof(elfContext));
            return Read(new DwarfReaderContext(elfContext));
        }

        public static DwarfFile ReadFromElf(ElfObjectFile elf)
        {
            return ReadFromElf(new DwarfElfContext(elf));
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