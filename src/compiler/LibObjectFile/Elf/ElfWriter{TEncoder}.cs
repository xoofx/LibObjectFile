using System;
using System.IO;

namespace LibObjectFile.Elf
{
    using static RawElf;

    internal abstract class ElfWriter<TEncoder> : ElfWriter where TEncoder : struct, ElfEncoder 
    {
        private TEncoder _encoder;
        private ulong _offsetOfSectionHeaderTable;
        private ulong _startOfFile;

        protected ElfWriter(ElfObjectFile objectFile, Stream stream) : base(objectFile, stream)
        {
            _encoder = new TEncoder();
        }

        public override void Write()
        {
            if (ObjectFile.FileClass == ElfFileClass.None)
            {
                throw new InvalidOperationException("Cannot write an ELF Class = None");
            }

            _startOfFile = (ulong)Stream.Position;
            PrepareSections();
            WriteHeader();
            WriteSections();
        }

        private void WriteHeader()
        {
            if (ObjectFile.FileClass == ElfFileClass.Is32)
            {
                WriteHeader32();
            }
            else
            {
                WriteHeader64();
            }
        }

        public override void Encode(out Elf32_Half dest, ushort value)
        {
            _encoder.Encode(out dest, value);
        }

        public override void Encode(out Elf64_Half dest, ushort value)
        {
            _encoder.Encode(out dest, value);
        }

        public override void Encode(out Elf32_Word dest, uint value)
        {
            _encoder.Encode(out dest, value);
        }

        public override void Encode(out Elf64_Word dest, uint value)
        {
            _encoder.Encode(out dest, value);
        }

        public override void Encode(out Elf32_Sword dest, int value)
        {
            _encoder.Encode(out dest, value);
        }

        public override void Encode(out Elf64_Sword dest, int value)
        {
            _encoder.Encode(out dest, value);
        }

        public override void Encode(out Elf32_Xword dest, ulong value)
        {
            _encoder.Encode(out dest, value);
        }

        public override void Encode(out Elf32_Sxword dest, long value)
        {
            _encoder.Encode(out dest, value);
        }

        public override void Encode(out Elf64_Xword dest, ulong value)
        {
            _encoder.Encode(out dest, value);
        }

        public override void Encode(out Elf64_Sxword dest, long value)
        {
            _encoder.Encode(out dest, value);
        }

        public override void Encode(out Elf32_Addr dest, uint value)
        {
            _encoder.Encode(out dest, value);
        }

        public override void Encode(out Elf64_Addr dest, ulong value)
        {
            _encoder.Encode(out dest, value);
        }

        public override void Encode(out Elf32_Off dest, uint offset)
        {
            _encoder.Encode(out dest, offset);
        }

        public override void Encode(out Elf64_Off dest, ulong offset)
        {
            _encoder.Encode(out dest, offset);
        }

        public override void Encode(out Elf32_Section dest, ushort index)
        {
            _encoder.Encode(out dest, index);
        }

        public override void Encode(out Elf64_Section dest, ushort index)
        {
            _encoder.Encode(out dest, index);
        }

        public override void Encode(out Elf32_Versym dest, ushort value)
        {
            _encoder.Encode(out dest, value);
        }

        public override void Encode(out Elf64_Versym dest, ushort value)
        {
            _encoder.Encode(out dest, value);
        }

        private unsafe void WriteHeader32()
        {
            var hdr = new Elf32_Ehdr();
            InitIdent(hdr.e_ident);

            ushort e_type;
            switch (ObjectFile.FileType)
            {
                case ElfFileType.None:
                    e_type = ET_NONE;
                    break;
                case ElfFileType.Relocatable:
                    e_type = ET_REL;
                    break;
                case ElfFileType.Executable:
                    e_type = ET_EXEC;
                    break;
                case ElfFileType.Dynamic:
                    e_type = ET_DYN;
                    break;
                case ElfFileType.Core:
                    e_type = ET_CORE;
                    break;
                default:
                    throw ThrowHelper.InvalidEnum(ObjectFile.FileType);
            }
            _encoder.Encode(out hdr.e_type, e_type);

            ushort e_machine = ObjectFile.Arch.Value;
            _encoder.Encode(out hdr.e_machine, e_machine);

            hdr.e_version = EV_CURRENT;

            _encoder.Encode(out hdr.e_entry, (uint)ObjectFile.EntryPointAddress);
            _encoder.Encode(out hdr.e_ehsize, (ushort) sizeof(Elf32_Ehdr));

            // entries for sections
            _encoder.Encode(out hdr.e_shoff, (uint)_offsetOfSectionHeaderTable);
            _encoder.Encode(out hdr.e_shentsize, (ushort)sizeof(Elf32_Shdr));
            _encoder.Encode(out hdr.e_shnum, (ushort)GetTotalSectionCount());
            _encoder.Encode(out hdr.e_shstrndx, (ushort)SectionHeaderNames.Index);

            var span = new ReadOnlySpan<byte>(&hdr, sizeof(Elf32_Ehdr));
            Stream.Write(span);
        }

        private unsafe void WriteHeader64()
        {
            var hdr = new Elf64_Ehdr();
            InitIdent(hdr.e_ident);

            ushort e_type;
            switch (ObjectFile.FileType)
            {
                case ElfFileType.None:
                    e_type = ET_NONE;
                    break;
                case ElfFileType.Relocatable:
                    e_type = ET_REL;
                    break;
                case ElfFileType.Executable:
                    e_type = ET_EXEC;
                    break;
                case ElfFileType.Dynamic:
                    e_type = ET_DYN;
                    break;
                case ElfFileType.Core:
                    e_type = ET_CORE;
                    break;
                default:
                    throw ThrowHelper.InvalidEnum(ObjectFile.FileType);
            }
            _encoder.Encode(out hdr.e_type, e_type);

            ushort e_machine = ObjectFile.Arch.Value;
            _encoder.Encode(out hdr.e_machine, e_machine);

            hdr.e_version = EV_CURRENT;

            _encoder.Encode(out hdr.e_entry, ObjectFile.EntryPointAddress);
            _encoder.Encode(out hdr.e_ehsize, (ushort)sizeof(Elf64_Ehdr));

            // entries for sections
            _encoder.Encode(out hdr.e_shoff, _offsetOfSectionHeaderTable);
            _encoder.Encode(out hdr.e_shentsize, (ushort)sizeof(Elf64_Shdr));
            _encoder.Encode(out hdr.e_shnum, (ushort) GetTotalSectionCount());
            _encoder.Encode(out hdr.e_shstrndx, (ushort)SectionHeaderNames.Index);

            var span = new ReadOnlySpan<byte>(&hdr, sizeof(Elf64_Ehdr));
            Stream.Write(span);
        }

        private uint GetTotalSectionCount()
        {
            if (ObjectFile.Sections.Count == 0) return 0;
            // + 1 (section names) + 1 (null section)
            return (uint)ObjectFile.Sections.Count + 1 + 1;
        }

        private unsafe void PrepareSections()
        {
            ulong offset = ObjectFile.FileClass == ElfFileClass.Is32 ? (uint)sizeof(Elf32_Ehdr) : (uint)sizeof(Elf64_Ehdr);
            _offsetOfSectionHeaderTable = 0;

            // If we have any sections, prepare their offsets
            if (ObjectFile.Sections.Count > 0)
            {
                uint sectionIndex = 1; // section names starts at one, after null section
                SectionHeaderNames.Reset();

                // First index is always an empty string
                SectionHeaderNames.GetOrCreateIndex(string.Empty);

                SectionHeaderNames.Index = sectionIndex;
                SectionHeaderNames.NameStringIndex = SectionHeaderNames.GetOrCreateIndex(SectionHeaderNames.Name);

                // The Section Header Table will be put just before all the sections
                _offsetOfSectionHeaderTable = offset;

                uint sizeOfSectionHeader = ObjectFile.FileClass == ElfFileClass.Is32 ? (uint)sizeof(Elf32_Shdr) : (uint)sizeof(Elf64_Shdr);
                offset += (uint) GetTotalSectionCount() * sizeOfSectionHeader;

                // Prepare all section names (to calculate the name indices and the size of the SectionNames)
                foreach (var section in ObjectFile.Sections)
                {
                    section.NameStringIndex = SectionHeaderNames.GetOrCreateIndex(section.Name);
                }

                // Section names is serialized right after the SectionHeaderTable
                SectionHeaderNames.Offset = offset;
                offset += SectionHeaderNames.GetSize(ObjectFile.FileClass);

                // Prepare all section before writing (e.g allowing sections to calculate their names)
                foreach (var section in ObjectFile.Sections)
                {
                    section.PrepareWriteInternal(this);
                }

                // Calculate offsets of all sections in the stream
                foreach (var section in ObjectFile.Sections)
                {
                    section.Offset = offset;

                    var link = section.Link;
                    if (link != null)
                    {
                        if (link.Parent != ObjectFile)
                        {
                            throw new InvalidOperationException($"The linked section `{link}` used by section `{link}` is not part of the existing section for the current object file");
                        }
                    }

                    // a NoBits section doesn't occupy any space in the file
                    if (section.Type == ElfSectionType.NoBits) continue;

                    offset += section.GetSize(ObjectFile.FileClass);
                }
            }
        }

        private void WriteSections()
        {
            if (ObjectFile.Sections.Count == 0) return;

            WriteSectionTable();

            // Write all sections right after the section table
            SectionHeaderNames.WriteInternal(this);
            foreach (var section in ObjectFile.Sections)
            {
                // a NoBits section doesn't occupy any space in the file
                if (section.Type == ElfSectionType.NoBits) continue;

                section.WriteInternal(this);
            }
        }

        private void WriteSectionTable()
        {
            var offset = (ulong)Stream.Position - _startOfFile;
            if (offset != _offsetOfSectionHeaderTable)
            {
                throw new InvalidOperationException("Internal error. Unexpected offset for SectionTable");
            }
            
            // Write NULL entry
            if (ObjectFile.FileClass == ElfFileClass.Is32)
            {
                WriteNullSectionTableEntry32();
            }
            else
            {
                WriteNullSectionTableEntry64();
            }

            WriteSectionTableEntry(SectionHeaderNames);
            foreach (var section in ObjectFile.Sections)
            {
                WriteSectionTableEntry(section);
            }
        }

        private void WriteSectionTableEntry(ElfSection section)
        {
            if (ObjectFile.FileClass == ElfFileClass.Is32)
            {
                WriteSectionTableEntry32(section);
            }
            else
            {
                WriteSectionTableEntry64(section);
            }
        }

        private void WriteSectionTableEntry32(ElfSection section)
        {
            var shdr = new Elf32_Shdr();
            _encoder.Encode(out shdr.sh_name, section.NameStringIndex);
            _encoder.Encode(out shdr.sh_type, GetSectionType(section.Type));
            _encoder.Encode(out shdr.sh_flags, GetSectionFlags(section.Flags));
            _encoder.Encode(out shdr.sh_addr, (uint)section.VirtualAddress);
            _encoder.Encode(out shdr.sh_offset, (uint)section.Offset);
            _encoder.Encode(out shdr.sh_size, (uint)section.GetSize(ObjectFile.FileClass));
            _encoder.Encode(out shdr.sh_link, section.Link?.Index ?? 0);
            _encoder.Encode(out shdr.sh_info, section.GetInfoIndexInternal(this)); // TODO support sh_info
            _encoder.Encode(out shdr.sh_addralign, (uint)section.Alignment);
            _encoder.Encode(out shdr.sh_entsize, (uint)section.GetFixedEntrySize(ObjectFile.FileClass));
            unsafe
            {
                var span = new ReadOnlySpan<byte>(&shdr, sizeof(Elf32_Shdr));
                Stream.Write(span);
            }
        }

        private void WriteSectionTableEntry64(ElfSection section)
        {
            var shdr = new Elf64_Shdr();
            _encoder.Encode(out shdr.sh_name, section.NameStringIndex);
            _encoder.Encode(out shdr.sh_type, GetSectionType(section.Type));
            _encoder.Encode(out shdr.sh_flags, GetSectionFlags(section.Flags));
            _encoder.Encode(out shdr.sh_addr, section.VirtualAddress);
            _encoder.Encode(out shdr.sh_offset, section.Offset);
            _encoder.Encode(out shdr.sh_size, section.GetSize(ObjectFile.FileClass));
            _encoder.Encode(out shdr.sh_link, section.Link?.Index ?? 0);
            _encoder.Encode(out shdr.sh_info, section.GetInfoIndexInternal(this));
            _encoder.Encode(out shdr.sh_addralign, section.Alignment);
            _encoder.Encode(out shdr.sh_entsize, section.GetFixedEntrySize(ObjectFile.FileClass));
            unsafe
            {
                var span = new ReadOnlySpan<byte>(&shdr, sizeof(Elf64_Shdr));
                Stream.Write(span);
            }
        }

        private void WriteNullSectionTableEntry32()
        {
            var shdr = new Elf32_Shdr();
            unsafe
            {
                var span = new ReadOnlySpan<byte>(&shdr, sizeof(Elf32_Shdr));
                Stream.Write(span);
            }
        }

        private void WriteNullSectionTableEntry64()
        {
            var shdr = new Elf64_Shdr();
            unsafe
            {
                var span = new ReadOnlySpan<byte>(&shdr, sizeof(Elf64_Shdr));
                Stream.Write(span);
            }
        }

        private static uint GetSectionType(ElfSectionType sectionType)
        {
            switch (sectionType)
            {
                case ElfSectionType.Null:
                    return SHT_NULL;
                case ElfSectionType.ProgBits:
                    return SHT_PROGBITS;
                case ElfSectionType.SymbolTable:
                    return SHT_SYMTAB;
                case ElfSectionType.StringTable:
                    return SHT_STRTAB;
                case ElfSectionType.RelocationAddends:
                    return SHT_RELA;
                case ElfSectionType.SymbolHashTable:
                    return SHT_HASH;
                case ElfSectionType.DynamicLinking:
                    return SHT_DYNAMIC;
                case ElfSectionType.Note:
                    return SHT_NOTE;
                case ElfSectionType.NoBits:
                    return SHT_NOBITS;
                case ElfSectionType.Relocation:
                    return SHT_REL;
                case ElfSectionType.Shlib:
                    return SHT_SHLIB;
                case ElfSectionType.DynamicLinkerSymbolTable:
                    return SHT_DYNSYM;
                default:
                    throw ThrowHelper.InvalidEnum(sectionType);
            }
        }

        private static uint GetSectionFlags(ElfSectionFlags sectionFlags)
        {
            uint flags = 0;
            if ((sectionFlags & ElfSectionFlags.Write) != 0)
            {
                flags |= SHF_WRITE;
            }
            if ((sectionFlags & ElfSectionFlags.Alloc) != 0)
            {
                flags |= SHF_ALLOC;
            }
            if ((sectionFlags & ElfSectionFlags.Executable) != 0)
            {
                flags |= SHF_EXECINSTR;
            }
            if ((sectionFlags & ElfSectionFlags.Merge) != 0)
            {
                flags |= SHF_MERGE;
            }
            if ((sectionFlags & ElfSectionFlags.Strings) != 0)
            {
                flags |= SHF_STRINGS;
            }
            if ((sectionFlags & ElfSectionFlags.InfoLink) != 0)
            {
                flags |= SHF_INFO_LINK;
            }
            if ((sectionFlags & ElfSectionFlags.LinkOrder) != 0)
            {
                flags |= SHF_LINK_ORDER;
            }
            if ((sectionFlags & ElfSectionFlags.OsNonConforming) != 0)
            {
                flags |= SHF_OS_NONCONFORMING;
            }
            if ((sectionFlags & ElfSectionFlags.Group) != 0)
            {
                flags |= SHF_GROUP;
            }
            if ((sectionFlags & ElfSectionFlags.Tls) != 0)
            {
                flags |= SHF_TLS;
            }
            if ((sectionFlags & ElfSectionFlags.Compressed) != 0)
            {
                flags |= SHF_COMPRESSED;
            }

            return flags;
        }

        private unsafe void InitIdent(byte* ident)
        {
            // Clear ident
            for (int i = 0; i < EI_NIDENT; i++)
            {
                ident[i] = 0;
            }

            ident[EI_MAG0] = ELFMAG0;
            ident[EI_MAG1] = ELFMAG1;
            ident[EI_MAG2] = ELFMAG2;
            ident[EI_MAG3] = ELFMAG3;

            switch (ObjectFile.FileClass)
            {
                case ElfFileClass.None:
                    ident[EI_CLASS] = ELFCLASSNONE;
                    break;
                case ElfFileClass.Is32:
                    ident[EI_CLASS] = ELFCLASS32;
                    break;
                case ElfFileClass.Is64:
                    ident[EI_CLASS] = ELFCLASS64;
                    break;
                default:
                    throw ThrowHelper.InvalidEnum(ObjectFile.FileClass);
            }

            switch (ObjectFile.Encoding)
            {
                case ElfEncoding.None:
                    ident[EI_DATA] = ELFDATANONE;
                    break;
                case ElfEncoding.Lsb:
                    ident[EI_DATA] = ELFDATA2LSB;
                    break;
                case ElfEncoding.Msb:
                    ident[EI_DATA] = ELFDATA2MSB;
                    break;
                default:
                    throw ThrowHelper.InvalidEnum(ObjectFile.Encoding);
            }

            ident[EI_VERSION] = EV_CURRENT;

            ident[EI_OSABI] = ObjectFile.OSAbi.Value;

            ident[EI_ABIVERSION] = 0;
        }
    }
}