using System;
using System.Collections.Generic;
using System.IO;

namespace LibObjectFile.Elf
{
    public sealed class ElfSymbolTableSection : ElfSection
    {
        private uint _localIndexPlusOne;

        public ElfSymbolTableSection()
        {
            Entries = new List<ElfSymbolTableEntry>();
        }

        public List<ElfSymbolTableEntry> Entries { get;  }

        public override unsafe ulong GetSize(ElfFileClass fileClass)
        {
            switch (fileClass)
            {
                case ElfFileClass.Is32:
                    return (ulong)((Entries.Count + 1) * sizeof(RawElf.Elf32_Sym));
                case ElfFileClass.Is64:
                    return (ulong)((Entries.Count + 1) * sizeof(RawElf.Elf64_Sym));
                default:
                    throw ThrowHelper.InvalidEnum(fileClass);
            }
        }

        public override unsafe ulong GetFixedEntrySize(ElfFileClass fileClass)
        {
            switch (fileClass)
            {
                case ElfFileClass.Is32:
                    return (ulong)sizeof(RawElf.Elf32_Sym);
                case ElfFileClass.Is64:
                    return (ulong)sizeof(RawElf.Elf64_Sym);
                default:
                    throw ThrowHelper.InvalidEnum(fileClass);
            }
        }

        protected override uint GetInfoIndex(ElfWriter writer)
        {
            return _localIndexPlusOne;
        }

        private ElfStringTableSection GetSafeStringTable()
        {
            if (Link.Section == null) throw new InvalidOperationException($"ElfSection.{nameof(Link)} cannot be null for this instance");
            if (Link.Section.Type != ElfSectionType.StringTable) throw new InvalidOperationException($"The type `{Link.Section.Type}` of ElfSection.{nameof(Link)} must be a {nameof(ElfSectionType.StringTable)}");
            var stringTable = Link.Section as ElfStringTableSection;
            if (stringTable == null) throw new InvalidOperationException($"The ElfSection.{nameof(Link)} must be an instance of {nameof(ElfStringTableSection)}");
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
            var sym32 = new RawElf.Elf32_Sym();
            unsafe
            {
                var span = new ReadOnlySpan<byte>(&sym32, sizeof(RawElf.Elf32_Sym));
                writer.Stream.Write(span);
            }

            // Write all entries
            for (int i = 0; i < Entries.Count; i++)
            {
                var entry = Entries[i];

                VerifyEntry(ref entry, i);

                sym32 = new RawElf.Elf32_Sym();
                writer.Encode(out sym32.st_name, (ushort)stringTable.GetOrCreateIndex(entry.Name));
                writer.Encode(out sym32.st_value, (uint)entry.Value);
                writer.Encode(out sym32.st_size, (uint)entry.Size);
                sym32.st_info = (byte)(((byte) entry.Bind << 4) | (byte) entry.Type);
                writer.Encode(out sym32.st_shndx, (RawElf.Elf32_Half) entry.Section.GetSectionIndex());

                unsafe
                {
                    var span = new ReadOnlySpan<byte>(&sym32, sizeof(RawElf.Elf32_Sym));
                    writer.Stream.Write(span);
                }
            }
        }

        private void Write64(ElfWriter writer)
        {
            var stringTable = GetSafeStringTable();

            // First entry is null
            var sym32 = new RawElf.Elf64_Sym();
            unsafe
            {
                var span = new ReadOnlySpan<byte>(&sym32, sizeof(RawElf.Elf64_Sym));
                writer.Stream.Write(span);
            }

            for (int i = 0; i < Entries.Count; i++)
            {
                var entry = Entries[i];
                VerifyEntry(ref entry, i);

                var sym64 = new RawElf.Elf64_Sym();
                writer.Encode(out sym64.st_name, stringTable.GetOrCreateIndex(entry.Name));
                writer.Encode(out sym64.st_value, entry.Value);
                writer.Encode(out sym64.st_size, entry.Size);
                sym64.st_info = (byte)(((byte)entry.Bind << 4) | (byte)entry.Type);
                writer.Encode(out sym64.st_shndx, (RawElf.Elf64_Half)entry.Section.GetSectionIndex());

                unsafe
                {
                    var span = new ReadOnlySpan<byte>(&sym64, sizeof(RawElf.Elf64_Sym));
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