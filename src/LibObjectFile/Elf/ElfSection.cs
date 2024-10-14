// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Diagnostics;
using System.Text;
using LibObjectFile.Diagnostics;

namespace LibObjectFile.Elf;

/// <summary>
/// Defines the base class for a section in an <see cref="ElfFile"/>.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public abstract class ElfSection : ElfContent
{
    protected ElfSection(ElfSectionType sectionType)
    {
        Type = sectionType;
        SectionIndex = -1;
    }

    /// <summary>
    /// Gets the type of this section
    /// </summary>
    public ElfSectionType Type { get; }

    /// <summary>
    /// Gets or sets the <see cref="ElfSectionFlags"/> of this section.
    /// </summary>
    public ElfSectionFlags Flags { get; set; }

    /// <summary>
    /// Gets or sets the name of this section.
    /// </summary>
    public ElfString Name { get; set; }

    /// <summary>
    /// Gets or sets the virtual address of this section.
    /// </summary>
    public ulong VirtualAddress { get; set; }

    /// <summary>
    /// Gets or sets the alignment requirement of this section.
    /// </summary>
    /// <remarks>
    /// An alignment of zero or 1 means that the section or segment has no alignment constraints.
    /// </remarks>
    public ulong VirtualAddressAlignment { get; set; }

    /// <summary>
    /// Gets or sets the link element of this section.
    /// </summary>
    public ElfSectionLink Link { get; set; }

    /// <summary>
    /// Gets or sets the info element of this section.
    /// </summary>
    public ElfSectionLink Info { get; set; }

    /// <summary>
    /// Gets the index of the sections in <see cref="ElfFile.Sections"/>
    /// </summary>
    public int SectionIndex { get; internal set; }

    /// <summary>
    /// Gets or sets an order of this section in the header table.
    /// </summary>
    /// <remarks>
    /// If this index is changed, you need to call <see cref="ElfFile.UpdateLayout(LibObjectFile.Diagnostics.DiagnosticBag)"/> to update the layout of the file.
    /// </remarks>
    public uint OrderInSectionHeaderTable { get; set; }

    /// <summary>
    /// Gets the default size of the table entry size of this section.
    /// </summary>
    /// <remarks>
    /// Depending on the type of the section, this value might be automatically updated after calling <see cref="ElfFile.UpdateLayout(LibObjectFile.Diagnostics.DiagnosticBag)"/>
    /// </remarks>
    public uint BaseTableEntrySize { get; protected set; }

    /// <summary>
    /// Gets the size of the table entry size of this section. Which is the sum of <see cref="BaseTableEntrySize"/> and <see cref="AdditionalTableEntrySize"/>.
    /// </summary>
    public ulong TableEntrySize => BaseTableEntrySize + AdditionalTableEntrySize;

    /// <summary>
    /// Gets or sets the additional entry size of this section.
    /// </summary>
    public uint AdditionalTableEntrySize { get; set; }

    /// <summary>
    /// Gets a boolean indicating if this section has some content (Size should be taken into account).
    /// </summary>
    public bool HasContent => Type != ElfSectionType.NoBits && Type != ElfSectionType.Null;

    public override void Verify(ElfVisitorContext context)
    {
        var diagnostics = context.Diagnostics;

        // Check parent for link section
        if (Link.Section != null)
        {
            if (Link.Section.Parent != this.Parent)
            {
                diagnostics.Error(DiagnosticId.ELF_ERR_InvalidSectionLinkParent, $"Invalid parent for {nameof(Link)}: `{Link}` used by section `{this}`. The {nameof(Link)}.{nameof(ElfSectionLink.Section)} must have the same parent {nameof(ElfFile)} than this section");
            }
        }

        if (Info.Section != null)
        {
            if (Info.Section.Parent != this.Parent)
            {
                diagnostics.Error(DiagnosticId.ELF_ERR_InvalidSectionInfoParent, $"Invalid parent for {nameof(Info)}: `{Info}` used by section `{this}`. The {nameof(Info)}.{nameof(ElfSectionLink.Section)} must have the same parent {nameof(ElfFile)} than this section");
            }
        }

        // Verify that Link is correctly setup for this section
        switch (Type)
        {
            case ElfSectionType.DynamicLinking:
            case ElfSectionType.DynamicLinkerSymbolTable:
            case ElfSectionType.SymbolTable:
                Link.TryGetSectionSafe<ElfStringTable>(this.GetType().Name, nameof(Link), this, diagnostics, out _, ElfSectionType.StringTable);
                break;
            case ElfSectionType.SymbolHashTable:
            case ElfSectionType.Relocation:
            case ElfSectionType.RelocationAddends:
                Link.TryGetSectionSafe<ElfSymbolTable>(this.GetType().Name, nameof(Link), this, diagnostics, out _, ElfSectionType.SymbolTable, ElfSectionType.DynamicLinkerSymbolTable);
                break;
        }
    }

    protected virtual void AfterRead(ElfReader reader)
    {
    }

    protected virtual void BeforeWrite(ElfWriter writer)
    {
    }

    protected override bool PrintMembers(StringBuilder builder)
    {
        builder.Append($"Section [{SectionIndex}](Internal: {Index}) `{Name}`, ");
        base.PrintMembers(builder);
        return true;
    }

    public sealed override void UpdateLayout(ElfVisitorContext context)
    {
        Name = context.ResolveName(Name);
        UpdateLayoutCore(context);
    }

    internal void BeforeWriteInternal(ElfWriter writer)
    {
        BeforeWrite(writer);
    }

    internal void AfterReadInternal(ElfReader reader)
    {
        AfterRead(reader);
    }

    internal virtual void InitializeEntrySizeFromRead(DiagnosticBag diagnostics, ulong entrySize, bool is32)
    {
        if (entrySize > uint.MaxValue)
        {
            diagnostics.Error(DiagnosticId.ELF_ERR_InvalidSectionEntrySize, $"Invalid entry size [{entrySize}] for section `{this}`. The entry size must be less than or equal to {uint.MaxValue}");
        }

        AdditionalTableEntrySize = (uint)entrySize;
    }
}