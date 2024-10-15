// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using LibObjectFile.Diagnostics;
using LibObjectFile.IO;

namespace LibObjectFile.Elf;

/// <summary>
/// A symbol table section with the type <see cref="ElfSectionType.SymbolTable"/> or <see cref="ElfSectionType.DynamicLinkerSymbolTable"/>
/// </summary>
public sealed class ElfSymbolTable : ElfSection
{
    public const string DefaultName = ".symtab";
    private bool _is32;

    public ElfSymbolTable() : this(true)
    {
    }

    public ElfSymbolTable(bool isDynamic) : base(isDynamic ? ElfSectionType.DynamicLinkerSymbolTable : ElfSectionType.SymbolTable)
    {
        Name = DefaultName;
        Entries = [new ElfSymbol()];
    }

    /// <summary>
    /// Gets a list of <see cref="ElfSymbol"/> entries.
    /// </summary>
    public List<ElfSymbol> Entries { get;  }

    public override void Read(ElfReader reader)
    {
        reader.Position = Position;
        Entries.Clear();

        var numberOfEntries = (int)(base.Size / base.TableEntrySize);
        var entries = Entries;
        CollectionsMarshal.SetCount(entries, numberOfEntries);

        if (_is32)
        {
            Read32(reader, numberOfEntries);
        }
        else
        {
            Read64(reader, numberOfEntries);
        }
    }

    public override void Write(ElfWriter writer)
    {
        if (_is32)
        {
            Write32(writer);
        }
        else
        {
            Write64(writer);
        }
    }

    private void Read32(ElfReader reader, int numberOfEntries)
    {
        using var batch = new BatchDataReader<ElfNative.Elf32_Sym>(reader.Stream, numberOfEntries);
        var span = CollectionsMarshal.AsSpan(Entries);
        ref var entry = ref MemoryMarshal.GetReference(span);
        while (batch.HasNext())
        {
            ref var sym = ref batch.ReadNext();

            entry.Name = new ElfString(reader.Decode(sym.st_name));
            entry.Value = reader.Decode(sym.st_value);
            entry.Size = reader.Decode(sym.st_size);

            var st_info = sym.st_info;
            entry.Type = (ElfSymbolType) (st_info & 0xF);
            entry.Bind = (ElfSymbolBind)(st_info >> 4);
            entry.Visibility = (ElfSymbolVisibility) sym.st_other;
            entry.SectionLink = new ElfSectionLink(reader.Decode(sym.st_shndx));
            entry = ref Unsafe.Add(ref entry, 1);
        }
    }

    private void Read64(ElfReader reader, int numberOfEntries)
    {
        using var batch = new BatchDataReader<ElfNative.Elf64_Sym>(reader.Stream, numberOfEntries);
        var span = CollectionsMarshal.AsSpan(Entries);
        ref var entry = ref MemoryMarshal.GetReference(span);
        while (batch.HasNext())
        {
            ref var sym = ref batch.ReadNext();

            entry.Name = new ElfString(reader.Decode(sym.st_name));
            entry.Value = reader.Decode(sym.st_value);
            entry.Size = reader.Decode(sym.st_size);

            var st_info = sym.st_info;
            entry.Type = (ElfSymbolType)(st_info & 0xF);
            entry.Bind = (ElfSymbolBind)(st_info >> 4);
            entry.Visibility = (ElfSymbolVisibility)sym.st_other;
            entry.SectionLink = new ElfSectionLink(reader.Decode(sym.st_shndx));
            entry = ref Unsafe.Add(ref entry, 1);
        }
    }
    
    private void Write32(ElfWriter writer)
    {
        var stringTable = (ElfStringTable)Link.Section!;

        // Write all entries
        for (int i = 0; i < Entries.Count; i++)
        {
            var entry = Entries[i];

            var sym = new ElfNative.Elf32_Sym();
            writer.Encode(out sym.st_name, (ushort)stringTable.Resolve(entry.Name!).Index);
            writer.Encode(out sym.st_value, (uint)entry.Value);
            writer.Encode(out sym.st_size, (uint)entry.Size);
            sym.st_info = (byte)(((byte) entry.Bind << 4) | (byte) entry.Type);
            sym.st_other = (byte) ((byte) entry.Visibility & 3);
            var sectionIndex = entry.SectionLink.GetIndex();
            writer.Encode(out sym.st_shndx, sectionIndex < ElfNative.SHN_LORESERVE || entry.SectionLink.IsSpecial ? (ElfNative.Elf32_Half)sectionIndex : (ElfNative.Elf32_Half)ElfNative.SHN_XINDEX);

            writer.Write(sym);
        }
    }

    private void Write64(ElfWriter writer)
    {
        var stringTable = (ElfStringTable)Link.Section!;

        for (int i = 0; i < Entries.Count; i++)
        {
            var entry = Entries[i];

            var sym = new ElfNative.Elf64_Sym();
            writer.Encode(out sym.st_name, stringTable.Resolve(entry.Name!).Index);
            writer.Encode(out sym.st_value, entry.Value);
            writer.Encode(out sym.st_size, entry.Size);
            sym.st_info = (byte)(((byte)entry.Bind << 4) | (byte)entry.Type);
            sym.st_other = (byte)((byte)entry.Visibility & 3);
            var sectionIndex = entry.SectionLink.GetIndex();
            writer.Encode(out sym.st_shndx, sectionIndex < ElfNative.SHN_LORESERVE || entry.SectionLink.IsSpecial ? (ElfNative.Elf64_Half)sectionIndex : (ElfNative.Elf64_Half)ElfNative.SHN_XINDEX);

            writer.Write(sym);
        }
    }

    protected override void AfterRead(ElfReader reader)
    {
        // Verify that the link is safe and configured as expected
        Link.TryGetSectionSafe<ElfStringTable>(nameof(ElfSymbolTable), nameof(Link), this, reader.Diagnostics, out var stringTable, ElfSectionType.StringTable);

        for (int i = 0; i < Entries.Count; i++)
        {
            var entry = Entries[i];

            if (stringTable != null)
            {
                if (stringTable.TryResolve(entry.Name, out var newEntry))
                {
                    entry.Name = newEntry;
                }
                else
                {
                    reader.Diagnostics.Error(DiagnosticId.ELF_ERR_InvalidSymbolEntryNameIndex, $"Invalid name index [{entry.Name.Index}] for symbol [{i}] in section [{this}]");
                }
            }

            if (entry.SectionLink.SpecialIndex < ElfNative.SHN_LORESERVE)
            {
                entry.SectionLink = reader.ResolveLink(entry.SectionLink, $"Invalid link section index {{0}} for  symbol table entry [{i}] from symbol table section [{this}]");
            }

            Entries[i] = entry;
        }
    }

    public override void Verify(ElfVisitorContext context)
    {
        var diagnostics = context.Diagnostics;

        bool needsSectionHeaderIndices = false;

        for (int i = 0; i < Entries.Count; i++)
        {
            var entry = Entries[i];

            if (i == 0 && !entry.IsEmpty)
            {
                diagnostics.Error(DiagnosticId.ELF_ERR_InvalidFirstSymbolEntryNonNull, $"Invalid entry #{i} in the {nameof(ElfSymbolTable)} section [{Index}]. The first entry must be null/undefined");
            }

            if (entry.SectionLink.Section != null)
            {
                if (entry.SectionLink.Section.Parent != Parent)
                {
                    diagnostics.Error(DiagnosticId.ELF_ERR_InvalidSymbolEntrySectionParent, $"Invalid section for the symbol entry #{i} in the {nameof(ElfSymbolTable)} section [{Index}]. The section of the entry `{entry}` must the same than this symbol table section");
                }

                needsSectionHeaderIndices |= entry.SectionLink.GetIndex() >= ElfNative.SHN_LORESERVE;
            }
        }

        if (needsSectionHeaderIndices)
        {
            bool foundSectionHeaderIndices = false;
            foreach (ElfSection otherSection in Parent!.Sections)
            {
                if (otherSection is ElfSymbolTableSectionHeaderIndices && otherSection.Link.Section == this)
                {
                    foundSectionHeaderIndices = true;
                    break;
                }
            }

            if (!foundSectionHeaderIndices)
            {
                diagnostics.Error(DiagnosticId.ELF_ERR_MissingSectionHeaderIndices, $"Symbol table [{Name.Value}] references section indexes higher than SHN_LORESERVE and accompanying {nameof(ElfSymbolTableSectionHeaderIndices)} section is missing");
            }
        }
    }

    protected override unsafe void UpdateLayoutCore(ElfVisitorContext context)
    {
        var diagnostics = context.Diagnostics;

        // Verify that the link is safe and configured as expected
        if (!Link.TryGetSectionSafe<ElfStringTable>(nameof(ElfSymbolTable), nameof(Link), this, diagnostics, out var stringTable, ElfSectionType.StringTable))
        {
            return;
        }

        bool isAllowingLocal = true;

        for (int i = 0; i < Entries.Count; i++)
        {
            var entry = Entries[i];
            entry.Name = stringTable.Resolve(entry.Name);


            // Update the last local index
            if (entry.Bind == ElfSymbolBind.Local)
            {
                // + 1 For the plus one
                Info = new ElfSectionLink(i + 1);
                if (!isAllowingLocal)
                {
                    diagnostics.Error(DiagnosticId.ELF_ERR_InvalidSymbolEntryLocalPosition,
                        $"Invalid position for the LOCAL symbol entry #{i} in the {nameof(ElfSymbolTable)} section [{Index}]. A LOCAL symbol entry must be before any other symbol entry");
                }
            }
            else
            {
                isAllowingLocal = false;
            }
        }


        Size = (uint)Entries.Count * TableEntrySize;
    }

    protected override unsafe void ValidateParent(ObjectElement parent)
    {
        base.ValidateParent(parent);
        var elf = (ElfFile)parent;
        _is32 = elf.FileClass == ElfFileClass.Is32;

        BaseTableEntrySize = (uint)(_is32 ? sizeof(ElfNative.Elf32_Sym) : sizeof(ElfNative.Elf64_Sym));
        AdditionalTableEntrySize = 0;
    }

    internal override unsafe void InitializeEntrySizeFromRead(DiagnosticBag diagnostics, ulong entrySize, bool is32)
    {
        _is32 = is32;

        if (is32)
        {
            if (entrySize != (ulong)sizeof(ElfNative.Elf32_Sym))
            {
                diagnostics.Error(DiagnosticId.ELF_ERR_InvalidSectionEntrySize, $"Invalid size [{entrySize}] for symbol entry. Expecting to be equal to [{sizeof(ElfNative.Elf32_Sym)}] bytes");
            }
            else
            {
                BaseTableEntrySize = (uint)sizeof(ElfNative.Elf32_Sym);
                AdditionalTableEntrySize = (uint)(entrySize - AdditionalTableEntrySize);
            }
        }
        else
        {
            if (entrySize != (ulong)sizeof(ElfNative.Elf64_Sym))
            {
                diagnostics.Error(DiagnosticId.ELF_ERR_InvalidSectionEntrySize, $"Invalid size [{entrySize}] for symbol entry. Expecting to be equal to [{sizeof(ElfNative.Elf64_Sym)}] bytes");
            }
            else
            {
                BaseTableEntrySize = (uint)sizeof(ElfNative.Elf64_Sym);
                AdditionalTableEntrySize = (uint)(entrySize - AdditionalTableEntrySize);
            }
        }
    }
}