using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using LibObjectFile.Utils;

namespace LibObjectFile.Elf
{
    public sealed class ElfObjectFile
    {
        private readonly List<ElfSection> _sections;
        private ElfSectionHeaderStringTable _sectionHeaderStringTable;
        private readonly List<ElfSegment> _segments;

        public const int IdentSizeInBytes = RawElf.EI_NIDENT;

        public ElfObjectFile() : this(true)
        {
        }

        internal ElfObjectFile(bool addDefaultSections)
        {
            _segments = new List<ElfSegment>();
            _sections = new List<ElfSection>();
            FileClass = ElfFileClass.Is64;
            OSABI = ElfOSABI.NONE;
            Encoding = ElfEncoding.Lsb;
            FileType = ElfFileType.Relocatable;
            Arch = ElfArch.X86_64;
            Version = RawElf.EV_CURRENT;
            Layout = new ElfObjectLayout();

            if (addDefaultSections)
            {
                AddSection(new ElfNullSection());
                AddSection(new ElfProgramHeaderTable());
            }
        }

        public ElfFileClass FileClass { get; set; }

        public ElfEncoding Encoding { get; set; }

        public uint Version { get; set; }

        public ElfOSABI OSABI { get; set; }

        public byte AbiVersion { get; set; }

        public ElfFileType FileType { get; set; }

        public ElfHeaderFlags Flags { get; set; }

        public ElfArch Arch { get; set; }

        public ulong EntryPointAddress { get; set; }

        public IReadOnlyList<ElfSegment> Segments => _segments;

        public IReadOnlyList<ElfSection> Sections => _sections;
        
        public uint VisibleSectionCount { get; private set; }

        public uint ShadowSectionCount { get; private set; }

        public ElfSectionHeaderStringTable SectionHeaderStringTable
        {
            get => _sectionHeaderStringTable;
            set
            {
                if (value != null)
                {
                    if (value.Parent == null)
                    {
                        throw new InvalidOperationException($"The {nameof(ElfSectionHeaderStringTable)} must have been added via `this.{nameof(AddSection)}(section)` before setting {nameof(SectionHeaderStringTable)}");
                    }

                    if (value.Parent != this)
                    {
                        throw new InvalidOperationException($"This {nameof(ElfSectionHeaderStringTable)} belongs already to another {nameof(ElfObjectFile)}. It must be removed from the other instance before adding it to this instance.");
                    }
                }
                _sectionHeaderStringTable = value;
            }
        }

        public ElfObjectLayout Layout { get; }

        public void Verify(DiagnosticBag diagnostics)
        {
            if (diagnostics == null) throw new ArgumentNullException(nameof(diagnostics));

            if (FileClass == ElfFileClass.None)
            {
                diagnostics.Error(DiagnosticId.ELF_ERR_InvalidHeaderFileClassNone, $"Cannot compute the layout with an {nameof(ElfObjectFile)} having a {nameof(FileClass)} == {ElfFileClass.None}");
            }

            foreach (var segment in Segments)
            {
                segment.Verify(diagnostics);
            }

            // Verify all sections before doing anything else
            foreach (var section in Sections)
            {
                section.Verify(diagnostics);
            }
        }

        internal void SortSectionsByOffset()
        {
            _sections.Sort(CompareSectionOffsetsDelegate);
            for (int i = 0; i < _sections.Count; i++)
            {
                _sections[i].Index = (uint)i;
            }
        }

        private static readonly Comparison<ElfSection> CompareSectionOffsetsDelegate = new Comparison<ElfSection>(CompareSectionOffsets);

        private static int CompareSectionOffsets(ElfSection left, ElfSection right)
        {
            return left.Offset.CompareTo(right.Offset);
        }

        public void UpdateLayout()
        {
            var diagnostics = new DiagnosticBag();
            TryUpdateLayout(diagnostics);
            if (diagnostics.HasErrors)
            {
                throw new ObjectFileException("Unexpected error while updating the layout of this instance", diagnostics);
            }
        }

        public unsafe bool TryUpdateLayout(DiagnosticBag diagnostics)
        {
            if (diagnostics == null) throw new ArgumentNullException(nameof(diagnostics));

            // Check first that we have a valid object file
            var localDiagnostics = new DiagnosticBag();
            Verify(localDiagnostics);
            
            // If we have any any errors
            if (localDiagnostics.HasErrors)
            {
                localDiagnostics.CopyTo(diagnostics);
                return false;
            }

            ulong offset = FileClass == ElfFileClass.Is32 ? (uint)sizeof(RawElf.Elf32_Ehdr) : (uint)sizeof(RawElf.Elf64_Ehdr);
            Layout.SizeOfElfHeader = (ushort)offset;
            Layout.OffsetOfProgramHeaderTable = 0;
            Layout.OffsetOfSectionHeaderTable = 0;
            Layout.SizeOfProgramHeaderEntry = FileClass == ElfFileClass.Is32 ? (ushort)sizeof(RawElf.Elf32_Phdr) : (ushort)sizeof(RawElf.Elf64_Phdr);
            Layout.SizeOfSectionHeaderEntry = FileClass == ElfFileClass.Is32 ? (ushort)sizeof(RawElf.Elf32_Shdr) : (ushort)sizeof(RawElf.Elf64_Shdr);

            bool programHeaderTableFoundAndUpdated = false;

            // If we have any sections, prepare their offsets
            var sections = Sections;
            if (sections.Count > 0)
            {
                // Calculate offsets of all sections in the stream
                for (var i = 0; i < sections.Count; i++)
                {
                    var section = sections[i];
                    if (i == 0 && section.Type == ElfSectionType.Null)
                    {
                        continue;
                    }

                    var align = section.Alignment == 0 ? 1 : section.Alignment;
                    offset = AlignHelper.AlignToUpper(offset, align);
                    section.Offset = offset;

                    if (section is ElfProgramHeaderTable programHeaderTable)
                    {
                        if (Segments.Count > 0)
                        {
                            Layout.OffsetOfProgramHeaderTable = section.Offset;
                            Layout.SizeOfProgramHeaderEntry = (ushort) section.TableEntrySize;
                            programHeaderTableFoundAndUpdated = true;
                        }
                    }

                    if (section == SectionHeaderStringTable)
                    {
                        var shstrTable = SectionHeaderStringTable;
                        shstrTable.Reset();

                        // Prepare all section names (to calculate the name indices and the size of the SectionNames)
                        for (var j = 0; j < sections.Count; j++)
                        {
                            var otherSection = sections[j];
                            if ((j == 0 && otherSection.Type == ElfSectionType.Null)) continue;
                            if (otherSection.IsShadow) continue;
                            otherSection.Name = otherSection.Name.WithIndex(shstrTable.GetOrCreateIndex(otherSection.Name));
                        }
                    }

                    // A section without content doesn't count with its size
                    if (!section.HasContent)
                    {
                        continue;
                    }

                    offset += section.Size;
                }

                // The Section Header Table will be put just after all the sections
                Layout.OffsetOfSectionHeaderTable = offset;
            }

            // Update program headers with offsets from auto layout
            if (Segments.Count > 0)
            {
                // Write program headers
                if (!programHeaderTableFoundAndUpdated)
                {
                    diagnostics.Error(DiagnosticId.ELF_ERR_MissingProgramHeaderTableSection, $"Missing {nameof(ElfProgramHeaderTable)} shadow section for writing program headers / segments from this object file");
                }

                for (int i = 0; i < Segments.Count; i++)
                {
                    var programHeader = Segments[i];
                    if (programHeader.OffsetKind == ElfValueKind.Auto)
                    {
                        programHeader.Offset = programHeader.Range.Offset;
                    }
                }
            }

            return true;
        }

        public void AddSegment(ElfSegment segment)
        {
            if (segment == null) throw new ArgumentNullException(nameof(segment));
            if (segment.Parent != null)
            {
                if (segment.Parent == this) throw new InvalidOperationException("Cannot add the segment as it is already added");
                if (segment.Parent != this) throw new InvalidOperationException($"Cannot add the segment as it is already added to another {nameof(ElfObjectFile)} instance");
            }

            segment.Parent = this;
            segment.Index = (uint)_segments.Count;
            _segments.Add(segment);
        }

        public void InsertSegmentAt(int index, ElfSegment segment)
        {
            if (index < 0 || index > _segments.Count) throw new ArgumentOutOfRangeException(nameof(index), $"Invalid index {index}, Must be >= 0 && <= {_segments.Count}");
            if (segment == null) throw new ArgumentNullException(nameof(segment));
            if (segment.Parent != null)
            {
                if (segment.Parent == this) throw new InvalidOperationException("Cannot add the segment as it is already added");
                if (segment.Parent != this) throw new InvalidOperationException($"Cannot add the segment as it is already added to another {nameof(ElfObjectFile)} instance");
            }

            segment.Index = (uint)index;
            _segments.Insert(index, segment);
            segment.Parent = this;

            // Update the index of following segments
            for(int i = index + 1; i < _segments.Count; i++)
            {
                var nextSegment = _segments[i];
                nextSegment.Index++;
            }
        }

        public void RemoveSegment(ElfSegment segment)
        {
            if (segment == null) throw new ArgumentNullException(nameof(segment));
            if (segment.Parent != this)
            {
                throw new InvalidOperationException($"Cannot remove this segment as it is not part of this {nameof(ElfObjectFile)} instance");
            }

            var i = (int)segment.Index;
            _segments.RemoveAt(i);
            segment.Index = 0;

            // Update indices for other sections
            for (int j = i + 1; j < _segments.Count; j++)
            {
                var nextSegments = _segments[j];
                nextSegments.Index--;
            }

            segment.Parent = null;
        }

        public ElfSegment RemoveSegmentAt(int index)
        {
            if (index < 0 || index > _segments.Count) throw new ArgumentOutOfRangeException(nameof(index), $"Invalid index {index}, Must be >= 0 && <= {_segments.Count}");
            var segment = _segments[index];
            RemoveSegment(segment);
            return segment;
        }

        public TSection AddSection<TSection>(TSection section) where TSection : ElfSection
        {
            if (section == null) throw new ArgumentNullException(nameof(section));
            if (section.Parent != null)
            {
                if (section.Parent == this) throw new InvalidOperationException("Cannot add the section as it is already added");
                if (section.Parent != this) throw new InvalidOperationException($"Cannot add the section as it is already added to another {nameof(ElfObjectFile)} instance");
            }

            section.Parent = this;
            section.Index = (uint)_sections.Count;
            _sections.Add(section);

            if (section.IsShadow)
            {
                section.SectionIndex = 0;
                ShadowSectionCount++;
            }
            else
            {
                section.SectionIndex = VisibleSectionCount;
                VisibleSectionCount++;
            }

            // Setup the ElfSectionHeaderStringTable if not already set
            if (section is ElfSectionHeaderStringTable sectionHeaderStringTable && SectionHeaderStringTable == null)
            {
                SectionHeaderStringTable = sectionHeaderStringTable;
            }

            return section;
        }

        public void InsertSectionAt(int index, ElfSection section)
        {
            if (index < 0 || index > _sections.Count) throw new ArgumentOutOfRangeException(nameof(index), $"Invalid index {index}, Must be >= 0 && <= {_sections.Count}");
            if (section == null) throw new ArgumentNullException(nameof(section));
            if (section.Parent != null)
            {
                if (section.Parent == this) throw new InvalidOperationException("Cannot add the section as it is already added");
                if (section.Parent != this) throw new InvalidOperationException($"Cannot add the section as it is already added to another {nameof(ElfObjectFile)} instance");
            }

            section.Parent = this;
            section.Index = (uint)index;
            _sections.Insert(index, section);

            if (section.IsShadow)
            {
                section.SectionIndex = 0;
                ShadowSectionCount++;

                // Update the index of the following sections
                for (int j = index + 1; j < _sections.Count; j++)
                {
                    var sectionAfter = _sections[j];
                    sectionAfter.Index++;
                }
            }
            else
            {
                ElfSection previousSection = null;
                for (int j = 0; j < index; j++)
                {
                    var sectionBefore = _sections[j];
                    if (!sectionBefore.IsShadow)
                    {
                        previousSection = sectionBefore;
                    }
                }
                section.SectionIndex = previousSection != null ? previousSection.SectionIndex + 1 : 0;

                // Update the index of the following sections
                for (int j = index + 1; j < _sections.Count; j++)
                {
                    var sectionAfter = _sections[j];
                    if (!sectionAfter.IsShadow)
                    {
                        sectionAfter.SectionIndex++;
                    }
                    sectionAfter.Index++;
                }

                VisibleSectionCount++;
            }

            // Setup the ElfSectionHeaderStringTable if not already set
            if (section is ElfSectionHeaderStringTable sectionHeaderStringTable && SectionHeaderStringTable == null)
            {
                SectionHeaderStringTable = sectionHeaderStringTable;
            }
        }
        
        public void RemoveSection(ElfSection section)
        {
            if (section == null) throw new ArgumentNullException(nameof(section));
            if (section.Parent != this)
            {
                throw new InvalidOperationException($"Cannot remove the section as it is not part of this {nameof(ElfObjectFile)} instance");
            }

            var i = (int)section.Index;
            _sections.RemoveAt(i);
            section.Index = 0;

            bool wasShadow = section.IsShadow;

            // Update indices for other sections
            for (int j = i + 1; j < _sections.Count; j++)
            {
                var nextSection = _sections[j];
                nextSection.Index--;

                // Update section index as well for following non-shadow sections
                if (!wasShadow && !nextSection.IsShadow)
                {
                    nextSection.SectionIndex--;
                }
            }

            if (wasShadow)
            {
                ShadowSectionCount--;
            }
            else
            {
                VisibleSectionCount--;
            }

            section.Parent = null;

            // Automatically replace the current ElfSectionHeaderStringTable with another existing one if any
            if (section is ElfSectionHeaderStringTable && SectionHeaderStringTable == section)
            {
                SectionHeaderStringTable = null;
                foreach (var nextSection in _sections)
                {
                    if (nextSection is ElfSectionHeaderStringTable nextSectionHeaderStringTable)
                    {
                        SectionHeaderStringTable = nextSectionHeaderStringTable;
                        break;
                    }
                }
            }
        }

        public ElfSection RemoveSectionAt(int index)
        {
            if (index < 0 || index > _sections.Count) throw new ArgumentOutOfRangeException(nameof(index), $"Invalid index {index}, Must be >= 0 && <= {_sections.Count}");
            var section = _sections[index];
            RemoveSection(section);
            return section;
        }

        public static ElfObjectFile Read(Stream stream, ElfReaderOptions options = null)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            var objectFile = new ElfObjectFile(false);
            options ??= new ElfReaderOptions();

            var reader = ElfReader.Create(objectFile, stream, options);
            reader.Read();

            if (reader.Diagnostics.HasErrors)
            {
                throw new ObjectFileException($"Unexpected error while reading ELF object file", reader.Diagnostics);
            }

            return objectFile;
        }

        public sealed class ElfObjectLayout
        {
            internal ElfObjectLayout()
            {
            }

            public ushort SizeOfElfHeader { get; internal set; }

            public ulong OffsetOfProgramHeaderTable { get; internal set; }

            public ushort SizeOfProgramHeaderEntry { get; internal set; }

            public ulong OffsetOfSectionHeaderTable { get; internal set; }

            public ushort SizeOfSectionHeaderEntry { get; internal set; }
        }
    }
}