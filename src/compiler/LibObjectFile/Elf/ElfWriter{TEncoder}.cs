using System;
using System.IO;

namespace LibObjectFile.Elf
{
    using static RawElf;

    internal abstract class ElfWriter<TEncoder> : ElfWriter where TEncoder : struct, IElfEncoder 
    {
        private TEncoder _encoder;
        private ulong _startOfFile;

        protected ElfWriter(ElfObjectFile objectFile, Stream stream) : base(objectFile, stream)
        {
            _encoder = new TEncoder();
        }

        public override void Write()
        {
            if (ObjectFile.FileClass == ElfFileClass.None)
            {
                Diagnostics.Error("Cannot write an ELF Class = None");
                throw new ObjectFileException($"Invalid {nameof(ElfObjectFile)}", Diagnostics);
            }

            _startOfFile = (ulong)Stream.Position;
            PrepareProgramHeadersAndSections();
            WriteHeader();
            WriteProgramHeaders();
            WriteSections();
        }

        private ElfObjectFile.ElfObjectLayout Layout => ObjectFile.Layout;

        private void WriteHeader()
        {
            if (ObjectFile.FileClass == ElfFileClass.Is32)
            {
                WriteSectionHeader32();
            }
            else
            {
                WriteSectionHeader64();
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

        private unsafe ushort GetProgramHeaderSize()
        {
            return (ushort)(ObjectFile.FileClass == ElfFileClass.Is32 ? sizeof(Elf32_Phdr) : sizeof(Elf64_Phdr));
        }

        private void WriteProgramHeader32(ref ElfSegment segment)
        {
            var hdr = new Elf32_Phdr();

            _encoder.Encode(out hdr.p_type, segment.Type.Value);
            _encoder.Encode(out hdr.p_offset, (uint)segment.Offset.Value);
            _encoder.Encode(out hdr.p_vaddr, (uint)segment.VirtualAddress);
            _encoder.Encode(out hdr.p_paddr, (uint)segment.PhysicalAddress);
            _encoder.Encode(out hdr.p_filesz, (uint)segment.SizeInFile);
            _encoder.Encode(out hdr.p_memsz, (uint)segment.SizeInMemory);
            _encoder.Encode(out hdr.p_flags, segment.Flags.Value);
            _encoder.Encode(out hdr.p_align, (uint)segment.Align);

            Write(hdr);
        }

        private void WriteProgramHeader64(ref ElfSegment segment)
        {
            var hdr = new Elf64_Phdr();

            _encoder.Encode(out hdr.p_type, segment.Type.Value);
            _encoder.Encode(out hdr.p_offset, segment.Offset.Value);
            _encoder.Encode(out hdr.p_vaddr, segment.VirtualAddress);
            _encoder.Encode(out hdr.p_paddr, segment.PhysicalAddress);
            _encoder.Encode(out hdr.p_filesz, segment.SizeInFile);
            _encoder.Encode(out hdr.p_memsz, segment.SizeInMemory);
            _encoder.Encode(out hdr.p_flags, segment.Flags.Value);
            _encoder.Encode(out hdr.p_align, segment.Align);

            Write(hdr);
        }
        private unsafe void WriteSectionHeader32()
        {
            var hdr = new Elf32_Ehdr();
            ObjectFile.CopyIdentTo(new Span<byte>(hdr.e_ident, EI_NIDENT));

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

            _encoder.Encode(out hdr.e_version, EV_CURRENT);

            _encoder.Encode(out hdr.e_entry, (uint)ObjectFile.EntryPointAddress);
            _encoder.Encode(out hdr.e_ehsize, Layout.SizeOfElfHeader);
            _encoder.Encode(out hdr.e_flags, (uint)ObjectFile.Flags);

            // program headers
            _encoder.Encode(out hdr.e_phoff, (uint)Layout.OffsetOfProgramHeaderTable);
            _encoder.Encode(out hdr.e_phentsize, Layout.SizeOfProgramHeaderEntry);
            _encoder.Encode(out hdr.e_phnum, (ushort) ObjectFile.ProgramHeaders.Count);

            // entries for sections
            _encoder.Encode(out hdr.e_shoff, (uint)Layout.OffsetOfSectionHeaderTable);
            _encoder.Encode(out hdr.e_shentsize, Layout.SizeOfSectionHeaderEntry);
            _encoder.Encode(out hdr.e_shnum, (ushort)GetTotalSectionCount());
            _encoder.Encode(out hdr.e_shstrndx, (ushort)ObjectFile.SectionHeaderStringTableInternal.Index);

            Write(hdr);
        }

        private unsafe void WriteSectionHeader64()
        {
            var hdr = new Elf64_Ehdr();
            ObjectFile.CopyIdentTo(new Span<byte>(hdr.e_ident, EI_NIDENT));

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

            _encoder.Encode(out hdr.e_version, EV_CURRENT);

            _encoder.Encode(out hdr.e_entry, ObjectFile.EntryPointAddress);
            _encoder.Encode(out hdr.e_ehsize, Layout.SizeOfElfHeader);
            _encoder.Encode(out hdr.e_flags, (uint)ObjectFile.Flags);

            // program headers
            _encoder.Encode(out hdr.e_phoff, Layout.OffsetOfProgramHeaderTable);
            _encoder.Encode(out hdr.e_phentsize, Layout.SizeOfProgramHeaderEntry);
            _encoder.Encode(out hdr.e_phnum, (ushort)ObjectFile.ProgramHeaders.Count);

            // entries for sections
            _encoder.Encode(out hdr.e_shoff, Layout.OffsetOfSectionHeaderTable);
            _encoder.Encode(out hdr.e_shentsize, (ushort)sizeof(Elf64_Shdr));
            _encoder.Encode(out hdr.e_shnum, (ushort) GetTotalSectionCount());
            _encoder.Encode(out hdr.e_shstrndx, (ushort)ObjectFile.SectionHeaderStringTableInternal.Index);

            Write(hdr);
        }

        private uint GetTotalSectionCount()
        {
            if (ObjectFile.Sections.Count == 0) return 0;
            // + 1 (section names) + 1 (null section)
            return (uint)ObjectFile.Sections.Count + 1 + 1;
        }

        private unsafe void PrepareProgramHeadersAndSections()
        {
            ulong offset = ObjectFile.FileClass == ElfFileClass.Is32 ? (uint)sizeof(Elf32_Ehdr) : (uint)sizeof(Elf64_Ehdr);
            Layout.SizeOfElfHeader = (ushort)offset;
            Layout.OffsetOfProgramHeaderTable = 0;
            Layout.OffsetOfSectionHeaderTable = 0;
            Layout.SizeOfProgramHeaderEntry = 0;
            Layout.SizeOfSectionHeaderEntry = 0;

            // Write program headers
            if (ObjectFile.ProgramHeaders.Count > 0)
            {
                Layout.OffsetOfProgramHeaderTable = offset;
                Layout.SizeOfProgramHeaderEntry = (ushort) (ObjectFile.FileClass == ElfFileClass.Is32 ? sizeof(Elf32_Phdr) : sizeof(Elf64_Phdr));

                for(int i = 0; i < ObjectFile.ProgramHeaders.Count; i++)
                {
                    var programHeader = ObjectFile.ProgramHeaders[i];

                    if (programHeader.Offset.Section != null && programHeader.Offset.Section.Parent != ObjectFile)
                    {
                        Diagnostics.Error($"Invalid parent of the section for the Offset of the program header #{i}. It must have the same parent {nameof(ObjectFile)} than the current being written", ObjectFile);
                    }
                }
                offset += (ulong)ObjectFile.ProgramHeaders.Count * Layout.SizeOfProgramHeaderEntry;
            }

            // If we have any sections, prepare their offsets
            if (ObjectFile.Sections.Count > 0)
            {
                Layout.SizeOfSectionHeaderEntry = ObjectFile.FileClass == ElfFileClass.Is32 ? (ushort)sizeof(Elf32_Shdr) : (ushort)sizeof(Elf64_Shdr);

                // Verify all sections before doing anything else
                foreach (var section in ObjectFile.Sections)
                {
                    section.Verify(this.Diagnostics);
                }

                // If we have any any errors
                if (Diagnostics.HasErrors)
                {
                    throw new ObjectFileException("Errors while verifying sections:", this.Diagnostics);
                }

                foreach (var section in ObjectFile.Sections)
                {
                    section.BeforeWriteInternal(this);
                }
                
                // Calculate offsets of all sections in the stream
                foreach (var section in ObjectFile.Sections)
                {
                    section.Offset = offset;

                    var link = section.Link;
                    if (link.Section != null)
                    {
                        if (link.Section.Parent != ObjectFile)
                        {
                            Diagnostics.Error($"Invalid linked section `{link}` used by section `{section}` is not part of the existing section for the current object file");
                        }
                    }

                    // a NoBits section doesn't occupy any space in the file
                    if (section.Type == ElfSectionType.NoBits) continue;

                    offset += section.Size;
                }

                var shstrTable = ObjectFile.SectionHeaderStringTableInternal;


                shstrTable.Reset();
                shstrTable.Name = shstrTable.Name.WithIndex(shstrTable.GetOrCreateIndex(shstrTable.FullName));

                // Prepare all section names (to calculate the name indices and the size of the SectionNames)
                foreach (var section in ObjectFile.Sections)
                {
                    section.Name = section.Name.WithIndex(shstrTable.GetOrCreateIndex(section.FullName));
                }

                // Section names is serialized right after the SectionHeaderTable
                shstrTable.Offset = offset;
                offset += shstrTable.Size;

                // The Section Header Table will be put just after all the sections
                Layout.OffsetOfSectionHeaderTable = offset;
            }

            if (Diagnostics.HasErrors)
            {
                throw new ObjectFileException("Unexpected errors while trying to write this object file", Diagnostics);
            }
        }

        private void WriteProgramHeaders()
        {
            if (ObjectFile.ProgramHeaders.Count == 0)
            {
                return;
            }

            var offset = (ulong)Stream.Position - _startOfFile;
            if (offset != Layout.OffsetOfProgramHeaderTable)
            {
                throw new InvalidOperationException("Internal error. Unexpected offset for ProgramHeaderTable");
            }

            for (int i = 0; i < ObjectFile.ProgramHeaders.Count; i++)
            {
                var header = ObjectFile.ProgramHeaders[i];
                if (ObjectFile.FileClass == ElfFileClass.Is32)
                {
                    WriteProgramHeader32(ref header);
                }
                else
                {
                    WriteProgramHeader64(ref header);
                }
            }
        }

        private void WriteSections()
        {
            if (ObjectFile.Sections.Count == 0) return;

            foreach (var section in ObjectFile.Sections)
            {
                // a NoBits section doesn't occupy any space in the file
                if (section.Type == ElfSectionType.NoBits) continue;

                section.WriteInternal(this);
            }

            // Write all sections right after the section table
            ObjectFile.SectionHeaderStringTableInternal.WriteInternal(this);

            // Write section header table
            WriteSectionHeaderTable();
        }

        private void WriteSectionHeaderTable()
        {
            var offset = (ulong)Stream.Position - _startOfFile;
            if (offset != Layout.OffsetOfSectionHeaderTable)
            {
                throw new InvalidOperationException("Internal error. Unexpected offset for SectionHeaderTable");
            }
            
            // Write NULL entry first
            if (ObjectFile.FileClass == ElfFileClass.Is32)
            {
                WriteNullSectionTableEntry32();
            }
            else
            {
                WriteNullSectionTableEntry64();
            }

            // Then write all regular sections
            foreach (var section in ObjectFile.Sections)
            {
                WriteSectionTableEntry(section);
            }

            // Then write the section names
            WriteSectionTableEntry(ObjectFile.SectionHeaderStringTableInternal);
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
            _encoder.Encode(out shdr.sh_name, ObjectFile.SectionHeaderStringTableInternal.GetOrCreateIndex(section.FullName));
            _encoder.Encode(out shdr.sh_type, (uint)section.Type);
            _encoder.Encode(out shdr.sh_flags, (uint)section.Flags);
            _encoder.Encode(out shdr.sh_addr, (uint)section.VirtualAddress);
            _encoder.Encode(out shdr.sh_offset, (uint)section.Offset);
            _encoder.Encode(out shdr.sh_size, (uint)section.Size);
            _encoder.Encode(out shdr.sh_link, section.Link.GetIndex());
            _encoder.Encode(out shdr.sh_info, section.Info.GetIndex());
            _encoder.Encode(out shdr.sh_addralign, (uint)section.Alignment);
            _encoder.Encode(out shdr.sh_entsize, (uint)section.TableEntrySize);
            Write(shdr);
        }

        private void WriteSectionTableEntry64(ElfSection section)
        {
            var shdr = new Elf64_Shdr();
            _encoder.Encode(out shdr.sh_name, ObjectFile.SectionHeaderStringTableInternal.GetOrCreateIndex(section.FullName));
            _encoder.Encode(out shdr.sh_type, (uint)section.Type);
            _encoder.Encode(out shdr.sh_flags, (uint)section.Flags);
            _encoder.Encode(out shdr.sh_addr, section.VirtualAddress);
            _encoder.Encode(out shdr.sh_offset, section.Offset);
            _encoder.Encode(out shdr.sh_size, section.Size);
            _encoder.Encode(out shdr.sh_link, section.Link.GetIndex());
            _encoder.Encode(out shdr.sh_info, section.Info.GetIndex());
            _encoder.Encode(out shdr.sh_addralign, section.Alignment);
            _encoder.Encode(out shdr.sh_entsize, section.TableEntrySize);
            Write(shdr);
        }

        private void WriteNullSectionTableEntry32()
        {
            Write(new Elf32_Shdr());
        }

        private void WriteNullSectionTableEntry64()
        {
            Write(new Elf64_Shdr());
        }
    }
}