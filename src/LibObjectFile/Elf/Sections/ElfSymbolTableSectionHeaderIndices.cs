// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using LibObjectFile.Diagnostics;

namespace LibObjectFile.Elf;

/// <summary>
/// A section with the type <see cref="ElfSectionType.SymbolTableSectionHeaderIndices"/>
/// </summary>
public sealed class ElfSymbolTableSectionHeaderIndices : ElfSection
{
    public const string DefaultName = ".symtab_shndx";

    private readonly List<uint> _entries;

    public ElfSymbolTableSectionHeaderIndices() : base(ElfSectionType.SymbolTableSectionHeaderIndices)
    {
        Name = DefaultName;
        _entries = new List<uint>();
        BaseTableEntrySize = sizeof(uint);
    }

    public override void Read(ElfReader reader)
    {
        reader.Position = Position;
        var numberOfEntries = base.Size / TableEntrySize;
        _entries.Clear();
        _entries.Capacity = (int)numberOfEntries;
        for (ulong i = 0; i < numberOfEntries; i++)
        {
            _entries.Add(reader.ReadU32());
        }
    }

    public override void Write(ElfWriter writer)
    {
        // Write all entries
        for (int i = 0; i < _entries.Count; i++)
        {
            writer.WriteU32(_entries[i]);
        }
    }

    protected override void AfterRead(ElfReader reader)
    {
        // Verify that the link is safe and configured as expected
        Link.TryGetSectionSafe<ElfSymbolTable>(nameof(ElfSymbolTableSectionHeaderIndices), nameof(Link), this, reader.Diagnostics, out var symbolTable, ElfSectionType.SymbolTable, ElfSectionType.DynamicLinkerSymbolTable);

        if (symbolTable is null)
        {
            return;
        }

        for (int i = 0; i < _entries.Count; i++)
        {
            var entry = _entries[i];
            if (entry != 0)
            {
                var resolvedLink = reader.ResolveLink(new ElfSectionLink((int)entry), $"Invalid link section index {{0}} for symbol table entry [{i}] from symbol table section .symtab_shndx");

                // Update the link in symbol table
                var symbolTableEntry = symbolTable.Entries[i];
                symbolTableEntry.SectionLink = resolvedLink;
                symbolTable.Entries[i] = symbolTableEntry;
            }
        }
    }

    public override void Verify(ElfVisitorContext context)
    {
        // Verify that the link is safe and configured as expected
        if (!Link.TryGetSectionSafe<ElfSymbolTable>(nameof(ElfSymbolTableSectionHeaderIndices), nameof(Link), this, context.Diagnostics, out var symbolTable, ElfSectionType.SymbolTable, ElfSectionType.DynamicLinkerSymbolTable))
        {
            return;
        }
    }

    protected override void UpdateLayoutCore(ElfVisitorContext context)
    {
        // Verify that the link is safe and configured as expected
        Link.TryGetSectionSafe<ElfSymbolTable>(nameof(ElfSymbolTableSectionHeaderIndices), nameof(Link), this, context.Diagnostics, out var symbolTable, ElfSectionType.SymbolTable, ElfSectionType.DynamicLinkerSymbolTable);

        int numberOfEntries = 0;

        if (symbolTable is not null)
        {
            for (int i = 0; i < symbolTable.Entries.Count; i++)
            {
                var section = symbolTable.Entries[i].SectionLink.Section;
                if (section is { SectionIndex: >= (int)ElfNative.SHN_LORESERVE })
                {
                    numberOfEntries = i + 1;
                }
            }
        }

        _entries.Capacity = numberOfEntries;
        _entries.Clear();

        if (symbolTable is not null)
        {
            for (int i = 0; i < numberOfEntries; i++)
            {
                var section = symbolTable.Entries[i].SectionLink.Section;
                if (section is { SectionIndex: >= (int)ElfNative.SHN_LORESERVE })
                {
                    _entries.Add((uint)section.SectionIndex);
                }
                else
                {
                    _entries.Add(0);
                }
            }
        }

        Size = Parent == null || Parent.FileClass == ElfFileClass.None ? 0 : (ulong)numberOfEntries * sizeof(uint);
    }

    internal override void InitializeEntrySizeFromRead(DiagnosticBag diagnostics, ulong entrySize, bool is32)
    {
        if (entrySize != sizeof(uint))
        {
            diagnostics.Error(DiagnosticId.ELF_ERR_InvalidSectionEntrySize, $"Invalid entry size `{entrySize}` for section [{this}]. The entry size must be at least `{sizeof(uint)}`");
            return;
        }

        BaseTableEntrySize = sizeof(uint);
        AdditionalTableEntrySize = 0;
    }
}