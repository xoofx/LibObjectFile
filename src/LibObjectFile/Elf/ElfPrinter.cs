// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.IO;
using System.Text;

namespace LibObjectFile.Elf;

/// <summary>
/// Extensions methods for <see cref="ElfFile"/> to print their layout in text forms, similar to readelf.
/// </summary>
public static class ElfPrinter
{
    /// <summary>
    /// Prints an <see cref="ElfFile"/> to the specified writer.
    /// </summary>
    /// <param name="elf">The object file to print.</param>
    /// <param name="writer">The destination text writer.</param>
    public static void Print(this ElfFile elf, TextWriter writer)
    {
        if (elf == null) throw new ArgumentNullException(nameof(elf));
        if (writer == null) throw new ArgumentNullException(nameof(writer));
        PrintElfHeader(elf, writer);
        PrintSectionHeaders(elf, writer);
        PrintSectionGroups(elf, writer);
        PrintProgramHeaders(elf, writer);
        PrintDynamicSections(elf, writer);
        PrintRelocations(elf, writer);
        PrintUnwind(elf, writer);
        PrintSymbolTables(elf, writer);
        PrintVersionInformation(elf, writer);
        PrintNotes(elf, writer);
    }

    public static void PrintElfHeader(ElfFile elf, TextWriter writer)
    {
        if (elf == null) throw new ArgumentNullException(nameof(elf));
        if (writer == null) throw new ArgumentNullException(nameof(writer));

        Span<byte> ident = stackalloc byte[ElfFile.IdentSizeInBytes];
        elf.CopyIdentTo(ident);

        writer.WriteLine("ELF Header:");

        writer.Write("  Magic:   ");
        foreach (var b in ident)
        {
            writer.Write($"{b:x2} ");
        }
        writer.WriteLine();
        writer.WriteLine($"  Class:                             {GetElfFileClass(elf.FileClass)}");
        writer.WriteLine($"  Data:                              {GetElfEncoding(elf.Encoding)}");
        writer.WriteLine($"  Version:                           {GetElfVersion((byte)elf.Version)}");
        writer.WriteLine($"  OS/ABI:                            {GetElfOsAbi(elf.OSABI)}");
        writer.WriteLine($"  ABI Version:                       {elf.AbiVersion}");
        writer.WriteLine($"  Type:                              {GetElfFileType(elf.FileType)}");
        writer.WriteLine($"  Machine:                           {GetElfArch(elf.Arch)}");
        writer.WriteLine($"  Version:                           0x{elf.Version:x}");
        writer.WriteLine($"  Entry point address:               0x{elf.EntryPointAddress:x}");
        writer.WriteLine($"  Start of program headers:          {elf.Layout.OffsetOfProgramHeaderTable} (bytes into file)");
        writer.WriteLine($"  Start of section headers:          {elf.Layout.OffsetOfSectionHeaderTable} (bytes into file)");
        writer.WriteLine($"  Flags:                             {elf.Flags}");
        writer.WriteLine($"  Size of this header:               {elf.Layout.SizeOfElfHeader} (bytes)");
        writer.WriteLine($"  Size of program headers:           {elf.Layout.SizeOfProgramHeaderEntry} (bytes)");
        writer.WriteLine($"  Number of program headers:         {elf.Segments.Count}");
        writer.WriteLine($"  Size of section headers:           {elf.Layout.SizeOfSectionHeaderEntry} (bytes)");
        if (elf.Sections.Count >= ElfNative.SHN_LORESERVE)
        {
            writer.WriteLine($"  Number of section headers:         0 ({elf.Sections.Count})");
        }
        else
        {
            writer.WriteLine($"  Number of section headers:         {elf.Sections.Count}");
        }
        writer.WriteLine($"  Section header string table index: {elf.SectionHeaderStringTable?.SectionIndex ?? 0}");
    }

    public static void PrintSectionHeaders(ElfFile elf, TextWriter writer)
    {
        if (elf == null) throw new ArgumentNullException(nameof(elf));
        if (writer == null) throw new ArgumentNullException(nameof(writer));

        writer.WriteLine();
        if (elf.Sections.Count == 0)
        {
            writer.WriteLine("There are no sections in this file.");
            return;
        }

        writer.WriteLine(elf.Sections.Count > 1 ? "Section Headers:" : "Section Header:");

        writer.WriteLine("  [Nr] Name              Type            Address          Off    Size   ES Flg Lk Inf Al");
        for (int i = 0; i < elf.Sections.Count; i++)
        {
            var section = elf.Sections[i];
            writer.WriteLine($"  [{section.SectionIndex,2:#0}] {GetElfSectionName(section),-17} {GetElfSectionType(section.Type),-15} {section.VirtualAddress:x16} {section.Position:x6} {section.Size:x6} {section.TableEntrySize:x2} {GetElfSectionFlags(section.Flags),3} {section.Link.GetIndex(),2} {section.Info.GetIndex(),3} {section.VirtualAddressAlignment,2}");
        }
        writer.WriteLine(@"Key to Flags:
  W (write), A (alloc), X (execute), M (merge), S (strings), I (info),
  L (link order), O (extra OS processing required), G (group), T (TLS),
  C (compressed), x (unknown), o (OS specific), E (exclude),
  D (mbind), l (large), p (processor specific)");
    }

    public static void PrintSectionGroups(ElfFile elf, TextWriter writer)
    {
        if (elf == null) throw new ArgumentNullException(nameof(elf));
        if (writer == null) throw new ArgumentNullException(nameof(writer));

        writer.WriteLine();
        writer.WriteLine("There are no section groups in this file.");
        // TODO
    }

    private static string GetElfSectionName(ElfSection section)
    {
        return section.Parent?.SectionHeaderStringTable == null ? "<no-strings>" : section.Name.Value!;
    }

    public static void PrintProgramHeaders(ElfFile elf, TextWriter writer)
    {
        if (elf == null) throw new ArgumentNullException(nameof(elf));
        if (writer == null) throw new ArgumentNullException(nameof(writer));

        writer.WriteLine();

        if (elf.Segments.Count == 0)
        {
            writer.WriteLine("There are no program headers in this file.");
            return;
        }

        writer.WriteLine(elf.Segments.Count > 1 ? "Program Headers:" : "Program Header:");

        writer.WriteLine("  Type           Offset   VirtAddr           PhysAddr           FileSiz  MemSiz   Flg Align");
        for (int i = 0; i < elf.Segments.Count; i++)
        {
            var phdr = elf.Segments[i];
            writer.WriteLine($"  {GetElfSegmentType(phdr.Type),-14} 0x{phdr.Position:x6} 0x{phdr.VirtualAddress:x16} 0x{phdr.PhysicalAddress:x16} 0x{phdr.Size:x6} 0x{phdr.SizeInMemory:x6} {GetElfSegmentFlags(phdr.Flags),3} 0x{phdr.VirtualAddressAlignment:x}");
        }

        if (elf.Segments.Count > 0 && elf.Sections.Count > 0 && elf.SectionHeaderStringTable != null)
        {
            writer.WriteLine();
            writer.WriteLine(" Section to Segment mapping:");
            writer.WriteLine("  Segment Sections...");

            for (int i = 0; i < elf.Segments.Count; i++)
            {
                var segment = elf.Segments[i];
                writer.Write($"   {i:00}     ");

                foreach (var section in elf.Sections)
                {
                    if (IsSectionInSegment(section, segment, true, true))
                    {
                        writer.Write($"{GetElfSectionName(section)} ");
                    }
                }

                writer.WriteLine();
            }
        }
    }

    public static void PrintRelocations(ElfFile elf, TextWriter writer)
    {
        if (elf == null) throw new ArgumentNullException(nameof(elf));
        if (writer == null) throw new ArgumentNullException(nameof(writer));

        bool hasRelocations = false;

        foreach (var section in elf.Sections)
        {
            if (section.Type == ElfSectionType.Relocation || section.Type == ElfSectionType.RelocationAddends)
            {
                hasRelocations = true;
                var relocTable = (ElfRelocationTable) section;

                writer.WriteLine();
                writer.WriteLine($"Relocation section {(elf.SectionHeaderStringTable == null ? "0" : $"'{section.Name}'")} at offset 0x{section.Position:x} contains {relocTable.Entries.Count} {(relocTable.Entries.Count > 1 ? "entries" : "entry")}:");

                if (elf.FileClass == ElfFileClass.Is32)
                {
                    // TODO
                    writer.WriteLine("    Offset             Info             Type               Symbol's Value  Symbol's Name + Addend");
                }
                else
                {
                    writer.WriteLine("    Offset             Info             Type               Symbol's Value  Symbol's Name + Addend");
                }
                foreach (var entry in relocTable.Entries)
                {
                    var symbolTable = relocTable.Link.Section as ElfSymbolTable;
                    if (symbolTable == null) continue;
                    string symbolName = string.Empty;
                    ulong symbolValue = 0;
                    if (entry.SymbolIndex < symbolTable.Entries.Count)
                    {
                        var symbolEntry = symbolTable.Entries[(int) entry.SymbolIndex];
                        symbolName = symbolEntry.Name.Value;
                        symbolValue = symbolEntry.Value;

                        if (string.IsNullOrEmpty(symbolName))
                        {
                            switch (symbolEntry.Type)
                            {
                                case ElfSymbolType.Section:
                                    if (symbolEntry.SectionLink.Section != null)
                                    {
                                        symbolName = symbolEntry.SectionLink.Section.Name.Value;
                                    }
                                    break;
                            }
                        }
                    }

                    if (elf.FileClass == ElfFileClass.Is32)
                    {
                        writer.WriteLine($"{entry.Offset:x8}  {entry.Info32:x8} {entry.Type.Name,-22} {symbolValue:x8} {symbolName} + {entry.Addend:x}");
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(symbolName))
                        {
                            writer.WriteLine($"{entry.Offset:x16}  {entry.Info64:x16} {entry.Type.Name,-22} {"",16}   {entry.Addend:x}");
                        }
                        else
                        {
                            writer.WriteLine($"{entry.Offset:x16}  {entry.Info64:x16} {entry.Type.Name,-22} {symbolValue:x16} {symbolName} + {entry.Addend:x}");
                        }
                    }
                }

            }
        }

        if (!hasRelocations)
        {
            writer.WriteLine();
            writer.WriteLine("There are no relocations in this file.");
        }
    }

    public static void PrintUnwind(ElfFile elf, TextWriter writer)
    {
        if (elf == null) throw new ArgumentNullException(nameof(elf));
        if (writer == null) throw new ArgumentNullException(nameof(writer));

        if (elf.Arch == ElfArchEx.I386 || elf.Arch == ElfArchEx.X86_64)
        {
            writer.WriteLine("No processor specific unwind information to decode");
        }
        else
        {
            writer.WriteLine();
            writer.WriteLine($"The decoding of unwind sections for machine type {GetElfArch(elf.Arch)} is not currently supported.");
        }
    }


    public static void PrintSymbolTables(ElfFile elf, TextWriter writer)
    {
        if (elf == null) throw new ArgumentNullException(nameof(elf));
        if (writer == null) throw new ArgumentNullException(nameof(writer));

        if (elf.Sections.Count == 0)
        {
            writer.WriteLine();
            writer.WriteLine("Dynamic symbol information is not available for displaying symbols.");
            return;
        }

        foreach (var section in elf.Sections)
        {
            if (!(section is ElfSymbolTable symbolTable)) continue;

            writer.WriteLine();
            writer.WriteLine(symbolTable.Entries.Count <= 1
                ? $"Symbol table '{GetElfSectionName(symbolTable)}' contains {symbolTable.Entries.Count} entry:"
                : $"Symbol table '{GetElfSectionName(symbolTable)}' contains {symbolTable.Entries.Count} entries:"
            );

            if (elf.FileClass == ElfFileClass.Is32)
            {
                writer.WriteLine("   Num:    Value  Size Type    Bind   Vis      Ndx Name");
            }
            else
            {
                writer.WriteLine("   Num:    Value          Size Type    Bind   Vis      Ndx Name");
            }

            for (var i = 0; i < symbolTable.Entries.Count; i++)
            {
                var symbol = symbolTable.Entries[i];
                writer.WriteLine($"{i,6}: {symbol.Value:x16} {symbol.Size,5} {GetElfSymbolType(symbol.Type),-7} {GetElfSymbolBind(symbol.Bind),-6} {GetElfSymbolVisibility(symbol.Visibility),-7} {GetElfSymbolLink(symbol.SectionLink),4} {symbol.Name.Value}");
            }
        }
    }

    private static string GetElfSymbolLink(ElfSectionLink link)
    {
        var index = link.GetIndex();
        switch ((uint)index)
        {
            case 0:
                return "UND";
            case ElfNative.SHN_ABS:
                return "ABS";
            case ElfNative.SHN_COMMON:
                return "COMMON";
        }
        return index.ToString();
    }

    public static void PrintNotes(ElfFile elf, TextWriter writer)
    {
        if (elf == null) throw new ArgumentNullException(nameof(elf));
        if (writer == null) throw new ArgumentNullException(nameof(writer));

        foreach (var section in elf.Sections)
        {
            if (!(section is ElfNoteTable noteTable)) continue;

            writer.WriteLine();
            writer.WriteLine($"Displaying notes found in: {noteTable.Name}");

            writer.WriteLine("  Owner\t\tData size\tDescription");
            foreach (var note in noteTable.Entries)
            {
                writer.WriteLine($"  {note.GetName()}\t\t0x{(note.GetDescriptorSize()):x8}\t{GetElfNoteDescription(note)}");
            }
        }
    }

    private static string GetElfNoteDescription(ElfNote note)
    {
        var builder = new StringBuilder();

        if (note.GetName() == "GNU")
        {
            switch (note.GetNoteType().Value)
            {
                case ElfNoteType.GNU_ABI_TAG:
                    builder.Append("NT_GNU_ABI_TAG (ABI version tag)");
                    break;
                case ElfNoteType.GNU_HWCAP:
                    builder.Append("NT_GNU_HWCAP (DSO-supplied software HWCAP info)");
                    break;
                case ElfNoteType.GNU_BUILD_ID:
                    builder.Append("NT_GNU_BUILD_ID (unique build ID bitstring)");
                    break;
                case ElfNoteType.GNU_GOLD_VERSION:
                    builder.Append("NT_GNU_GOLD_VERSION (gold version)");
                    break;
            }
        }
        else
        {
            switch (note.GetNoteType().Value)
            {
                case ElfNoteType.VERSION:
                    builder.Append("NT_VERSION (version)");
                    break;
            }
        }

        if (builder.Length == 0)
        {
            builder.Append($"Unknown note type: (0x{(uint)note.GetNoteType().Value:x8})");
        }

        builder.Append("\t\t");

        builder.Append(note.GetDescriptorAsText());
        return builder.ToString();
    }

    public static void PrintVersionInformation(ElfFile elf, TextWriter writer)
    {
        if (elf == null) throw new ArgumentNullException(nameof(elf));
        if (writer == null) throw new ArgumentNullException(nameof(writer));
        writer.WriteLine();
        writer.WriteLine("No version information found in this file.");
    }

    private static string GetElfSymbolType(ElfSymbolType symbolType)
    {
        switch (symbolType)
        {
            case ElfSymbolType.NoType: return "NOTYPE";
            case ElfSymbolType.Object: return "OBJECT";
            case ElfSymbolType.Function: return "FUNC";
            case ElfSymbolType.Section: return "SECTION";
            case ElfSymbolType.File: return "FILE";
            case ElfSymbolType.Common: return "COMMON";
            case ElfSymbolType.Tls: return "TLS";
            case ElfSymbolType.GnuIndirectFunction:
            case ElfSymbolType.SpecificOS1:
            case ElfSymbolType.SpecificOS2:
                return $"<OS specific>: {(uint)symbolType}";
            case ElfSymbolType.SpecificProcessor0:
            case ElfSymbolType.SpecificProcessor1:
            case ElfSymbolType.SpecificProcessor2:
                return $"<processor specific>: {(uint)symbolType}";
            default:
                return $"<unknown>: {(uint)symbolType}";
        }
    }

    private static string GetElfSymbolBind(ElfSymbolBind symbolBind)
    {
        switch (symbolBind)
        {
            case ElfSymbolBind.Local:
                return "LOCAL";
            case ElfSymbolBind.Global:
                return "GLOBAL";
            case ElfSymbolBind.Weak:
                return "WEAK";
            case ElfSymbolBind.SpecificOS1:
            case ElfSymbolBind.SpecificOS2:
                return $"<OS specific>: {(uint)symbolBind}";
            case ElfSymbolBind.SpecificProcessor0:
            case ElfSymbolBind.SpecificProcessor1:
            case ElfSymbolBind.SpecificProcessor2:
                return $"<processor specific>: {(uint)symbolBind}";
            default:
                return $"<unknown>: {(uint)symbolBind}";
        }
    }

    private static string GetElfSymbolVisibility(ElfSymbolVisibility symbolVisibility)
    {
        return symbolVisibility switch
        {
            ElfSymbolVisibility.Default => "DEFAULT",
            ElfSymbolVisibility.Internal => "INTERNAL",
            ElfSymbolVisibility.Hidden => "HIDDEN",
            ElfSymbolVisibility.Protected => "PROTECTED",
            _ => $"Unrecognized visibility value: {(uint) symbolVisibility}"
        };
    }

    private static bool IsTlsSpecial(ElfSection section, ElfSegment segment)
    {
        return (((section).Flags & ElfSectionFlags.Tls) != 0
                && (section).Type == ElfSectionType.NoBits
                && (segment).Type != ElfSegmentTypeCore.Tls);
    }

    private static ulong GetSectionSize(ElfSection section, ElfSegment segment)
    {
        return IsTlsSpecial(section, segment) ? 0 : section.Size;
    }

    private static bool IsSectionInSegment(ElfSection section, ElfSegment segment, bool checkVirtualAddress, bool isStrict)
    {
        return (( /* Only PT_LOAD, PT_GNU_RELRO and PT_TLS segments can contain	 SHF_TLS sections.  */
                    ((((section).Flags & ElfSectionFlags.Tls) != 0)
                     && (segment.Type == ElfSegmentTypeCore.Tls
                         //|| segment.Type == ElfSegmentTypeCore.GnuRelPro
                         || segment.Type == ElfSegmentTypeCore.Load))
                    /* PT_TLS segment contains only SHF_TLS sections, PT_PHDR no
                       sections at all.  */
                    || (((section).Flags & ElfSectionFlags.Tls) == 0

                        && segment.Type != ElfSegmentTypeCore.Tls

                        && segment.Type != ElfSegmentTypeCore.ProgramHeader))
                /* PT_LOAD and similar segments only have SHF_ALLOC sections.  */
                && !((section.Flags & ElfSectionFlags.Alloc) == 0

                     && (segment.Type == ElfSegmentTypeCore.Load

                         || segment.Type == ElfSegmentTypeCore.Dynamic
                         //|| segment.Type == PT_GNU_EH_FRAME
                         //|| segment.Type == PT_GNU_STACK
                         //|| segment.Type == PT_GNU_RELRO
                         //|| (segment.Type >= PT_GNU_MBIND_LO
                         //&& segment.Type <= PT_GNU_MBIND_HI
                     )))
               /* Any section besides one of type SHT_NOBITS must have file
                  offsets within the segment.  */
               && (section.Type == ElfSectionType.NoBits
                   || ((section).Position >= segment.Position

                       && (!(isStrict)

                           || (section.Position - segment.Position

                               <= segment.Size - 1))

                       && ((section.Position - segment.Position
                            + GetSectionSize(section, segment))

                           <= segment.Size)))
               /* SHF_ALLOC sections must have VMAs within the segment.  */
               && (!(checkVirtualAddress)
                   || (section.Flags & ElfSectionFlags.Alloc) == 0
                   || (section.VirtualAddress >= segment.VirtualAddress

                       && (!(isStrict)

                           || (section.VirtualAddress - segment.VirtualAddress

                               <= segment.SizeInMemory - 1))

                       && ((section.VirtualAddress - segment.VirtualAddress
                            + GetSectionSize(section, segment))

                           <= segment.SizeInMemory)))
               /* No zero size sections at start or end of PT_DYNAMIC nor
                  PT_NOTE.  */
               && ((segment.Type != ElfSegmentTypeCore.Dynamic

                    && segment.Type != ElfSegmentTypeCore.Note)
                   || section.Size != 0
                   || segment.SizeInMemory == 0
                   || ((section.Type == ElfSectionType.NoBits

                        || (section.Position > segment.Position

                            && (section.Position - segment.Position

                                < segment.Size)))

                       && ((section.Flags & ElfSectionFlags.Alloc) == 0

                           || (section.VirtualAddress > segment.VirtualAddress

                               && (section.VirtualAddress - segment.VirtualAddress

                                   < segment.SizeInMemory)))));
    }

    public static void PrintDynamicSections(ElfFile elf, TextWriter writer)
    {
        if (elf == null) throw new ArgumentNullException(nameof(elf));
        if (writer == null) throw new ArgumentNullException(nameof(writer));

        bool is32 = elf.FileClass == ElfFileClass.Is32;
        bool foundDynamic = false;

        foreach (var section in elf.Sections)
        {
            if (section is not ElfDynamicLinkingTable dynamic) continue;
            foundDynamic = true;

            // readelf lists entries up to and including the first DT_NULL terminator, ignoring any
            // trailing padding entries the section may reserve for later editing.
            int count = dynamic.Entries.Count;
            for (int i = 0; i < dynamic.Entries.Count; i++)
            {
                if (dynamic.Entries[i].TagType == ElfDynamicTag.Null)
                {
                    count = i + 1;
                    break;
                }
            }

            writer.WriteLine();
            writer.WriteLine(count == 1
                ? $"Dynamic section at offset 0x{dynamic.Position:x} contains {count} entry:"
                : $"Dynamic section at offset 0x{dynamic.Position:x} contains {count} entries:");
            writer.WriteLine("  Tag        Type                         Name/Value");

            for (int i = 0; i < count; i++)
            {
                var entry = dynamic.Entries[i];
                var tagHex = is32 ? $"0x{(uint)entry.Tag:x8}" : $"0x{(ulong)entry.Tag:x16}";
                var typeField = $" ({GetElfDynamicTagName(entry.Tag)})";
                writer.WriteLine($" {tagHex}{typeField,-22}{GetElfDynamicValue(dynamic, entry)}");
            }
        }

        if (!foundDynamic)
        {
            writer.WriteLine();
            writer.WriteLine("There is no dynamic section in this file.");
        }
    }

    private static string GetElfDynamicValue(ElfDynamicLinkingTable dynamic, ElfDynamic entry)
    {
        var tag = entry.TagType;
        var value = entry.Value;

        if (ElfDynamicLinkingTable.IsStringValueTag(tag) && dynamic.TryGetString(entry, out var name))
        {
            return tag switch
            {
                ElfDynamicTag.Needed => $"Shared library: [{name}]",
                ElfDynamicTag.SoName => $"Library soname: [{name}]",
                ElfDynamicTag.RPath => $"Library rpath: [{name}]",
                ElfDynamicTag.RunPath => $"Library runpath: [{name}]",
                ElfDynamicTag.Config => $"Configuration file: [{name}]",
                ElfDynamicTag.DepAudit => $"Dependency audit library: [{name}]",
                ElfDynamicTag.Audit => $"Audit library: [{name}]",
                _ => $"0x{value:x}",
            };
        }

        switch (tag)
        {
            case ElfDynamicTag.PltRelSz:
            case ElfDynamicTag.RelaSz:
            case ElfDynamicTag.RelaEnt:
            case ElfDynamicTag.StrSz:
            case ElfDynamicTag.SymEnt:
            case ElfDynamicTag.RelSz:
            case ElfDynamicTag.RelEnt:
            case ElfDynamicTag.InitArraySz:
            case ElfDynamicTag.FiniArraySz:
            case ElfDynamicTag.PreInitArraySz:
            case ElfDynamicTag.RelrSz:
            case ElfDynamicTag.RelrEnt:
            case ElfDynamicTag.GnuConflictSz:
            case ElfDynamicTag.GnuLibListSz:
            case ElfDynamicTag.PltPadSz:
            case ElfDynamicTag.MoveSz:
            case ElfDynamicTag.MoveEnt:
            case ElfDynamicTag.SymInSz:
            case ElfDynamicTag.SymInEnt:
                return $"{value} (bytes)";

            case ElfDynamicTag.RelaCount:
            case ElfDynamicTag.RelCount:
            case ElfDynamicTag.VerDefNum:
            case ElfDynamicTag.VerNeedNum:
                return value.ToString();

            case ElfDynamicTag.PltRel:
                return value == (ulong)ElfNative.DT_RELA ? "RELA" : value == (ulong)ElfNative.DT_REL ? "REL" : $"0x{value:x}";

            case ElfDynamicTag.Flags:
                return GetElfDynamicFlags(value);

            case ElfDynamicTag.Flags1:
                return $"Flags:{GetElfDynamicFlags1(value)}";

            default:
                return $"0x{value:x}";
        }
    }

    private static string GetElfDynamicFlags(ulong flags)
    {
        // DF_* flags. Not exposed by ElfNative, so the bit values are inlined here.
        (ulong Bit, string Name)[] known =
        [
            (0x1, "ORIGIN"),
            (0x2, "SYMBOLIC"),
            (0x4, "TEXTREL"),
            (0x8, "BIND_NOW"),
            (0x10, "STATIC_TLS"),
        ];

        var builder = new StringBuilder();
        foreach (var (bit, flagName) in known)
        {
            if ((flags & bit) == 0) continue;
            if (builder.Length > 0) builder.Append(' ');
            builder.Append(flagName);
            flags &= ~bit;
        }

        if (flags != 0)
        {
            if (builder.Length > 0) builder.Append(' ');
            builder.Append($"0x{flags:x}");
        }

        return builder.ToString();
    }

    private static string GetElfDynamicFlags1(ulong flags)
    {
        // DF_1_* flags, in readelf order. Each match is emitted as " NAME" so the caller can prefix "Flags:".
        (ulong Bit, string Name)[] known =
        [
            (0x00000001, "NOW"), (0x00000002, "GLOBAL"), (0x00000004, "GROUP"), (0x00000008, "NODELETE"),
            (0x00000010, "LOADFLTR"), (0x00000020, "INITFIRST"), (0x00000040, "NOOPEN"), (0x00000080, "ORIGIN"),
            (0x00000100, "DIRECT"), (0x00000200, "TRANS"), (0x00000400, "INTERPOSE"), (0x00000800, "NODEFLIB"),
            (0x00001000, "NODUMP"), (0x00002000, "CONFALT"), (0x00004000, "ENDFILTEE"), (0x00008000, "DISPRELDNE"),
            (0x00010000, "DISPRELPND"), (0x00020000, "NODIRECT"), (0x00040000, "IGNMULDEF"), (0x00080000, "NOKSYMS"),
            (0x00100000, "NOHDR"), (0x00200000, "EDITED"), (0x00400000, "NORELOC"), (0x00800000, "SYMINTPOSE"),
            (0x01000000, "GLOBAUDIT"), (0x02000000, "SINGLETON"), (0x04000000, "STUB"), (0x08000000, "PIE"),
            (0x10000000, "KMOD"), (0x20000000, "WEAKFILTER"), (0x40000000, "NOCOMMON"),
        ];

        var builder = new StringBuilder();
        foreach (var (bit, flagName) in known)
        {
            if ((flags & bit) == 0) continue;
            builder.Append(' ');
            builder.Append(flagName);
            flags &= ~bit;
        }

        if (flags != 0)
        {
            builder.Append($" 0x{flags:x}");
        }

        return builder.ToString();
    }

    private static string GetElfDynamicTagName(long tag)
    {
        switch ((ElfDynamicTag)tag)
        {
            case ElfDynamicTag.Null: return "NULL";
            case ElfDynamicTag.Needed: return "NEEDED";
            case ElfDynamicTag.PltRelSz: return "PLTRELSZ";
            case ElfDynamicTag.PltGot: return "PLTGOT";
            case ElfDynamicTag.Hash: return "HASH";
            case ElfDynamicTag.StrTab: return "STRTAB";
            case ElfDynamicTag.SymTab: return "SYMTAB";
            case ElfDynamicTag.Rela: return "RELA";
            case ElfDynamicTag.RelaSz: return "RELASZ";
            case ElfDynamicTag.RelaEnt: return "RELAENT";
            case ElfDynamicTag.StrSz: return "STRSZ";
            case ElfDynamicTag.SymEnt: return "SYMENT";
            case ElfDynamicTag.Init: return "INIT";
            case ElfDynamicTag.Fini: return "FINI";
            case ElfDynamicTag.SoName: return "SONAME";
            case ElfDynamicTag.RPath: return "RPATH";
            case ElfDynamicTag.Symbolic: return "SYMBOLIC";
            case ElfDynamicTag.Rel: return "REL";
            case ElfDynamicTag.RelSz: return "RELSZ";
            case ElfDynamicTag.RelEnt: return "RELENT";
            case ElfDynamicTag.PltRel: return "PLTREL";
            case ElfDynamicTag.Debug: return "DEBUG";
            case ElfDynamicTag.TextRel: return "TEXTREL";
            case ElfDynamicTag.JmpRel: return "JMPREL";
            case ElfDynamicTag.BindNow: return "BIND_NOW";
            case ElfDynamicTag.InitArray: return "INIT_ARRAY";
            case ElfDynamicTag.FiniArray: return "FINI_ARRAY";
            case ElfDynamicTag.InitArraySz: return "INIT_ARRAYSZ";
            case ElfDynamicTag.FiniArraySz: return "FINI_ARRAYSZ";
            case ElfDynamicTag.RunPath: return "RUNPATH";
            case ElfDynamicTag.Flags: return "FLAGS";
            // DT_ENCODING (32) aliases DT_PREINIT_ARRAY; readelf reports 32 as PREINIT_ARRAY.
            case ElfDynamicTag.PreInitArray: return "PREINIT_ARRAY";
            case ElfDynamicTag.PreInitArraySz: return "PREINIT_ARRAYSZ";
            case ElfDynamicTag.SymtabShndx: return "SYMTAB_SHNDX";
            case ElfDynamicTag.RelrSz: return "RELRSZ";
            case ElfDynamicTag.Relr: return "RELR";
            case ElfDynamicTag.RelrEnt: return "RELRENT";
            case ElfDynamicTag.GnuPrelinked: return "GNU_PRELINKED";
            case ElfDynamicTag.GnuConflictSz: return "GNU_CONFLICTSZ";
            case ElfDynamicTag.GnuLibListSz: return "GNU_LIBLISTSZ";
            case ElfDynamicTag.Checksum: return "CHECKSUM";
            case ElfDynamicTag.PltPadSz: return "PLTPADSZ";
            case ElfDynamicTag.MoveEnt: return "MOVEENT";
            case ElfDynamicTag.MoveSz: return "MOVESZ";
            case ElfDynamicTag.Feature1: return "FEATURE_1";
            case ElfDynamicTag.PosFlag1: return "POSFLAG_1";
            case ElfDynamicTag.SymInSz: return "SYMINSZ";
            case ElfDynamicTag.SymInEnt: return "SYMINENT";
            case ElfDynamicTag.GnuHash: return "GNU_HASH";
            case ElfDynamicTag.TlsDescPlt: return "TLSDESC_PLT";
            case ElfDynamicTag.TlsDescGot: return "TLSDESC_GOT";
            case ElfDynamicTag.GnuConflict: return "GNU_CONFLICT";
            case ElfDynamicTag.GnuLibList: return "GNU_LIBLIST";
            case ElfDynamicTag.Config: return "CONFIG";
            case ElfDynamicTag.DepAudit: return "DEPAUDIT";
            case ElfDynamicTag.Audit: return "AUDIT";
            case ElfDynamicTag.PltPad: return "PLTPAD";
            case ElfDynamicTag.MoveTab: return "MOVETAB";
            case ElfDynamicTag.SymInfo: return "SYMINFO";
            case ElfDynamicTag.VerSym: return "VERSYM";
            case ElfDynamicTag.RelaCount: return "RELACOUNT";
            case ElfDynamicTag.RelCount: return "RELCOUNT";
            case ElfDynamicTag.Flags1: return "FLAGS_1";
            case ElfDynamicTag.VerDef: return "VERDEF";
            case ElfDynamicTag.VerDefNum: return "VERDEFNUM";
            case ElfDynamicTag.VerNeed: return "VERNEED";
            case ElfDynamicTag.VerNeedNum: return "VERNEEDNUM";
            default:
                if (tag >= ElfNative.DT_LOOS && tag <= ElfNative.DT_HIOS) return $"<OS specific: 0x{tag:x}>";
                if (tag >= ElfNative.DT_LOPROC && tag <= ElfNative.DT_HIPROC) return $"<processor specific: 0x{tag:x}>";
                return $"<unknown>: 0x{tag:x}";
        }
    }

    private static string GetElfSegmentFlags(ElfSegmentFlags flags)
    {
        if (flags.Value == 0) return string.Empty;

        var builder = new StringBuilder();
        builder.Append((flags.Value & ElfNative.PF_R) != 0 ? 'R' : ' ');
        builder.Append((flags.Value & ElfNative.PF_W) != 0 ? 'W' : ' ');
        builder.Append((flags.Value & ElfNative.PF_X) != 0 ? 'E' : ' ');
        // TODO: other flags
        return builder.ToString();
    }

    public static string GetElfSegmentType(ElfSegmentType segmentType)
    {
        return segmentType.Value switch
        {
            ElfNative.PT_NULL => "NULL",
            ElfNative.PT_LOAD => "LOAD",
            ElfNative.PT_DYNAMIC => "DYNAMIC",
            ElfNative.PT_INTERP => "INTERP",
            ElfNative.PT_NOTE => "NOTE",
            ElfNative.PT_SHLIB => "SHLIB",
            ElfNative.PT_PHDR => "PHDR",
            ElfNative.PT_TLS => "TLS",
            ElfNative.PT_GNU_EH_FRAME => "GNU_EH_FRAME",
            ElfNative.PT_GNU_STACK => "GNU_STACK",
            ElfNative.PT_GNU_RELRO => "GNU_RELRO",
            _ => $"<unknown>: {segmentType.Value:x}"
        };
    }

    private static string GetElfSectionFlags(ElfSectionFlags flags)
    {
        if (flags == ElfSectionFlags.None) return string.Empty;

        var builder = new StringBuilder();
        if ((flags & ElfSectionFlags.Write) != 0) builder.Append('W');
        if ((flags & ElfSectionFlags.Alloc) != 0) builder.Append('A');
        if ((flags & ElfSectionFlags.Executable) != 0) builder.Append('X');
        if ((flags & ElfSectionFlags.Merge) != 0) builder.Append('M');
        if ((flags & ElfSectionFlags.Strings) != 0) builder.Append('S');
        if ((flags & ElfSectionFlags.InfoLink) != 0) builder.Append('I');
        if ((flags & ElfSectionFlags.LinkOrder) != 0) builder.Append('L');
        if ((flags & ElfSectionFlags.OsNonConforming) != 0) builder.Append('O');
        if ((flags & ElfSectionFlags.Group) != 0) builder.Append('G');
        if ((flags & ElfSectionFlags.Tls) != 0) builder.Append('T');
        if ((flags & ElfSectionFlags.Compressed) != 0) builder.Append('C');

        // TODO: unknown, OS specific, Exclude...etc.
        return builder.ToString();
    }

    private static string GetElfSectionType(ElfSectionType sectionType)
    {
        switch (sectionType)
        {
            case ElfSectionType.Null:
                return "NULL";
            case ElfSectionType.ProgBits:
                return "PROGBITS";
            case ElfSectionType.SymbolTable:
                return "SYMTAB";
            case ElfSectionType.StringTable:
                return "STRTAB";
            case ElfSectionType.RelocationAddends:
                return "RELA";
            case ElfSectionType.SymbolHashTable:
                return "HASH";
            case ElfSectionType.DynamicLinking:
                return "DYNAMIC";
            case ElfSectionType.Note:
                return "NOTE";
            case ElfSectionType.NoBits:
                return "NOBITS";
            case ElfSectionType.Relocation:
                return "REL";
            case ElfSectionType.Shlib:
                return "SHLIB";
            case ElfSectionType.DynamicLinkerSymbolTable:
                return "DYNSYM";
            case ElfSectionType.InitArray:
                return "INIT_ARRAY";
            case ElfSectionType.FiniArray:
                return "FINI_ARRAY";
            case ElfSectionType.PreInitArray:
                return "PREINIT_ARRAY";
            case ElfSectionType.Group:
                return "GROUP";
            case ElfSectionType.SymbolTableSectionHeaderIndices:
                return "SYMTAB SECTION INDICIES";
            case ElfSectionType.RelativeRelocation:
                return "RELR";
            case ElfSectionType.GnuHash:
                return "GNU_HASH";
            case ElfSectionType.GnuLibList:
                return "GNU_LIBLIST";
            case ElfSectionType.GnuAttributes:
                return "GNU_ATTRIBUTES";
            case ElfSectionType.Checksum:
                return "CHECKSUM";
            case ElfSectionType.GnuVersionDefinition:
            case (ElfSectionType)0x6ffffffc:
                return "VERDEF";
            case ElfSectionType.GnuVersionNeedsSection:
                return "VERNEED";
            case ElfSectionType.GnuVersionSymbolTable:
            case (ElfSectionType)0x6ffffff0:
                return "VERSYM";
            case (ElfSectionType)0x7ffffffd:
                return "AUXILIARY";
            case (ElfSectionType)0x7fffffff:
                return "FILTER";
            default:
                return $"{(uint)sectionType:x8}: <unknown>";
        }
    }


    private static string GetElfFileClass(ElfFileClass fileClass)
    {
        switch (fileClass)
        {
            case ElfFileClass.None:
                return "none";
            case ElfFileClass.Is32:
                return "ELF32";
            case ElfFileClass.Is64:
                return "ELF64";
            default:
                return $"<unknown: {(int) fileClass:x}>";
        }
    }

    private static string GetElfEncoding(ElfEncoding encoding)
    {
        return encoding switch
        {
            ElfEncoding.None => "none",
            ElfEncoding.Lsb => "2's complement, little endian",
            ElfEncoding.Msb => "2's complement, big endian",
            _ => $"<unknown: {(int) encoding:x}>"
        };
    }

    private static string GetElfVersion(byte version)
    {
        return version switch
        {
            ElfNative.EV_CURRENT => $"{version} (current)",
            ElfNative.EV_NONE => "",
            _ => $"{version}  <unknown>",
        };
    }


    private static string GetElfOsAbi(ElfOSABIEx osabi)
    {
        return osabi.Value switch
        {
            ElfOSABI.NONE => "UNIX - System V",
            ElfOSABI.HPUX => "UNIX - HP-UX",
            ElfOSABI.NETBSD => "UNIX - NetBSD",
            ElfOSABI.GNU => "UNIX - GNU",
            ElfOSABI.SOLARIS => "UNIX - Solaris",
            ElfOSABI.AIX => "UNIX - AIX",
            ElfOSABI.IRIX => "UNIX - IRIX",
            ElfOSABI.FREEBSD => "UNIX - FreeBSD",
            ElfOSABI.TRU64 => "UNIX - TRU64",
            ElfOSABI.MODESTO => "Novell - Modesto",
            ElfOSABI.OPENBSD => "UNIX - OpenBSD",
            _ => $"<unknown: {osabi.Value:x}"
        };
    }
    static string GetElfFileType(ElfFileType fileType)
    {
        switch (fileType)
        {
            case ElfFileType.None: return "NONE (None)";
            case ElfFileType.Relocatable: return "REL (Relocatable file)";
            case ElfFileType.Executable: return "EXEC (Executable file)";
            case ElfFileType.Dynamic: return "DYN (Shared object file)";
            case ElfFileType.Core: return "CORE (Core file)";
            default:
                var e_type = (ushort) fileType;
                if (e_type >= ElfNative.ET_LOPROC && e_type <= ElfNative.ET_HIPROC) return $"Processor Specific: ({e_type:x})";
                else if (e_type >= ElfNative.ET_LOOS && e_type <= ElfNative.ET_HIOS)
                    return $"OS Specific: ({e_type:x})";
                else
                    return $"<unknown>: {e_type:x}";
        }
    }

    static string GetElfArch(ElfArchEx arch)
    {
        switch ((ushort)arch.Value)
        {
            case ElfNative.EM_NONE: return "None";
            case ElfNative.EM_M32: return "WE32100";
            case ElfNative.EM_SPARC: return "Sparc";
            case ElfNative.EM_386: return "Intel 80386";
            case ElfNative.EM_68K: return "MC68000";
            case ElfNative.EM_88K: return "MC88000";
            case ElfNative.EM_860: return "Intel 80860";
            case ElfNative.EM_MIPS: return "MIPS R3000";
            case ElfNative.EM_S370: return "IBM System/370";
            case ElfNative.EM_MIPS_RS3_LE: return "MIPS R4000 big-endian";
            case ElfNative.EM_PARISC: return "HPPA";
            case ElfNative.EM_SPARC32PLUS: return "Sparc v8+";
            case ElfNative.EM_960: return "Intel 80960";
            case ElfNative.EM_PPC: return "PowerPC";
            case ElfNative.EM_PPC64: return "PowerPC64";
            case ElfNative.EM_S390: return "IBM S/390";
            case ElfNative.EM_V800: return "Renesas V850 (using RH850 ABI)";
            case ElfNative.EM_FR20: return "Fujitsu FR20";
            case ElfNative.EM_RH32: return "TRW RH32";
            case ElfNative.EM_ARM: return "ARM";
            case ElfNative.EM_SH: return "Renesas / SuperH SH";
            case ElfNative.EM_SPARCV9: return "Sparc v9";
            case ElfNative.EM_TRICORE: return "Siemens Tricore";
            case ElfNative.EM_ARC: return "ARC";
            case ElfNative.EM_H8_300: return "Renesas H8/300";
            case ElfNative.EM_H8_300H: return "Renesas H8/300H";
            case ElfNative.EM_H8S: return "Renesas H8S";
            case ElfNative.EM_H8_500: return "Renesas H8/500";
            case ElfNative.EM_IA_64: return "Intel IA-64";
            case ElfNative.EM_MIPS_X: return "Stanford MIPS-X";
            case ElfNative.EM_COLDFIRE: return "Motorola Coldfire";
            case ElfNative.EM_68HC12: return "Motorola MC68HC12 Microcontroller";
            case ElfNative.EM_MMA: return "Fujitsu Multimedia Accelerator";
            case ElfNative.EM_PCP: return "Siemens PCP";
            case ElfNative.EM_NCPU: return "Sony nCPU embedded RISC processor";
            case ElfNative.EM_NDR1: return "Denso NDR1 microprocesspr";
            case ElfNative.EM_STARCORE: return "Motorola Star*Core processor";
            case ElfNative.EM_ME16: return "Toyota ME16 processor";
            case ElfNative.EM_ST100: return "STMicroelectronics ST100 processor";
            case ElfNative.EM_TINYJ: return "Advanced Logic Corp. TinyJ embedded processor";
            case ElfNative.EM_X86_64: return "Advanced Micro Devices X86-64";
            case ElfNative.EM_PDSP: return "Sony DSP processor";
            case ElfNative.EM_FX66: return "Siemens FX66 microcontroller";
            case ElfNative.EM_ST9PLUS: return "STMicroelectronics ST9+ 8/16 bit microcontroller";
            case ElfNative.EM_ST7: return "STMicroelectronics ST7 8-bit microcontroller";
            case ElfNative.EM_68HC16: return "Motorola MC68HC16 Microcontroller";
            case ElfNative.EM_68HC11: return "Motorola MC68HC11 Microcontroller";
            case ElfNative.EM_68HC08: return "Motorola MC68HC08 Microcontroller";
            case ElfNative.EM_68HC05: return "Motorola MC68HC05 Microcontroller";
            case ElfNative.EM_SVX: return "Silicon Graphics SVx";
            case ElfNative.EM_ST19: return "STMicroelectronics ST19 8-bit microcontroller";
            case ElfNative.EM_VAX: return "Digital VAX";
            case ElfNative.EM_CRIS: return "Axis Communications 32-bit embedded processor";
            case ElfNative.EM_JAVELIN: return "Infineon Technologies 32-bit embedded cpu";
            case ElfNative.EM_FIREPATH: return "Element 14 64-bit DSP processor";
            case ElfNative.EM_ZSP: return "LSI Logic's 16-bit DSP processor";
            case ElfNative.EM_MMIX: return "Donald Knuth's educational 64-bit processor";
            case ElfNative.EM_HUANY: return "Harvard Universitys's machine-independent object format";
            case ElfNative.EM_PRISM: return "Vitesse Prism";
            case ElfNative.EM_AVR: return "Atmel AVR 8-bit microcontroller";
            case ElfNative.EM_FR30: return "Fujitsu FR30";
            case ElfNative.EM_D10V: return "d10v";
            case ElfNative.EM_D30V: return "d30v";
            case ElfNative.EM_V850: return "Renesas V850";
            case ElfNative.EM_M32R: return "Renesas M32R (formerly Mitsubishi M32r)";
            case ElfNative.EM_MN10300: return "mn10300";
            case ElfNative.EM_MN10200: return "mn10200";
            case ElfNative.EM_PJ: return "picoJava";
            case ElfNative.EM_XTENSA: return "Tensilica Xtensa Processor";
            default:
                return $"<unknown>: 0x{arch.Value:x}";
        }
    }
}