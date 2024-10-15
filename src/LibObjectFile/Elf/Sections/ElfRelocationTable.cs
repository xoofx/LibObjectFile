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
/// A relocation section with the type <see cref="ElfSectionType.Relocation"/> or <see cref="ElfSectionType.RelocationAddends"/>
/// </summary>
public sealed class ElfRelocationTable : ElfSection
{
    private readonly List<ElfRelocation> _entries;
    private bool _is32;
    public const string DefaultName = ".rel";
    public const string DefaultNameWithAddends = ".rela";

    public ElfRelocationTable() : this(true)
    {
    }

    public ElfRelocationTable(bool relocationWithAddends) : base(relocationWithAddends ? ElfSectionType.RelocationAddends : ElfSectionType.Relocation)
    {
        Name = DefaultNameWithAddends;
        _entries = new List<ElfRelocation>();
    }

    /// <summary>
    /// Gets a list of <see cref="ElfRelocation"/> entries.
    /// </summary>
    public List<ElfRelocation> Entries => _entries;

    public bool IsRelocationWithAddends => this.Type == ElfSectionType.RelocationAddends;

    public override void Read(ElfReader reader)
    {
        reader.Position = Position;
        if (_is32)
        {
            Read32(reader);
        }
        else
        {
            Read64(reader);
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

    private unsafe void Read32(ElfReader reader)
    {
        var numberOfEntries = base.Size / base.TableEntrySize;
        var entries = _entries;
        CollectionsMarshal.SetCount(entries, (int)numberOfEntries);
        var span = CollectionsMarshal.AsSpan(entries);

        if (IsRelocationWithAddends)
        {
            using var batch = new BatchDataReader<ElfNative.Elf32_Rela>(reader.Stream, (int)numberOfEntries);
            ref var entry = ref MemoryMarshal.GetReference(span);
            while (batch.HasNext())
            {
                ref var rel = ref batch.Read();
                entry.Offset = reader.Decode(rel.r_offset);
                var r_info = reader.Decode(rel.r_info);
                entry.Type = new ElfRelocationType(Parent!.Arch, r_info & 0xFF);
                entry.SymbolIndex = r_info >> 8;
                entry.Addend = reader.Decode(rel.r_addend);
                entry = ref Unsafe.Add(ref entry, 1);
            }
        }
        else
        {
            using var batch = new BatchDataReader<ElfNative.Elf32_Rel>(reader.Stream, (int)numberOfEntries);
            ref var entry = ref MemoryMarshal.GetReference(span);
            while (batch.HasNext())
            {
                ref var rel = ref batch.Read();
                entry.Offset = reader.Decode(rel.r_offset);
                var r_info = reader.Decode(rel.r_info);
                entry.Type = new ElfRelocationType(Parent!.Arch, r_info & 0xFF);
                entry.SymbolIndex = r_info >> 8;
                entry.Addend = 0;
                entry = ref Unsafe.Add(ref entry, 1);
            }
        }
    }

    private unsafe void Read64(ElfReader reader)
    {
        var numberOfEntries = base.Size / base.TableEntrySize;
        var entries = _entries;
        CollectionsMarshal.SetCount(entries, (int)numberOfEntries);
        var span = CollectionsMarshal.AsSpan(entries);

        if (IsRelocationWithAddends)
        {
            using var batch = new BatchDataReader<ElfNative.Elf64_Rela>(reader.Stream, (int)numberOfEntries);
            ref var entry = ref MemoryMarshal.GetReference(span);
            while (batch.HasNext())
            {
                ref var rel = ref batch.Read();
                entry.Offset = reader.Decode(rel.r_offset);
                var r_info = reader.Decode(rel.r_info);
                entry.Type = new ElfRelocationType(Parent!.Arch, (uint)(r_info & 0xFFFFFFFF));
                entry.SymbolIndex = (uint)(r_info >> 32);
                entry.Addend = reader.Decode(rel.r_addend);
                entry = ref Unsafe.Add(ref entry, 1);
            }
        }
        else
        {
            using var batch = new BatchDataReader<ElfNative.Elf64_Rel>(reader.Stream, (int)numberOfEntries);
            ref var entry = ref MemoryMarshal.GetReference(span);
            while (batch.HasNext())
            {
                ref var rel = ref batch.Read();
                entry.Offset = reader.Decode(rel.r_offset);
                var r_info = reader.Decode(rel.r_info);
                entry.Type = new ElfRelocationType(Parent!.Arch, (uint)(r_info & 0xFFFFFFFF));
                entry.SymbolIndex = (uint)(r_info >> 32);
                entry.Addend = 0;
                entry = ref Unsafe.Add(ref entry, 1);
            }
        }
    }

    private void Write32(ElfWriter writer)
    {
        var entries = CollectionsMarshal.AsSpan(_entries);
        if (IsRelocationWithAddends)
        {
            using var batch = new BatchDataWriter<ElfNative.Elf32_Rela>(writer.Stream, entries.Length);
            // Write all entries
            var rel = new ElfNative.Elf32_Rela();
            for (int i = 0; i < entries.Length; i++)
            {
                ref var entry = ref entries[i];

                writer.Encode(out rel.r_offset, (uint)entry.Offset);
                uint r_info = entry.Info32;
                writer.Encode(out rel.r_info, r_info);
                writer.Encode(out rel.r_addend, (int)entry.Addend);

                batch.Write(rel);
            }
        }
        else
        {
            using var batch = new BatchDataWriter<ElfNative.Elf32_Rel>(writer.Stream, entries.Length);
            // Write all entries
            var rel = new ElfNative.Elf32_Rel();
            for (int i = 0; i < entries.Length; i++)
            {
                ref var entry = ref entries[i];

                writer.Encode(out rel.r_offset, (uint)entry.Offset);
                uint r_info = entry.Info32;
                writer.Encode(out rel.r_info, r_info);

                batch.Write(rel);
            }
        }
    }

    private void Write64(ElfWriter writer)
    {
        var entries = CollectionsMarshal.AsSpan(_entries);
        if (IsRelocationWithAddends)
        {
            using var batch = new BatchDataWriter<ElfNative.Elf64_Rela>(writer.Stream, entries.Length);
            var rel = new ElfNative.Elf64_Rela();
            // Write all entries
            for (int i = 0; i < entries.Length; i++)
            {
                ref var entry = ref entries[i];

                writer.Encode(out rel.r_offset, entry.Offset);
                ulong r_info = entry.Info64;
                writer.Encode(out rel.r_info, r_info);
                writer.Encode(out rel.r_addend, entry.Addend);

                batch.Write(rel);
            }
        }
        else
        {
            using var batch = new BatchDataWriter<ElfNative.Elf64_Rel>(writer.Stream, entries.Length);
            var rel = new ElfNative.Elf64_Rel();
            // Write all entries
            for (int i = 0; i < entries.Length; i++)
            {
                ref var entry = ref entries[i];

                writer.Encode(out rel.r_offset, (uint)entry.Offset);
                ulong r_info = entry.Info64;
                writer.Encode(out rel.r_info, r_info);

                batch.Write(rel);
            }
        }
    }

    protected override void AfterRead(ElfReader reader)
    {
        var name = Name.Value;

        var defaultName = GetDefaultName(Type);

        if (!name.StartsWith(defaultName))
        {
            reader.Diagnostics.Warning(DiagnosticId.ELF_WRN_InvalidRelocationTablePrefixName, $"The name of the {Type} section `{this}` doesn't start with `{DefaultName}`");
        }
        else
        {
            // Check the name of relocation
            var currentTargetName = name.Substring(defaultName.Length);
            var sectionTargetName = Info.Section?.Name.Value;
            if (sectionTargetName != null && currentTargetName != sectionTargetName)
            {
                reader.Diagnostics.Warning(DiagnosticId.ELF_WRN_InvalidRelocationTablePrefixTargetName, $"Invalid name `{name}` for relocation table  [{Index}] the current link section is named `{sectionTargetName}` so the expected name should be `{defaultName}{sectionTargetName}`", this);
            }
        }
    }

    public override void Verify(ElfVisitorContext context)
    {
        var diagnostics = context.Diagnostics;

        //if (Info.Section == null)
        //{
        //    diagnostics.Error($"Invalid {nameof(Info)} of the section [{Index}] `{nameof(ElfRelocationTable)}` that cannot be null and must point to a valid section", this);
        //}
        //else
        if (Info.Section != null && Info.Section.Parent != Parent)
        {
            diagnostics.Error(DiagnosticId.ELF_ERR_InvalidRelocationInfoParent, $"Invalid parent for the {nameof(Info)} of the section [{Index}] `{nameof(ElfRelocationTable)}`. It must point to the same {nameof(ElfFile)} parent instance than this section parent", this);
        }

        var symbolTable = Link.Section as ElfSymbolTable;

        // Write all entries
        for (int i = 0; i < Entries.Count; i++)
        {
            var entry = Entries[i];
            if (entry.Addend != 0 && !IsRelocationWithAddends)
            {
                diagnostics.Error(DiagnosticId.ELF_ERR_InvalidRelocationEntryAddend, $"Invalid relocation entry {i} in section [{Index}] `{nameof(ElfRelocationTable)}`. The addend != 0 while the section is not a `{ElfSectionType.RelocationAddends}`", this);
            }

            if (entry.Type.Arch != Parent!.Arch)
            {
                diagnostics.Error(DiagnosticId.ELF_ERR_InvalidRelocationEntryArch, $"Invalid Arch `{entry.Type.Arch}` for relocation entry {i} in section [{Index}] `{nameof(ElfRelocationTable)}`. The arch doesn't match the arch `{Parent.Arch}`", this);
            }

            if (symbolTable != null && entry.SymbolIndex > (uint)symbolTable.Entries.Count)
            {
                diagnostics.Error(DiagnosticId.ELF_ERR_InvalidRelocationSymbolIndex, $"Out of range symbol index `{entry.SymbolIndex}` (max: {symbolTable.Entries.Count + 1} from symbol table {symbolTable}) for relocation entry {i} in section [{Index}] `{nameof(ElfRelocationTable)}`", this);
            }
        }
    }

    protected override unsafe void UpdateLayoutCore(ElfVisitorContext context)
    {
        BaseTableEntrySize = (uint)(_is32
            ? (IsRelocationWithAddends ? sizeof(ElfNative.Elf32_Rela) : sizeof(ElfNative.Elf32_Rel))
            : (IsRelocationWithAddends ? sizeof(ElfNative.Elf64_Rela) : sizeof(ElfNative.Elf64_Rel)));

        AdditionalTableEntrySize = 0;

        Size = (ulong)Entries.Count * BaseTableEntrySize;
    }

    protected override void ValidateParent(ObjectElement parent)
    {
        base.ValidateParent(parent);
        var elf = (ElfFile)parent;
        _is32 = elf.FileClass == ElfFileClass.Is32;
    }

    internal override unsafe void InitializeEntrySizeFromRead(DiagnosticBag diagnostics, ulong entrySize, bool is32)
    {
        _is32 = is32;
        if (_is32)
        {
            if (entrySize != (ulong)(IsRelocationWithAddends ? sizeof(ElfNative.Elf32_Rela) : sizeof(ElfNative.Elf32_Rel)))
            {
                diagnostics.Error(DiagnosticId.ELF_ERR_InvalidSectionEntrySize, $"Invalid entry size `{entrySize}` for section [{this}]. The entry size must be == `{sizeof(ElfNative.Elf32_Rela)}` for `{ElfSectionType.RelocationAddends}` or `{sizeof(ElfNative.Elf32_Rel)}` for `{ElfSectionType.Relocation}`", this);
            }
        }
        else
        {
            if (entrySize != (ulong)(IsRelocationWithAddends ? sizeof(ElfNative.Elf64_Rela) : sizeof(ElfNative.Elf64_Rel)))
            {
                diagnostics.Error(DiagnosticId.ELF_ERR_InvalidSectionEntrySize, $"Invalid entry size `{entrySize}` for section [{this}]. The entry size must be == `{sizeof(ElfNative.Elf64_Rela)}` for `{ElfSectionType.RelocationAddends}` or `{sizeof(ElfNative.Elf64_Rel)}` for `{ElfSectionType.Relocation}`", this);
            }
        }

        BaseTableEntrySize = (uint)(_is32
            ? (IsRelocationWithAddends ? sizeof(ElfNative.Elf32_Rela) : sizeof(ElfNative.Elf32_Rel))
            : (IsRelocationWithAddends ? sizeof(ElfNative.Elf64_Rela) : sizeof(ElfNative.Elf64_Rel)));

        AdditionalTableEntrySize = 0;
    }

    private static string GetDefaultName(ElfSectionType type)
    {
        return type == ElfSectionType.Relocation ? DefaultName : DefaultNameWithAddends;
    }
}