// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;

namespace LibObjectFile.Elf
{
    /// <summary>
    /// A section with the type <see cref="ElfSectionType.SymbolTableSectionHeaderIndices"/>
    /// </summary>
    public sealed class ElfSymbolTableSectionHeaderIndices : ElfSection
    {
        public const string DefaultName = ".symtab_shndx";

        private readonly List<ElfSectionLink> _entries;

        public ElfSymbolTableSectionHeaderIndices() : base(ElfSectionType.SymbolTableSectionHeaderIndices)
        {
            Name = DefaultName;
            _entries = new List<ElfSectionLink>();
        }

        public override ElfSectionType Type
        {
            get => base.Type;
            set
            {
                if (value != ElfSectionType.SymbolTableSectionHeaderIndices)
                {
                    throw new ArgumentException($"Invalid type `{Type}` of the section [{Index}] `{nameof(ElfSymbolTableSectionHeaderIndices)}`. Only `{ElfSectionType.SymbolTableSectionHeaderIndices}` is valid");
                }
                base.Type = value;
            }
        }

        /// <summary>
        /// Gets a list of <see cref="ElfSectionLink"/> entries.
        /// </summary>
        public IReadOnlyList<ElfSectionLink> Entries => _entries;

        public override unsafe ulong TableEntrySize => sizeof(uint);

        protected override void Read(ElfReader reader)
        {
            var numberOfEntries = base.Size / TableEntrySize;
            _entries.Clear();
            _entries.Capacity = (int)numberOfEntries;
            for (ulong i = 0; i < numberOfEntries; i++)
            {
                _entries.Add(new ElfSectionLink(reader.ReadU32()));
            }
        }

        protected override void Write(ElfWriter writer)
        {
            // Write all entries
            for (int i = 0; i < Entries.Count; i++)
            {
                writer.WriteU32(Entries[i].GetIndex());
            }
        }

        protected override void AfterRead(ElfReader reader)
        {
            // Verify that the link is safe and configured as expected
            Link.TryGetSectionSafe<ElfSymbolTable>(nameof(ElfSymbolTableSectionHeaderIndices), nameof(Link), this, reader.Diagnostics, out var symbolTable, ElfSectionType.SymbolTable, ElfSectionType.DynamicLinkerSymbolTable);

            for (int i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                if (entry.SpecialIndex != 0)
                {
                    entry = reader.ResolveLink(entry.Section, $"Invalid link section index {entry.SpecialIndex} for symbol table entry [{i}] from symbol table section [{this}]");
                    _entries[i] = entry;

                    // Update the link in symbol table
                    var symbolTableEntry = symbolTable.Entries[i];
                    symbolTableEntry.Section = entry.Section;
                    symbolTable.Entries[i] = symbolTableEntry;
                }
            }
        }

        public override void Verify(DiagnosticBag diagnostics)
        {
            base.Verify(diagnostics);

            // Verify that the link is safe and configured as expected
            if (!Link.TryGetSectionSafe<ElfSymbolTable>(nameof(ElfSymbolTableSectionHeaderIndices), nameof(Link), this, diagnostics, out var symbolTable, ElfSectionType.SymbolTable, ElfSectionType.DynamicLinkerSymbolTable))
            {
                return;
            }

            for (int i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                if (entry.SpecialIndex != 0 && entry.Section != null && entry.Section.Parent != Parent)
                {
                    diagnostics.Error(DiagnosticId.ELF_ERR_InvalidSymbolEntrySectionParent, $"Invalid section for the symbol entry #{i} in the {nameof(ElfSymbolTable)} section [{Index}]");
                }
            }
        }

        public override unsafe void UpdateLayout(DiagnosticBag diagnostics)
        {
            if (diagnostics == null) throw new ArgumentNullException(nameof(diagnostics));
            Size = Parent == null || Parent.FileClass == ElfFileClass.None ? 0 : (ulong)Entries.Count * sizeof(uint);
        }
    }
}