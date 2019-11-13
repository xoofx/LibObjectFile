using System;
using System.Collections.Generic;
using System.IO;

namespace LibObjectFile.Elf
{
    public sealed class ElfObjectFile
    {
        internal readonly List<ElfSection> _sections;
        internal const int MinSectionIndex = 1;

        public const int IdentSizeInBytes = RawElf.EI_NIDENT;

        public ElfObjectFile()
        {
            ProgramHeaders = new List<ElfSegment>();
            _sections = new List<ElfSection>();
            FileClass = ElfFileClass.Is64;
            OSAbi = ElfOSAbi.NONE;
            Encoding = ElfEncoding.Lsb;
            FileType = ElfFileType.Relocatable;
            Arch = ElfArch.X86_64;
            Version = RawElf.EV_CURRENT;
            Layout = new ElfObjectLayout();
            SectionHeaderStringTableInternal = new ElfStringTable(256) {  Parent = this }.ConfigureAs(ElfSectionSpecialType.SectionHeaderStringTable);
        }

        public ElfFileClass FileClass { get; set; }

        public ElfEncoding Encoding { get; set; }

        public uint Version { get; set; }

        public ElfOSAbi OSAbi { get; set; }

        public byte AbiVersion { get; set; }

        public ElfFileType FileType { get; set; }

        public ElfHeaderFlags Flags { get; set; }

        public ElfArch Arch { get; set; }

        public ulong EntryPointAddress { get; set; }

        public List<ElfSegment> ProgramHeaders { get; }

        public IReadOnlyList<ElfSection> Sections => _sections;

        public IElfSectionView SectionHeaderStringTable => SectionHeaderStringTableInternal;

        internal ElfStringTable SectionHeaderStringTableInternal { get; }

        public ElfObjectLayout Layout { get; }

        public TSection AddSection<TSection>(TSection section) where TSection : ElfSection
        {
            if (section == null) throw new ArgumentNullException(nameof(section));
            if (section.Parent != null)
            {
                if (section.Parent == this) throw new InvalidOperationException("Cannot add the section as it is already added");
                if (section.Parent != this) throw new InvalidOperationException($"Cannot add the section as it is already added to another {nameof(ElfObjectFile)} instance");
            }

            section.Parent = this;
            section.Index = (uint)(MinSectionIndex + _sections.Count);
            _sections.Add(section);

            // Always moved after the other sections
            SectionHeaderStringTableInternal.Index = section.Index + 1;
            return section;
        }

        public void RemoveSection(ElfSection section)
        {
            if (section == null) throw new ArgumentNullException(nameof(section));
            if (section.Parent != this)
            {
                throw new InvalidOperationException($"Cannot remove the section as it is not part of this {nameof(ElfObjectFile)} instance");
            }

            var i = (int)section.Index - MinSectionIndex;
            _sections.RemoveAt(i);

            // Update indices for other sections
            for (; i < _sections.Count; i++)
            {
                _sections[i].Index = (uint)(MinSectionIndex + i);
            }

            section.Parent = null;

            // Always moved after the other sections
            SectionHeaderStringTableInternal.Index--;
        }

        public static ElfObjectFile Read(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            var objectFile = new ElfObjectFile();
            var reader = ElfReader.Create(objectFile, stream);
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