// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibObjectFile.Diagnostics;
using LibObjectFile.Elf;

namespace LibObjectFile.Dwarf;

public class DwarfElfContext : VisitorContextBase<ElfFile>
{
    private readonly int _codeSectionSymbolIndex;
    private int _infoSectionSymbolIndex;
    private int _abbreviationTableSymbolIndex;
    private int _lineTableSymbolIndex;
    private int _stringTableSymbolIndex;
    private int _locationSectionSymbolIndex;
    private readonly ElfSymbolTable? _symbolTable;

    public DwarfElfContext(ElfFile elf) : base(elf, new DiagnosticBag())
    {
        Elf = elf ?? throw new ArgumentNullException(nameof(elf));

        var relocContext = new ElfRelocationContext();

        var codeSection = elf.Sections.OfType<ElfStreamSection>().FirstOrDefault(s => s.Name == ".text");

        _symbolTable = elf.Sections.OfType<ElfSymbolTable>().FirstOrDefault();
        var mapSectionToSymbolIndex = new Dictionary<ElfSection, int>();
        if (_symbolTable != null)
        {
            for (var i = 0; i < _symbolTable.Entries.Count; i++)
            {
                var entry = _symbolTable.Entries[i];

                if (entry.Type == ElfSymbolType.Section && entry.SectionLink.Section != null)
                {
                    mapSectionToSymbolIndex[entry.SectionLink.Section] = i;
                }
            }

            if (codeSection != null)
            {
                if (!mapSectionToSymbolIndex.TryGetValue(codeSection, out _codeSectionSymbolIndex))
                {
                    _codeSectionSymbolIndex = _symbolTable.Entries.Count;
                    _symbolTable.Entries.Add(new ElfSymbol()
                    {
                        Type = ElfSymbolType.Section,
                        SectionLink = codeSection,
                    });
                }
            }
        }

        foreach (var section in elf.Sections)
        {
            switch (section.Name.Value)
            {
                case ".debug_info":
                    InfoSection = ((ElfStreamSection)section);
                    mapSectionToSymbolIndex.TryGetValue(InfoSection, out _infoSectionSymbolIndex);
                    break;
                case ".debug_abbrev":
                    AbbreviationTable = ((ElfStreamSection)section);
                    mapSectionToSymbolIndex.TryGetValue(AbbreviationTable, out _abbreviationTableSymbolIndex);
                    break;
                case ".debug_aranges":
                    AddressRangeTable = ((ElfStreamSection)section);
                    break;
                case ".debug_str":
                    StringTable = ((ElfStreamSection)section);
                    mapSectionToSymbolIndex.TryGetValue(StringTable, out _stringTableSymbolIndex);
                    break;
                case ".debug_line":
                    LineTable = ((ElfStreamSection)section);
                    mapSectionToSymbolIndex.TryGetValue(LineTable, out _lineTableSymbolIndex);
                    break;
                case ".debug_loc":
                    LocationSection = ((ElfStreamSection)section);
                    mapSectionToSymbolIndex.TryGetValue(LocationSection, out _locationSectionSymbolIndex);
                    break;

                case ".rela.debug_aranges":
                case ".rel.debug_aranges":
                    RelocAddressRangeTable = (ElfRelocationTable)section;
                    RelocAddressRangeTable.Relocate(relocContext);
                    break;

                case ".rela.debug_line":
                case ".rel.debug_line":
                    RelocLineTable = (ElfRelocationTable)section;
                    RelocLineTable.Relocate(relocContext);
                    break;

                case ".rela.debug_info":
                case ".rel.debug_info":
                    RelocInfoSection = (ElfRelocationTable)section;
                    RelocInfoSection.Relocate(relocContext);
                    break;

                case ".rela.debug_loc":
                case ".rel.debug_loc":
                    RelocLocationSection = (ElfRelocationTable)section;
                    RelocLocationSection.Relocate(relocContext);
                    break;
            }
        }
    }

    public ElfFile Elf { get; }

    public bool IsLittleEndian => Elf.Encoding == ElfEncoding.Lsb;

    public DwarfAddressSize AddressSize => Elf.FileClass == ElfFileClass.Is64 ? DwarfAddressSize.Bit64 : DwarfAddressSize.Bit32;

    public ElfStreamSection? InfoSection { get; private set; }

    public ElfRelocationTable? RelocInfoSection { get; set; }

    public ElfStreamSection? AbbreviationTable { get; set; }

    public ElfStreamSection? AddressRangeTable { get; private set; }

    public ElfRelocationTable? RelocAddressRangeTable { get; set; }

    public ElfStreamSection? StringTable { get; set; }

    public ElfStreamSection? LineTable { get; set; }

    public ElfRelocationTable? RelocLineTable { get; set; }

    public ElfStreamSection? LocationSection { get; private set; }

    public ElfRelocationTable? RelocLocationSection { get; set; }

    public int CodeSectionSymbolIndex => _codeSectionSymbolIndex;

    public int InfoSectionSymbolIndex => _infoSectionSymbolIndex;

    public int StringTableSymbolIndex => _stringTableSymbolIndex;

    public int AbbreviationTableSymbolIndex => _abbreviationTableSymbolIndex;

    public int LineTableSymbolIndex => _lineTableSymbolIndex;

    public int LocationSectionSymbolIndex => _locationSectionSymbolIndex;

    public ElfStreamSection GetOrCreateInfoSection()
    {
        return InfoSection ??= GetOrCreateDebugSection(".debug_info", true, out _infoSectionSymbolIndex);
    }

    public ElfRelocationTable GetOrCreateRelocInfoSection()
    {
        return RelocInfoSection ??= GetOrCreateRelocationTable(InfoSection!);
    }

    public ElfStreamSection GetOrCreateAbbreviationTable()
    {
        return AbbreviationTable ??= GetOrCreateDebugSection(".debug_abbrev", true, out _abbreviationTableSymbolIndex);
    }

    public ElfStreamSection GetOrCreateAddressRangeTable()
    {
        return AddressRangeTable ??= GetOrCreateDebugSection(".debug_aranges", false, out _);
    }

    public ElfRelocationTable GetOrCreateRelocAddressRangeTable()
    {
        return RelocAddressRangeTable ??= GetOrCreateRelocationTable(AddressRangeTable!);
    }

    public ElfStreamSection GetOrCreateLineSection()
    {
        return LineTable ??= GetOrCreateDebugSection(".debug_line", true, out _lineTableSymbolIndex);
    }

    public ElfRelocationTable GetOrCreateRelocLineSection()
    {
        return RelocLineTable ??= GetOrCreateRelocationTable(LineTable!);
    }

    public ElfStreamSection GetOrCreateStringTable()
    {
        return StringTable ??= GetOrCreateDebugSection(".debug_str", true, out _stringTableSymbolIndex);
    }

    public ElfStreamSection GetOrCreateLocationSection()
    {
        return LocationSection ??= GetOrCreateDebugSection(".debug_loc", true, out _locationSectionSymbolIndex);
    }

    public ElfRelocationTable GetOrCreateRelocLocationSection()
    {
        return RelocLocationSection ??= GetOrCreateRelocationTable(LocationSection!);
    }

    public void RemoveStringTable()
    {
        if (StringTable != null)
        {
            Elf.Content.Remove(StringTable);
            StringTable = null;
        }
    }

    public void RemoveAbbreviationTable()
    {
        if (AbbreviationTable != null)
        {
            Elf.Content.Remove(AbbreviationTable);
            AbbreviationTable = null;
        }
    }

    public void RemoveLineTable()
    {
        if (LineTable != null)
        {
            Elf.Content.Remove(LineTable);
            LineTable = null;
        }

        RemoveRelocLineTable();
    }

    public void RemoveRelocLineTable()
    {
        if (RelocLineTable != null)
        {
            Elf.Content.Remove(RelocLineTable);
            RelocLineTable = null;
        }
    }

    public void RemoveAddressRangeTable()
    {
        if (AddressRangeTable != null)
        {
            Elf.Content.Remove(AddressRangeTable);
            AddressRangeTable = null;
        }

        RemoveRelocAddressRangeTable();
    }

    public void RemoveRelocAddressRangeTable()
    {
        if (RelocAddressRangeTable != null)
        {
            Elf.Content.Remove(RelocAddressRangeTable);
            RelocAddressRangeTable = null;
        }
    }

    public void RemoveInfoSection()
    {
        if (InfoSection != null)
        {
            Elf.Content.Remove(InfoSection);
            InfoSection = null;
        }

        RemoveRelocInfoSection();
    }

    public void RemoveRelocInfoSection()
    {
        if (RelocInfoSection != null)
        {
            Elf.Content.Remove(RelocInfoSection);
            RelocInfoSection = null;
        }
    }

    public void RemoveLocationSection()
    {
        if (LocationSection != null)
        {
            Elf.Content.Remove(LocationSection);
            LocationSection = null;
        }

        RemoveRelocLocationSection();
    }

    public void RemoveRelocLocationSection()
    {
        if (RelocLocationSection != null)
        {
            Elf.Content.Remove(RelocLocationSection);
            RelocLocationSection = null;
        }
    }

    private ElfStreamSection GetOrCreateDebugSection(string name, bool createSymbol, out int symbolIndex)
    {
        var newSection = new ElfStreamSection(ElfSectionType.ProgBits)
        {
            Name = name,
            Alignment = 1,
            Stream = new MemoryStream(),
        };

        Elf.Content.Add(newSection);
        symbolIndex = 0;

        if (createSymbol && _symbolTable != null)
        {
            symbolIndex = _symbolTable.Entries.Count;
            _symbolTable.Entries.Add(new ElfSymbol()
            {
                Type = ElfSymbolType.Section,
                SectionLink = newSection,
            });
        }

        return newSection;
    }

    private ElfRelocationTable GetOrCreateRelocationTable(ElfStreamSection section)
    {
        var newSection = new ElfRelocationTable(true)
        {
            Name = $".rela{section.Name}",
            Alignment = (ulong)AddressSize,
            Flags = ElfSectionFlags.InfoLink,
            Info = section,
            Link = _symbolTable,
        };
        Elf.Content.Add(newSection);
        return newSection;
    }
}