using System;
using System.Collections.Generic;

namespace LibObjectFile.Elf
{
    public sealed class ElfRelocationTable : ElfSection
    {
        public ElfRelocationTable() : base(ElfSectionType.RelocationAddends)
        {
            Entries = new List<ElfRelocation>();
        }

        public List<ElfRelocation> Entries { get; }
        
        public ElfSectionLink TargetSection { get; set; }
        
        public override ElfSectionType Type
        {
            get => base.Type;
            set
            {
                if (value != ElfSectionType.Relocation && value != ElfSectionType.RelocationAddends)
                {
                    throw new ArgumentException($"Invalid type `{Type}` of the section [{Index}] `{nameof(ElfRelocationTable)}` while `{ElfSectionType.Relocation}` or `{ElfSectionType.RelocationAddends}` are expected");
                }
                base.Type = value;
            }
        }

        public bool IsRelocationWithAddends => this.Type == ElfSectionType.RelocationAddends;

        protected override unsafe ulong GetSize()
        {
            return Parent.FileClass == ElfFileClass.Is32 ? 
                (ulong)(Entries.Count * (IsRelocationWithAddends ? sizeof(RawElf.Elf32_Rela) : sizeof(RawElf.Elf32_Rel)))  :
                (ulong)(Entries.Count * (IsRelocationWithAddends ? sizeof(RawElf.Elf64_Rela) : sizeof(RawElf.Elf64_Rel)));
        }

        protected override void Write(ElfWriter writer)
        {
            if (Parent.FileClass == ElfFileClass.Is32)
            {
                Write32(writer);
            }
            else
            {
                Write64(writer);
            }
        }

        protected override unsafe ulong GetTableEntrySize()
        {
            return Parent.FileClass == ElfFileClass.Is32 ? 
                (ulong) (IsRelocationWithAddends ? sizeof(RawElf.Elf32_Rela) : sizeof(RawElf.Elf32_Rel)) : 
                (ulong) (IsRelocationWithAddends ? sizeof(RawElf.Elf64_Rela) : sizeof(RawElf.Elf64_Rel));
        }

        protected override void PrepareWrite(ElfWriter writer)
        {
            if (TargetSection.Section == null)
            {
                writer.Diagnostics.Error($"Invalid {nameof(TargetSection)} of the section [{Index}] `{nameof(ElfRelocationTable)}` that cannot be null and must point to a valid section", this);
            }
            else if (TargetSection.Section.Parent != Parent)
            {
                writer.Diagnostics.Error($"Invalid parent for the {nameof(TargetSection)} of the section [{Index}] `{nameof(ElfRelocationTable)}`. It must point to the same {nameof(ElfObjectFile)} parent instance than this section parent", this);
            }

            // Write all entries
            for (int i = 0; i < Entries.Count; i++)
            {
                var entry = Entries[i];
                if (entry.Addend != 0 && !IsRelocationWithAddends)
                {
                    writer.Diagnostics.Error($"Invalid relocation entry {i} in section [{Index}] `{nameof(ElfRelocationTable)}`. The addend != 0 while the section is not a `{ElfSectionType.RelocationAddends}`", this);
                }
            }
        }

        protected override uint GetInfoIndex(ElfWriter writer)
        {
            return TargetSection.GetSectionIndex();
        }

        private void Write32(ElfWriter writer)
        {
            if (IsRelocationWithAddends)
            {
                // Write all entries
                for (int i = 0; i < Entries.Count; i++)
                {
                    var entry = Entries[i];

                    var sym = new RawElf.Elf32_Rel();
                    writer.Encode(out sym.r_offset, (uint)entry.Offset);
                    uint r_info = (entry.SymbolIndex << 8) | (entry.Type.Value & 0xFF);
                    writer.Encode(out sym.r_info, r_info);

                    unsafe
                    {
                        var span = new ReadOnlySpan<byte>(&sym, sizeof(RawElf.Elf32_Rel));
                        writer.Stream.Write(span);
                    }
                }
            }
            else
            {
                // Write all entries
                for (int i = 0; i < Entries.Count; i++)
                {
                    var entry = Entries[i];

                    var sym = new RawElf.Elf32_Rela();
                    writer.Encode(out sym.r_offset, (uint)entry.Offset);
                    uint r_info = (entry.SymbolIndex << 8) | (entry.Type.Value & 0xFF);
                    writer.Encode(out sym.r_info, r_info);
                    writer.Encode(out sym.r_addend, (int)entry.Addend);
                    writer.Write(sym);
                }
            }
        }

        private void Write64(ElfWriter writer)
        {
            if (IsRelocationWithAddends)
            {
                // Write all entries
                for (int i = 0; i < Entries.Count; i++)
                {
                    var entry = Entries[i];

                    var sym = new RawElf.Elf64_Rel();
                    writer.Encode(out sym.r_offset, (uint)entry.Offset);
                    ulong r_info = ((ulong)entry.SymbolIndex << 32) | (entry.Type.Value);
                    writer.Encode(out sym.r_info, r_info);
                    writer.Write(sym);
                }
            }
            else
            {
                // Write all entries
                for (int i = 0; i < Entries.Count; i++)
                {
                    var entry = Entries[i];

                    var sym = new RawElf.Elf64_Rela();
                    writer.Encode(out sym.r_offset, entry.Offset);
                    ulong r_info = ((ulong)entry.SymbolIndex << 32) | (entry.Type.Value );
                    writer.Encode(out sym.r_info, r_info);
                    writer.Encode(out sym.r_addend, entry.Addend);
                    writer.Write(sym);
                }
            }
        }
    }
}