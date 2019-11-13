using System;
using System.Collections.Generic;
using System.IO;

namespace LibObjectFile.Elf
{
    internal abstract class ElfReader<TDecoder> : ElfReader where TDecoder : struct, IElfDecoder
    {
        private TDecoder _decoder;
        private ulong _startOfFile;
        private ushort _programHeaderCount;
        private ushort _sectionHeaderCount;
        private ushort _sectionStringTableIndex;
        private readonly List<ElfSection> _sections;
        private bool _isFirstSectionValidNull;
        private bool _hasValidSectionStringTable;

        protected ElfReader(ElfObjectFile objectFile, Stream stream) : base(objectFile, stream)
        {
            _decoder = new TDecoder();
            _sections = new List<ElfSection>();
        }

        private ElfObjectFile.ElfObjectLayout Layout => ObjectFile.Layout;

        internal override void Read()
        {
            if (ObjectFile.FileClass == ElfFileClass.None)
            {
                Diagnostics.Error("Cannot read an ELF Class = None");
                throw new ObjectFileException($"Invalid {nameof(ElfObjectFile)}", Diagnostics);
            }

            _startOfFile = (ulong)Stream.Position;
            ReadElfHeader();
            ReadProgramHeaders();
            ReadSections();

            VerifyAndFixProgramHeadersAndSections();
        }
        
        private void ReadElfHeader()
        {
            if (ObjectFile.FileClass == ElfFileClass.Is32)
            {
                ReadElfHeader32();
            }
            else
            {
                ReadElfHeader64();
            }

            if (_sectionHeaderCount >= RawElf.SHN_LORESERVE)
            {
                Diagnostics.Error($"Invalid number `{_sectionHeaderCount}` of section headers found from Elf Header. Must be < {RawElf.SHN_LORESERVE}");
            }
        }
        
        private unsafe void ReadElfHeader32()
        {
            RawElf.Elf32_Ehdr hdr;
            ulong streamOffset = (ulong)Stream.Position;
            if (!TryRead(sizeof(RawElf.Elf32_Ehdr), out hdr))
            {
                Diagnostics.Error($"Unable to read entirely Elf header. Not enough data (size: {sizeof(RawElf.Elf32_Ehdr)}) read at offset {streamOffset} from the stream");
            }

            ushort e_type = _decoder.Decode(hdr.e_type);
            switch (e_type)
            {
                case RawElf.ET_NONE:
                    ObjectFile.FileType = ElfFileType.None;
                    break;
                case RawElf.ET_REL:
                    ObjectFile.FileType = ElfFileType.Relocatable;
                    break;
                case RawElf.ET_EXEC:
                    ObjectFile.FileType = ElfFileType.Executable;
                    break;
                case RawElf.ET_DYN:
                    ObjectFile.FileType = ElfFileType.Dynamic;
                    break;
                case RawElf.ET_CORE:
                    e_type = RawElf.ET_CORE;
                    ObjectFile.FileType = ElfFileType.Core;
                    break;
                default:
                    Diagnostics.Error($"Invalid {nameof(RawElf.Elf32_Ehdr)}.{nameof(RawElf.Elf32_Ehdr.e_type)} 0x{e_type:x4}");
                    return;
            }

            ObjectFile.Arch = new ElfArch(_decoder.Decode(hdr.e_machine));
            ObjectFile.Version = _decoder.Decode(hdr.e_version);

            ObjectFile.EntryPointAddress = _decoder.Decode(hdr.e_entry);
            Layout.SizeOfElfHeader = _decoder.Decode(hdr.e_ehsize);
            ObjectFile.Flags = _decoder.Decode(hdr.e_flags);

            // program headers
            Layout.OffsetOfProgramHeaderTable = _decoder.Decode(hdr.e_phoff);
            Layout.SizeOfProgramHeaderEntry = _decoder.Decode(hdr.e_phentsize);
            _programHeaderCount = _decoder.Decode(hdr.e_phnum);

            // entries for sections
            Layout.OffsetOfSectionHeaderTable = _decoder.Decode(hdr.e_shoff);
            Layout.SizeOfSectionHeaderEntry = _decoder.Decode(hdr.e_shentsize);
            _sectionHeaderCount = _decoder.Decode(hdr.e_shnum);
            _sectionStringTableIndex = _decoder.Decode(hdr.e_shstrndx);
        }

        private unsafe void ReadElfHeader64()
        {
            RawElf.Elf64_Ehdr hdr;
            ulong streamOffset = (ulong)Stream.Position;
            if (!TryRead(sizeof(RawElf.Elf64_Ehdr), out hdr))
            {
                Diagnostics.Error($"Unable to read entirely Elf header. Not enough data (size: {sizeof(RawElf.Elf64_Ehdr)}) read at offset {streamOffset} from the stream");
            }

            ushort e_type = _decoder.Decode(hdr.e_type);
            switch (e_type)
            {
                case RawElf.ET_NONE:
                    ObjectFile.FileType = ElfFileType.None;
                    break;
                case RawElf.ET_REL:
                    ObjectFile.FileType = ElfFileType.Relocatable;
                    break;
                case RawElf.ET_EXEC:
                    ObjectFile.FileType = ElfFileType.Executable;
                    break;
                case RawElf.ET_DYN:
                    ObjectFile.FileType = ElfFileType.Dynamic;
                    break;
                case RawElf.ET_CORE:
                    e_type = RawElf.ET_CORE;
                    ObjectFile.FileType = ElfFileType.Core;
                    break;
                default:
                    Diagnostics.Error($"Invalid {nameof(RawElf.Elf64_Ehdr)}.{nameof(RawElf.Elf64_Ehdr.e_type)} 0x{e_type:x4}");
                    return;
            }

            ObjectFile.Arch = new ElfArch(_decoder.Decode(hdr.e_machine));
            ObjectFile.Version = _decoder.Decode(hdr.e_version);

            ObjectFile.EntryPointAddress = _decoder.Decode(hdr.e_entry);
            Layout.SizeOfElfHeader = _decoder.Decode(hdr.e_ehsize);
            ObjectFile.Flags = _decoder.Decode(hdr.e_flags);

            // program headers
            Layout.OffsetOfProgramHeaderTable = _decoder.Decode(hdr.e_phoff);
            Layout.SizeOfProgramHeaderEntry = _decoder.Decode(hdr.e_phentsize);
            _programHeaderCount = _decoder.Decode(hdr.e_phnum);

            // entries for sections
            Layout.OffsetOfSectionHeaderTable = _decoder.Decode(hdr.e_shoff);
            Layout.SizeOfSectionHeaderEntry = _decoder.Decode(hdr.e_shentsize);
            _sectionHeaderCount = _decoder.Decode(hdr.e_shnum);
            _sectionStringTableIndex = _decoder.Decode(hdr.e_shstrndx);
        }

        private void ReadProgramHeaders()
        {
            if (Layout.SizeOfProgramHeaderEntry == 0)
            {
                if (_programHeaderCount > 0)
                {
                    Diagnostics.Error($"Unable to read program header table as the size of program header entry ({nameof(RawElf.Elf32_Ehdr.e_phentsize)}) == 0 in the Elf Header");
                }
                return;
            }

            for (int i = 0; i < _programHeaderCount; i++)
            {
                var offset = Layout.OffsetOfProgramHeaderTable + (ulong)i * Layout.SizeOfProgramHeaderEntry;

                if (offset >= (ulong)Stream.Length)
                {
                    Diagnostics.Error($"Unable to read program header [{i}] as its offset {offset} is out of bounds");
                    break;
                }

                // Seek to the header position
                Stream.Position = (long)offset;

                var header = (ObjectFile.FileClass == ElfFileClass.Is32) ? ReadProgramHeader32(i) : ReadProgramHeader64(i);
                ObjectFile.ProgramHeaders.Add(header);
            }
        }
        
        private ElfSegment ReadProgramHeader32(int phdrIndex)
        {
            var streamOffset = Stream.Position;
            if (!TryRead(Layout.SizeOfSectionHeaderEntry, out RawElf.Elf32_Phdr hdr))
            {
                Diagnostics.Error($"Unable to read entirely program header [{phdrIndex}]. Not enough data (size: {Layout.SizeOfProgramHeaderEntry}) read at offset {streamOffset} from the stream");
            }

            return new ElfSegment
            {
                Type = new ElfSegmentType(_decoder.Decode(hdr.p_type)),
                Offset = new ElfOffset(_decoder.Decode(hdr.p_offset)),
                VirtualAddress = _decoder.Decode(hdr.p_vaddr),
                PhysicalAddress = _decoder.Decode(hdr.p_paddr),
                SizeInFile = _decoder.Decode(hdr.p_filesz),
                SizeInMemory = _decoder.Decode(hdr.p_memsz),
                Flags = new ElfSegmentFlags(_decoder.Decode(hdr.p_flags)),
                Align = _decoder.Decode(hdr.p_align)
            };
        }

        private ElfSegment ReadProgramHeader64(int phdrIndex)
        {
            var streamOffset = Stream.Position;
            if (!TryRead(Layout.SizeOfSectionHeaderEntry, out RawElf.Elf64_Phdr hdr))
            {
                Diagnostics.Error($"Unable to read entirely program header [{phdrIndex}]. Not enough data (size: {Layout.SizeOfProgramHeaderEntry}) read at offset {streamOffset} from the stream");
            }

            return new ElfSegment
            {
                Type = new ElfSegmentType(_decoder.Decode(hdr.p_type)),
                Offset = new ElfOffset(_decoder.Decode(hdr.p_offset)),
                VirtualAddress = _decoder.Decode(hdr.p_vaddr),
                PhysicalAddress = _decoder.Decode(hdr.p_paddr),
                SizeInFile = _decoder.Decode(hdr.p_filesz),
                SizeInMemory = _decoder.Decode(hdr.p_memsz),
                Flags = new ElfSegmentFlags(_decoder.Decode(hdr.p_flags)),
                Align = _decoder.Decode(hdr.p_align)
            };
        }
        
        private void ReadSections()
        {
            if (_sectionHeaderCount == 0) return;

            // Write section header table
            ReadSectionHeaderTable();
        }

        private void ReadSectionHeaderTable()
        {
            if (Layout.SizeOfSectionHeaderEntry == 0)
            {
                if (_sectionHeaderCount > 0)
                {
                    Diagnostics.Error($"Unable to read section header table as the size of section header entry ({nameof(RawElf.Elf32_Ehdr.e_ehsize)}) == 0 in the Elf Header");
                }
                return;
            }

            for (int i = 0; i < _sectionHeaderCount; i++)
            {
                var offset = Layout.OffsetOfSectionHeaderTable + (ulong)i * Layout.SizeOfSectionHeaderEntry;

                if (offset >= (ulong)Stream.Length)
                {
                    Diagnostics.Error($"Unable to read section [{i}] as its offset {offset} is out of bounds");
                    break;
                }

                // Seek to the header position
                Stream.Position = (long)offset;

                var section = ReadSectionTableEntry(i);
                _sections.Add(section);
            }
        }

        private ElfSection ReadSectionTableEntry(int sectionIndex)
        {
            return ObjectFile.FileClass == ElfFileClass.Is32 ? ReadSectionTableEntry32(sectionIndex) : ReadSectionTableEntry64(sectionIndex);
        }

        private ElfSection ReadSectionTableEntry32(int sectionIndex)
        {
            var streamOffset = Stream.Position;
            if (!TryRead(Layout.SizeOfSectionHeaderEntry, out RawElf.Elf32_Shdr rawSection))
            {
                Diagnostics.Error($"Unable to read entirely section header [{sectionIndex}]. Not enough data (size: {Layout.SizeOfSectionHeaderEntry}) read at offset {streamOffset} from the stream");
            }

            if (sectionIndex == 0)
            {
                _isFirstSectionValidNull = rawSection.IsNull;
            }
            
            var sectionType = (ElfSectionType)_decoder.Decode(rawSection.sh_type);
            var section = CreateElfSection(sectionIndex, sectionType);

            section.Name = new ElfString(_decoder.Decode(rawSection.sh_name));
            section.Type = (ElfSectionType)_decoder.Decode(rawSection.sh_type);
            section.Flags = (ElfSectionFlags)_decoder.Decode(rawSection.sh_flags);
            section.VirtualAddress = _decoder.Decode(rawSection.sh_addr);
            section.Offset = _decoder.Decode(rawSection.sh_offset);
            section.Alignment = _decoder.Decode(rawSection.sh_addralign);
            section.Link = new ElfSectionLink(_decoder.Decode(rawSection.sh_link));
            section.Info = new ElfSectionLink(_decoder.Decode(rawSection.sh_info));
            section.OriginalSize = _decoder.Decode(rawSection.sh_size);
            section.OriginalTableEntrySize = _decoder.Decode(rawSection.sh_entsize);

            return section;
        }

        private ElfSection ReadSectionTableEntry64(int sectionIndex)
        {
            var streamOffset = Stream.Position;
            if (!TryRead(Layout.SizeOfSectionHeaderEntry, out RawElf.Elf64_Shdr rawSection))
            {
                Diagnostics.Error($"Unable to read entirely section header [{sectionIndex}]. Not enough data (size: {Layout.SizeOfSectionHeaderEntry}) read at offset {streamOffset} from the stream");
            }

            if (sectionIndex == 0)
            {
                _isFirstSectionValidNull = rawSection.IsNull;
            }

            var sectionType = (ElfSectionType)_decoder.Decode(rawSection.sh_type);
            var section = CreateElfSection(sectionIndex, sectionType);

            section.Name = new ElfString(_decoder.Decode(rawSection.sh_name));
            section.Type = (ElfSectionType)_decoder.Decode(rawSection.sh_type);
            section.Flags = (ElfSectionFlags)_decoder.Decode(rawSection.sh_flags);
            section.VirtualAddress = _decoder.Decode(rawSection.sh_addr);
            section.Offset = _decoder.Decode(rawSection.sh_offset);
            section.Alignment = _decoder.Decode(rawSection.sh_addralign);
            section.Link = new ElfSectionLink(_decoder.Decode(rawSection.sh_link));
            section.Info = new ElfSectionLink(_decoder.Decode(rawSection.sh_info));
            section.OriginalSize = _decoder.Decode(rawSection.sh_size);
            section.OriginalTableEntrySize = _decoder.Decode(rawSection.sh_entsize);

            return section;
        }
        
        public override ElfSectionLink ResolveLink(ElfSectionLink link, string errorMessageFormat)
        {
            if (errorMessageFormat == null) throw new ArgumentNullException(nameof(errorMessageFormat));

            // Connect section Link instance
            if (!link.IsSpecial)
            {
                if (link.SpecialSectionIndex == _sectionStringTableIndex)
                {
                    link = new ElfSectionLink(ObjectFile.SectionHeaderStringTableInternal);
                }
                else
                {
                    var sectionIndex = link.SpecialSectionIndex;
                    if (sectionIndex >= _sections.Count)
                    {
                        Diagnostics.Error(string.Format(errorMessageFormat, link.SpecialSectionIndex));
                    }
                    else
                    {
                        link = new ElfSectionLink(_sections[(int)sectionIndex]);
                    }
                }
            }

            return link;
        }
       
        private void VerifyAndFixProgramHeadersAndSections()
        {
            if (!_isFirstSectionValidNull)
            {
                Diagnostics.Error($"Invalid non {RawElf.SHN_UNDEF} first section");
            }

            if (_hasValidSectionStringTable)
            {
                Stream.Position = (long)ObjectFile.SectionHeaderStringTableInternal.Offset;
                ObjectFile.SectionHeaderStringTableInternal.ReadInternal(this);
            }

            for (var i = 0; i < _sections.Count; i++)
            {
                var section = _sections[i];

                // Resolve the name of the section
                if (ObjectFile.SectionHeaderStringTableInternal.TryFind(section.Name.Index, out var sectionName))
                {
                    section.Name = section.Name.WithName(sectionName);
                }
                else
                {
                    Diagnostics.Error($"Unable to resolve string index [{section.Name.Index}] for section [{section.Index}] from section header string table");
                }

                // Connect section Link instance
                section.Link = ResolveLink(section.Link, $"Invalid section Link [{{0}}] for section [{i}]");

                // Connect section Info instance
                section.Info = ResolveLink(section.Info, $"Invalid section Info [{{0}}] for section [{i}]");

                if (i == 0 && _isFirstSectionValidNull)
                {
                    continue;
                }

                if (i == _sectionStringTableIndex && _hasValidSectionStringTable)
                {
                    continue;
                }

                Stream.Position = (long)section.Offset;
                section.ReadInternal(this);
            }

            foreach (var section in _sections)
            {
                section.AfterReadInternal(this);
            }

            for (var i = 0; i < _sections.Count; i++)
            {
                if (i == 0 && _isFirstSectionValidNull)
                {
                    continue;
                }

                var section = _sections[i];

                if (i == _sectionStringTableIndex && _hasValidSectionStringTable)
                {
                    continue;
                }

                section.Index = (uint)(ObjectFile._sections.Count + ElfObjectFile.MinSectionIndex);
                ObjectFile._sections.Add(section);
            }

            // Fixup section header string table index
            if (_hasValidSectionStringTable && ObjectFile.Sections.Count > 0)
            {
                ObjectFile.SectionHeaderStringTableInternal.Index = (uint)ObjectFile.Sections.Count + ElfObjectFile.MinSectionIndex;
            }

            // Lastly verify integrity of all sections
            foreach (var section in ObjectFile.Sections)
            {
                section.Verify(this.Diagnostics);
            }

            // Fix program headers
            for(int i = 0; i < ObjectFile.ProgramHeaders.Count; i++)
            {
                var phdr = ObjectFile.ProgramHeaders[i];

                for (int j = 0; j < ObjectFile.Sections.Count; j++)
                {
                    var section = ObjectFile.Sections[j];
                    if (phdr.Offset.Delta >= section.Offset && phdr.Offset.Delta < (section.Offset + section.OriginalSize))
                    {
                        phdr.Offset = new ElfOffset(section, phdr.Offset.Delta - section.Offset);
                        break;
                    }
                }
            }
        }

        private ElfSection CreateElfSection(int sectionIndex, ElfSectionType sectionType)
        {
            ElfSection section;

            switch (sectionType)
            {
                case ElfSectionType.Null:
                case ElfSectionType.ProgBits:
                case ElfSectionType.SymbolHashTable:
                case ElfSectionType.DynamicLinking:
                case ElfSectionType.Note:
                case ElfSectionType.NoBits:
                case ElfSectionType.Shlib:
                case ElfSectionType.DynamicLinkerSymbolTable:
                    section = new ElfCustomSection();
                    break;
                case ElfSectionType.SymbolTable:
                    section = new ElfSymbolTable();
                    break;
                case ElfSectionType.StringTable:

                    if (sectionIndex == _sectionStringTableIndex)
                    {
                        _hasValidSectionStringTable = true;
                        section = ObjectFile.SectionHeaderStringTableInternal;
                    }
                    else
                    {
                        section = new ElfStringTable();
                    }
                    break;
                case ElfSectionType.Relocation:
                case ElfSectionType.RelocationAddends:
                    section = new ElfRelocationTable();
                    break;
                default:
                    section = new ElfCustomSection();
                    break;
            }

            section.Index = (uint)sectionIndex;
            section.Parent = ObjectFile;

            return section;
        }


        public override ushort Decode(RawElf.Elf32_Half src)
        {
            return _decoder.Decode(src);
        }

        public override ushort Decode(RawElf.Elf64_Half src)
        {
            return _decoder.Decode(src);
        }

        public override uint Decode(RawElf.Elf32_Word src)
        {
            return _decoder.Decode(src);
        }

        public override uint Decode(RawElf.Elf64_Word src)
        {
            return _decoder.Decode(src);
        }

        public override int Decode(RawElf.Elf32_Sword src)
        {
            return _decoder.Decode(src);
        }

        public override int Decode(RawElf.Elf64_Sword src)
        {
            return _decoder.Decode(src);
        }

        public override ulong Decode(RawElf.Elf32_Xword src)
        {
            return _decoder.Decode(src);
        }

        public override long Decode(RawElf.Elf32_Sxword src)
        {
            return _decoder.Decode(src);
        }

        public override ulong Decode(RawElf.Elf64_Xword src)
        {
            return _decoder.Decode(src);
        }

        public override long Decode(RawElf.Elf64_Sxword src)
        {
            return _decoder.Decode(src);
        }

        public override uint Decode(RawElf.Elf32_Addr src)
        {
            return _decoder.Decode(src);
        }

        public override ulong Decode(RawElf.Elf64_Addr src)
        {
            return _decoder.Decode(src);
        }

        public override uint Decode(RawElf.Elf32_Off src)
        {
            return _decoder.Decode(src);
        }

        public override ulong Decode(RawElf.Elf64_Off src)
        {
            return _decoder.Decode(src);
        }

        public override ushort Decode(RawElf.Elf32_Section src)
        {
            return _decoder.Decode(src);
        }

        public override ushort Decode(RawElf.Elf64_Section src)
        {
            return _decoder.Decode(src);
        }

        public override ushort Decode(RawElf.Elf32_Versym src)
        {
            return _decoder.Decode(src);
        }

        public override ushort Decode(RawElf.Elf64_Versym src)
        {
            return _decoder.Decode(src);
        }
    }
}