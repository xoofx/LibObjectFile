namespace LibObjectFile.Elf
{
    public sealed class ElfProgramHeaderTable : ElfShadowSection
    {
        public ElfProgramHeaderTable()
        {
            Name = ".shadow.phdrtab";
        }

        protected override void Read(ElfReader reader)
        {
            // This is not read by this instance but by ElfReader directly
        }

        protected override unsafe ulong GetSizeAuto()
        {
            return (ulong)Parent.Segments.Count * Parent.Layout.SizeOfProgramHeaderEntry;
        }

        public override unsafe ulong TableEntrySize
        {
            get
            {
                if (Parent == null) return 0;
                return Parent.FileClass == ElfFileClass.Is32 ? (ulong)sizeof(RawElf.Elf32_Phdr) : (ulong)sizeof(RawElf.Elf64_Phdr);
            }
        }

        protected override void Write(ElfWriter writer)
        {
            for (int i = 0; i < Parent.Segments.Count; i++)
            {
                var header = Parent.Segments[i];
                if (Parent.FileClass == ElfFileClass.Is32)
                {
                    WriteProgramHeader32(writer, ref header);
                }
                else
                {
                    WriteProgramHeader64(writer, ref header);
                }
            }
        }
        
        private void WriteProgramHeader32(ElfWriter writer, ref ElfSegment segment)
        {
            var hdr = new RawElf.Elf32_Phdr();

            writer.Encode(out hdr.p_type, segment.Type.Value);
            writer.Encode(out hdr.p_offset, (uint)segment.Offset);
            writer.Encode(out hdr.p_vaddr, (uint)segment.VirtualAddress);
            writer.Encode(out hdr.p_paddr, (uint)segment.PhysicalAddress);
            writer.Encode(out hdr.p_filesz, (uint)segment.Size);
            writer.Encode(out hdr.p_memsz, (uint)segment.SizeInMemory);
            writer.Encode(out hdr.p_flags, segment.Flags.Value);
            writer.Encode(out hdr.p_align, (uint)segment.Align);

            writer.Write(hdr);
        }

        private void WriteProgramHeader64(ElfWriter writer, ref ElfSegment segment)
        {
            var hdr = new RawElf.Elf64_Phdr();

            writer.Encode(out hdr.p_type, segment.Type.Value);
            writer.Encode(out hdr.p_offset, segment.Offset);
            writer.Encode(out hdr.p_vaddr, segment.VirtualAddress);
            writer.Encode(out hdr.p_paddr, segment.PhysicalAddress);
            writer.Encode(out hdr.p_filesz, segment.Size);
            writer.Encode(out hdr.p_memsz, segment.SizeInMemory);
            writer.Encode(out hdr.p_flags, segment.Flags.Value);
            writer.Encode(out hdr.p_align, segment.Align);

            writer.Write(hdr);
        }
    }
}