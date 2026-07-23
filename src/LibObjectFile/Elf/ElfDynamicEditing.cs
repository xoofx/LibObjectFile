// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibObjectFile.Diagnostics;

namespace LibObjectFile.Elf;

/// <summary>
/// Address-preserving edits of a dynamically-linked <see cref="ElfFile"/>.
/// </summary>
/// <remarks>
/// These operations never change the byte offset of an existing section, so existing symbols, relocations and
/// code stay valid. The sections that must grow (<c>.dynamic</c> and <c>.dynstr</c>) are relocated to the end of
/// the file and mapped by a fresh <c>PT_LOAD</c> built from a spare <c>PT_NOTE</c> program header (so the
/// program-header table does not grow). This is the same technique used by <c>patchelf --add-needed</c>.
/// </remarks>
public static class ElfDynamicEditing
{
    private const ulong PageSize = 0x1000;

    /// <summary>
    /// Injects a <c>DT_NEEDED</c> dependency on <paramref name="libraryName"/> into an existing dynamically-linked
    /// image, so the dynamic loader loads that library at startup. The <c>.dynamic</c> and <c>.dynstr</c> sections
    /// are relocated (as sections, kept consistent with <c>PT_DYNAMIC</c> and <c>DT_STRTAB</c>), while every other
    /// section keeps its byte offset. The vacated space is backfilled so no existing reference shifts.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The file is not dynamically linked, or has no <c>PT_NOTE</c> segment to repurpose as the new <c>PT_LOAD</c>.
    /// </exception>
    public static void AddNeededLibrary(this ElfFile elf, string libraryName)
    {
        ArgumentNullException.ThrowIfNull(elf);
        ArgumentException.ThrowIfNullOrEmpty(libraryName);

        var dynamicSection = elf.Sections.OfType<ElfDynamicLinkingTable>().FirstOrDefault()
            ?? throw new InvalidOperationException("The ELF file has no dynamic section (.dynamic); it is not dynamically linked.");
        var stringSection = dynamicSection.StringTable
            ?? throw new InvalidOperationException("The dynamic section is not linked to a string table (.dynstr).");
        var dynamicSegment = elf.Segments.FirstOrDefault(s => s.Type == ElfSegmentTypeCore.Dynamic)
            ?? throw new InvalidOperationException("The ELF file has no PT_DYNAMIC segment.");
        var noteSegment = elf.Segments.FirstOrDefault(s => s.Type == ElfSegmentTypeCore.Note)
            ?? throw new InvalidOperationException("The ELF file has no PT_NOTE segment to repurpose as a new PT_LOAD.");

        // Original layout of the sections we relocate, captured before any mutation.
        ulong dynamicOldVaddr = dynamicSection.VirtualAddress;
        ulong stringOldVaddr = stringSection.VirtualAddress;
        ulong dynamicOldSize = dynamicSection.Size;
        ulong stringOldSize = stringSection.Size;
        uint dynamicOldAlign = dynamicSection.FileAlignment;
        uint stringOldAlign = stringSection.FileAlignment;

        // Grow .dynstr with the library name and add the DT_NEEDED entry to .dynamic.
        dynamicSection.AddNeededLibrary(libraryName);

        // Relocate .dynamic and .dynstr to end-of-file, backfilling their old positions with equal-size padding
        // so every other section keeps its byte offset. .dynamic goes first so PT_DYNAMIC covers the start of the
        // new region; page-align it so the new PT_LOAD is vaddr/offset congruent.
        RelocateToEnd(elf, dynamicSection, dynamicOldSize, dynamicOldAlign, (uint)PageSize);
        RelocateToEnd(elf, stringSection, stringOldSize, stringOldAlign, 1);

        var diagnostics = new DiagnosticBag();
        elf.UpdateLayout(diagnostics);
        if (diagnostics.HasErrors)
        {
            throw new ObjectFileException("Failed to update the ELF layout while injecting a dependency.", diagnostics);
        }

        ulong baseVaddr = AlignUp(MaxLoadedVirtualAddressEnd(elf), PageSize);
        ulong firstPosition = dynamicSection.Position;
        dynamicSection.VirtualAddress = baseVaddr + (dynamicSection.Position - firstPosition);
        stringSection.VirtualAddress = baseVaddr + (stringSection.Position - firstPosition);

        // Point DT_STRTAB/DT_STRSZ at the relocated .dynstr. These are value-only updates (no entry count change),
        // so the layout computed above stays valid.
        UpdateTagValue(dynamicSection.Entries, ElfDynamicTag.StrTab, stringSection.VirtualAddress);
        UpdateTagValue(dynamicSection.Entries, ElfDynamicTag.StrSz, stringSection.Size);

        // The _DYNAMIC symbol (and any other symbol inside the moved sections) must follow the new address.
        RelocateSymbols(elf, dynamicSection, dynamicOldVaddr);
        RelocateSymbols(elf, stringSection, stringOldVaddr);

        // Repurpose the PT_NOTE program header into a writable PT_LOAD mapping both relocated sections. It must be
        // writable because the loader writes DT_DEBUG back into .dynamic.
        noteSegment.Type = ElfSegmentTypeCore.Load;
        noteSegment.Flags = ElfSegmentFlagsCore.Readable | ElfSegmentFlagsCore.Writable;
        noteSegment.VirtualAddress = baseVaddr;
        noteSegment.PhysicalAddress = baseVaddr;
        noteSegment.VirtualAddressAlignment = PageSize;
        noteSegment.Range = new ElfContentRange(dynamicSection, 0, stringSection, 0);
        noteSegment.SizeInMemory = stringSection.Position + stringSection.Size - dynamicSection.Position;

        // Point PT_DYNAMIC at the relocated .dynamic.
        dynamicSegment.VirtualAddress = dynamicSection.VirtualAddress;
        dynamicSegment.PhysicalAddress = dynamicSection.VirtualAddress;
        dynamicSegment.SizeInMemory = dynamicSection.Size;
        dynamicSegment.Range = new ElfContentRange(dynamicSection);
    }

    /// <summary>
    /// Moves <paramref name="section"/> to the end of the content list and backfills the space it vacated with a
    /// zero-filled shadow content of the same size and alignment, so every following section keeps its byte offset.
    /// The section header keeps its index (driven by <see cref="ElfSection.OrderInSectionHeaderTable"/>), only its
    /// file offset moves.
    /// </summary>
    private static void RelocateToEnd(ElfFile elf, ElfSection section, ulong originalSize, uint originalAlignment, uint appendAlignment)
    {
        // RemoveAt (not Remove) is required: only RemoveAt fires the content hooks that keep the section list
        // in sync, otherwise the section would be registered twice and produce a duplicate section header.
        int index = elf.Content.IndexOf(section);
        elf.Content.RemoveAt(index);
        var padding = new ElfStreamContentData(new MemoryStream(new byte[originalSize]))
        {
            FileAlignment = originalAlignment,
        };
        elf.Content.Insert(index, padding);
        section.FileAlignment = appendAlignment;
        elf.Content.Add(section);
    }

    private static void RelocateSymbols(ElfFile elf, ElfSection section, ulong oldVirtualAddress)
    {
        ulong newVirtualAddress = section.VirtualAddress;
        if (newVirtualAddress == oldVirtualAddress) return;

        foreach (var symbolTable in elf.Sections.OfType<ElfSymbolTable>())
        {
            var entries = symbolTable.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (ReferenceEquals(entry.SectionLink.Section, section))
                {
                    entry.Value = newVirtualAddress + (entry.Value - oldVirtualAddress);
                    entries[i] = entry;
                }
            }
        }
    }

    private static void UpdateTagValue(List<ElfDynamic> entries, ElfDynamicTag tag, ulong value)
    {
        int index = entries.FindIndex(e => e.TagType == tag);
        if (index < 0)
        {
            throw new InvalidOperationException($"The dynamic section is missing the required {tag} entry.");
        }

        var entry = entries[index];
        entry.Value = value;
        entries[index] = entry;
    }

    private static ulong MaxLoadedVirtualAddressEnd(ElfFile elf)
    {
        ulong max = 0;
        foreach (var segment in elf.Segments)
        {
            if (segment.SizeInMemory == 0) continue;
            max = Math.Max(max, segment.VirtualAddress + segment.SizeInMemory);
        }
        foreach (var section in elf.Sections)
        {
            if ((section.Flags & ElfSectionFlags.Alloc) == 0) continue;
            max = Math.Max(max, section.VirtualAddress + section.Size);
        }
        return max;
    }

    private static ulong AlignUp(ulong value, ulong align) => (value + align - 1) & ~(align - 1);
}
