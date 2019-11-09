using System;
using System.Collections.Generic;
using System.IO;

namespace LibObjectFile.Elf
{
    public sealed class ElfSymbolTable : ElfSection
    {
        private uint _localIndexPlusOne;

        public ElfSymbolTable()
        {
            Entries = new List<ElfSymbolTableEntry>();
        }

        public List<ElfSymbolTableEntry> Entries { get;  }

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

        private ElfStringTable GetSafeStringTable()
        {
            if (Link.Section == null) throw new InvalidOperationException($"ElfSection.{nameof(Link)} cannot be null for this instance");
            if (Link.Section.Type != ElfSectionType.StringTable) throw new InvalidOperationException($"The type `{Link.Section.Type}` of ElfSection.{nameof(Link)} must be a {nameof(ElfSectionType.StringTable)}");
            var stringTable = Link.Section as ElfStringTable;
            if (stringTable == null) throw new InvalidOperationException($"The ElfSection.{nameof(Link)} must be an instance of {nameof(ElfStringTable)}");
            return stringTable;
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
            var stringTable = GetSafeStringTable();
            for (int i = 0; i < Entries.Count; i++)
            {
                var entry = Entries[i];
                stringTable.GetOrCreateIndex(entry.Name);

                // Update the last local index
                if (entry.Bind == ElfSymbolBind.Local)
                {
                    // + 1 For the plus one, another +1 for the entry 0
                    _localIndexPlusOne = (uint)(i + 1 + 1);
                }
            }
        }

        private void Write32(ElfWriter writer)
        {
            var stringTable = GetSafeStringTable();

            // First entry is null
            var sym = new RawElf.Elf32_Sym();
            unsafe
            {
                var span = new ReadOnlySpan<byte>(&sym, sizeof(RawElf.Elf32_Sym));
                writer.Stream.Write(span);
            }

            // Write all entries
            for (int i = 0; i < Entries.Count; i++)
            {
                var entry = Entries[i];

                VerifyEntry(ref entry, i);

                sym = new RawElf.Elf32_Sym();
                writer.Encode(out sym.st_name, (ushort)stringTable.GetOrCreateIndex(entry.Name));
                writer.Encode(out sym.st_value, (uint)entry.Value);
                writer.Encode(out sym.st_size, (uint)entry.Size);
                sym.st_info = (byte)(((byte) entry.Bind << 4) | (byte) entry.Type);
                sym.st_other = (byte) ((byte) entry.Visibility & 3);
                writer.Encode(out sym.st_shndx, (RawElf.Elf32_Half) entry.Section.GetSectionIndex());

                unsafe
                {
                    var span = new ReadOnlySpan<byte>(&sym, sizeof(RawElf.Elf32_Sym));
                    writer.Stream.Write(span);
                }
            }
        }

        private void Write64(ElfWriter writer)
        {
            var stringTable = GetSafeStringTable();

            // First entry is null
            var sym = new RawElf.Elf64_Sym();
            unsafe
            {
                var span = new ReadOnlySpan<byte>(&sym, sizeof(RawElf.Elf64_Sym));
                writer.Stream.Write(span);
            }

            for (int i = 0; i < Entries.Count; i++)
            {
                var entry = Entries[i];
                VerifyEntry(ref entry, i);

                sym = new RawElf.Elf64_Sym();
                writer.Encode(out sym.st_name, stringTable.GetOrCreateIndex(entry.Name));
                writer.Encode(out sym.st_value, entry.Value);
                writer.Encode(out sym.st_size, entry.Size);
                sym.st_info = (byte)(((byte)entry.Bind << 4) | (byte)entry.Type);
                sym.st_other = (byte)((byte)entry.Visibility & 3);
                writer.Encode(out sym.st_shndx, (RawElf.Elf64_Half)entry.Section.GetSectionIndex());

                unsafe
                {
                    var span = new ReadOnlySpan<byte>(&sym, sizeof(RawElf.Elf64_Sym));
                    writer.Stream.Write(span);
                }
            }
        }

        private void VerifyEntry(ref ElfSymbolTableEntry entry, int index)
        {
            if (entry.Section.Section != null && entry.Section.Section.Parent != Parent) throw new InvalidOperationException($"The {nameof(ElfObjectFile)} parent of the section of the entry #{index} {entry} must the same than this symbol table section");
        }
    }
}