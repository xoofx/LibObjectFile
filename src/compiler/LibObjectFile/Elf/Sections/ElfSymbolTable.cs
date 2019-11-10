using System;
using System.Collections.Generic;

namespace LibObjectFile.Elf
{
    public sealed class ElfSymbolTable : ElfSection
    {
        private uint _localIndexPlusOne;

        public const string DefaultName = ".symtab";

        public ElfSymbolTable() : base(ElfSectionType.SymbolTable)
        {
            Name = DefaultName;
            Entries = new List<ElfSymbol>();
        }

        public override ElfSectionType Type
        {
            get => base.Type;
            set
            {
                if (value != ElfSectionType.SymbolTable)
                {
                    throw new ArgumentException($"Invalid type `{Type}` of the section [{Index}] `{nameof(ElfSymbolTable)}`. Only `{ElfSectionType.SymbolTable}` is valid");
                }
                base.Type = value;
            }
        }

        public List<ElfSymbol> Entries { get;  }

        protected override unsafe ulong GetSize()
        {
            return Parent.FileClass == ElfFileClass.Is32 ? (ulong) ((Entries.Count + 1) * sizeof(RawElf.Elf32_Sym)) : (ulong) ((Entries.Count + 1) * sizeof(RawElf.Elf64_Sym));
        }

        protected override unsafe ulong GetTableEntrySize()
        {
            return Parent.FileClass == ElfFileClass.Is32 ? (ulong) sizeof(RawElf.Elf32_Sym) : (ulong) sizeof(RawElf.Elf64_Sym);
        }

        protected override uint GetInfoIndex(ElfWriter writer)
        {
            return _localIndexPlusOne;
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

        protected override void PrepareWrite(ElfWriter writer)
        {
            // Verify that the link is safe and configured as expected
            if (!Link.TryGetSectionSafe<ElfStringTable>(ElfSectionType.StringTable, nameof(ElfSymbolTable), nameof(Link), this, writer.Diagnostics, out var stringTable))
            {
                return;
            }

            bool isAllowingLocal = true;

            for (int i = 0; i < Entries.Count; i++)
            {
                var entry = Entries[i];

                if (entry.Section.Section != null && entry.Section.Section.Parent != Parent)
                {
                    writer.Diagnostics.Error($"Invalid section for the symbol entry #{i} in the {nameof(ElfSymbolTable)} section [{Index}]. The section of the entry `{entry}` must the same than this symbol table section");
                }

                stringTable.GetOrCreateIndex(entry.Name);

                // Update the last local index
                if (entry.Bind == ElfSymbolBind.Local)
                {
                    // + 1 For the plus one, another +1 for the entry 0
                    _localIndexPlusOne = (uint)(i + 1 + 1);
                    if (!isAllowingLocal)
                    {
                        writer.Diagnostics.Error($"Invalid position for the LOCAL symbol entry #{i} in the {nameof(ElfSymbolTable)} section [{Index}]. A LOCAL symbol entry must be before any other symbol entry");
                    }
                }
                else
                {
                    isAllowingLocal = false;
                }
            }
        }

        private void Write32(ElfWriter writer)
        {
            var stringTable = (ElfStringTable)Link.Section;

            // First entry is null
            var sym = new RawElf.Elf32_Sym();
            writer.Write(sym);

            // Write all entries
            for (int i = 0; i < Entries.Count; i++)
            {
                var entry = Entries[i];

                sym = new RawElf.Elf32_Sym();
                writer.Encode(out sym.st_name, (ushort)stringTable.GetOrCreateIndex(entry.Name));
                writer.Encode(out sym.st_value, (uint)entry.Value);
                writer.Encode(out sym.st_size, (uint)entry.Size);
                sym.st_info = (byte)(((byte) entry.Bind << 4) | (byte) entry.Type);
                sym.st_other = (byte) ((byte) entry.Visibility & 3);
                writer.Encode(out sym.st_shndx, (RawElf.Elf32_Half) entry.Section.GetSectionIndex());

                writer.Write(sym);
            }
        }

        private void Write64(ElfWriter writer)
        {
            var stringTable = (ElfStringTable)Link.Section;

            // First entry is null
            var sym = new RawElf.Elf64_Sym();
            writer.Write(sym);

            for (int i = 0; i < Entries.Count; i++)
            {
                var entry = Entries[i];

                sym = new RawElf.Elf64_Sym();
                writer.Encode(out sym.st_name, stringTable.GetOrCreateIndex(entry.Name));
                writer.Encode(out sym.st_value, entry.Value);
                writer.Encode(out sym.st_size, entry.Size);
                sym.st_info = (byte)(((byte)entry.Bind << 4) | (byte)entry.Type);
                sym.st_other = (byte)((byte)entry.Visibility & 3);
                writer.Encode(out sym.st_shndx, (RawElf.Elf64_Half)entry.Section.GetSectionIndex());

                writer.Write(sym);
            }
        }
   }
}