using System;
using System.Buffers;
using System.IO;
using System.Text;

namespace LibObjectFile.Elf
{
    public static class ElfPrinter
    {
        public static void Print(this ElfObjectFile elf, TextWriter writer)
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
        }

        public static void PrintElfHeader(ElfObjectFile elf, TextWriter writer)
        {
            if (elf == null) throw new ArgumentNullException(nameof(elf));
            if (writer == null) throw new ArgumentNullException(nameof(writer));

            Span<byte> ident = stackalloc byte[ElfObjectFile.IdentSizeInBytes];
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
            writer.WriteLine($"  OS/ABI:                            {GetElfOsAbi(elf.OSAbi)}");
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
            writer.WriteLine($"  Number of program headers:         {elf.ProgramHeaders.Count}");
            writer.WriteLine($"  Size of section headers:           {elf.Layout.SizeOfSectionHeaderEntry} (bytes)");
            writer.WriteLine($"  Number of section headers:         {(elf.Sections.Count == 0 ? 0 : elf.Sections.Count + 2)}");
            writer.WriteLine($"  Section header string table index: {elf.SectionHeaderStringTable.Index}");
        }

        public static void PrintSectionHeaders(ElfObjectFile elf, TextWriter writer)
        {
            if (elf == null) throw new ArgumentNullException(nameof(elf));
            if (writer == null) throw new ArgumentNullException(nameof(writer));
            if (elf.Sections.Count == 0)
            {
                writer.WriteLine("There are no sections in this file.");
                return;
            }

            writer.WriteLine();
            writer.WriteLine(elf.Sections.Count > 0 ? "Section Headers:" : "Section Header:");

            writer.WriteLine("  [Nr] Name              Type            Address          Off    Size   ES Flg Lk Inf Al");
            writer.WriteLine("  [ 0]                   NULL            0000000000000000 000000 000000 00      0   0  0");
            for (int i = 0; i < elf.Sections.Count; i++)
            {
                var section = elf.Sections[i];
                writer.WriteLine($"  [{section.Index,2:#0}] {section.FullName,-17} {GetElfSectionType(section.Type),-15} {section.VirtualAddress:x16} {section.Offset:x6} {section.Size:x6} {section.TableEntrySize:x2} {GetElfSectionFlags(section.Flags),3} {section.Link.GetIndex(),2} {section.Info.GetIndex(),3} {section.Alignment,2}");
            }

            {
                var section = elf.SectionHeaderStringTableInternal;
                writer.WriteLine($"  [{section.Index,2:#0}] {section.FullName,-17} {GetElfSectionType(section.Type),-15} {section.VirtualAddress:x16} {section.Offset:x6} {section.Size:x6} {section.TableEntrySize:x2} {GetElfSectionFlags(section.Flags),3} {section.Link.GetIndex(),2} {section.Info.GetIndex(),3} {section.Alignment,2}");
            }
            writer.WriteLine(@"Key to Flags:
  W (write), A (alloc), X (execute), M (merge), S (strings), I (info),
  L (link order), O (extra OS processing required), G (group), T (TLS),
  C (compressed), x (unknown), o (OS specific), E (exclude),
  l (large), p (processor specific)");
        }

        public static void PrintSectionGroups(ElfObjectFile elf, TextWriter writer)
        {
            if (elf == null) throw new ArgumentNullException(nameof(elf));
            if (writer == null) throw new ArgumentNullException(nameof(writer));
            writer.WriteLine();
            writer.WriteLine("There are no section groups in this file.");
            // TODO
        }

        public static void PrintProgramHeaders(ElfObjectFile elf, TextWriter writer)
        {
            if (elf == null) throw new ArgumentNullException(nameof(elf));
            if (writer == null) throw new ArgumentNullException(nameof(writer));

            writer.WriteLine();

            if (elf.ProgramHeaders.Count == 0)
            {
                writer.WriteLine("There are no program headers in this file.");
                return;
            }
            
            writer.WriteLine(elf.ProgramHeaders.Count > 1 ? "Program Headers:" : "Program Header:");

            writer.WriteLine("  Type           Offset   VirtAddr           PhysAddr           FileSiz  MemSiz   Flg Align");
            for (int i = 0; i < elf.ProgramHeaders.Count; i++)
            {
                var phdr = elf.ProgramHeaders[i];
                writer.WriteLine($"  {GetElfSegmentType(phdr.Type),-14} 0x{GetElfSegmentOffset(phdr.Offset):x6} 0x{phdr.VirtualAddress:x16} 0x{phdr.PhysicalAddress:x16} 0x{phdr.SizeInFile:x6} 0x{phdr.SizeInMemory:x6} {GetElfSegmentFlags(phdr.Flags),3} 0x{phdr.Align:x4}");
            }

            if (elf.ProgramHeaders.Count > 0 && elf.Sections.Count > 0)
            {
                writer.WriteLine();
                writer.WriteLine(" Section to Segment mapping:");
                writer.WriteLine("  Segment Sections...");

                for (int i = 0; i < elf.ProgramHeaders.Count; i++)
                {
                    var segment = elf.ProgramHeaders[i];
                    writer.Write($"   {i:00}     ");

                    foreach (var section in elf.Sections)
                    {
                        if (IsSectionInSegment(section, segment, true, true))
                        {
                            writer.Write($"{section.FullName} ");
                        }
                    }

                    writer.WriteLine();
                }
            }
        }

        public static void PrintRelocations(ElfObjectFile elf, TextWriter writer)
        {
            if (elf == null) throw new ArgumentNullException(nameof(elf));
            if (writer == null) throw new ArgumentNullException(nameof(writer));

            writer.WriteLine();

            bool hasRelocations = false;

            foreach (var section in elf.Sections)
            {
                if (section.Type == ElfSectionType.Relocation || section.Type == ElfSectionType.RelocationAddends)
                {
                    hasRelocations = true;
                    var relocTable = (ElfRelocationTable) section;

                    writer.WriteLine($"Relocation section '{section.FullName}' at offset 0x{section.Offset:x} contains {relocTable.Entries.Count} entries:");
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
                        if (entry.SymbolIndex <= symbolTable.Entries.Count)
                        {
                            symbolName = symbolTable.Entries[(int)entry.SymbolIndex].Name;
                            symbolValue = symbolTable.Entries[(int)entry.SymbolIndex].Value;
                        }

                        if (elf.FileClass == ElfFileClass.Is32)
                        {
                            writer.WriteLine($"{entry.Offset:x8}  {entry.Info32:x8} {entry.Type.Name,-22} {symbolValue:x8} {symbolName} + {entry.Addend}");
                        }
                        else
                        {
                            writer.WriteLine($"{entry.Offset:x16}  {entry.Info64:x16} {entry.Type.Name,-22} {symbolValue:x16} {symbolName} + {entry.Addend}");
                        }
                    }

                }
            }

            if (!hasRelocations)
            {
                writer.WriteLine("There are no relocations in this file.");
            }
        }

        public static void PrintUnwind(ElfObjectFile elf, TextWriter writer)
        {
            if (elf == null) throw new ArgumentNullException(nameof(elf));
            if (writer == null) throw new ArgumentNullException(nameof(writer));

            writer.WriteLine();
            writer.WriteLine($"The decoding of unwind sections for machine type {GetElfArch(elf.Arch)} is not currently supported.");
        }


        public static void PrintSymbolTables(ElfObjectFile elf, TextWriter writer)
        {
            if (elf == null) throw new ArgumentNullException(nameof(elf));
            if (writer == null) throw new ArgumentNullException(nameof(writer));

            if (elf.Sections.Count == 0) return;

            foreach (var section in elf.Sections)
            {
                if (!(section is ElfSymbolTable symbolTable)) continue;

                writer.WriteLine();
                writer.WriteLine(symbolTable.Entries.Count <= 1
                    ? $"Symbol table '{symbolTable.FullName}' contains {symbolTable.Entries.Count} entry:"
                    : $"Symbol table '{symbolTable.FullName}' contains {symbolTable.Entries.Count} entries:"
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
                    writer.WriteLine($"{i,6}: {symbol.Value:x16} {symbol.Size,5} {GetElfSymbolType(symbol.Type),-7} {GetElfSymbolBind(symbol.Bind),-6} {GetElfSymbolVisibility(symbol.Visibility),-7} {(symbol.Section.GetIndex() == 0 ? "UND" : symbol.Section.GetIndex().ToString()),4} {symbol.Name.Value}");
                }
            }
        }

        public static void PrintVersionInformation(ElfObjectFile elf, TextWriter writer)
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
                        || ((section).Offset >= segment.Offset.Value

                            && (!(isStrict)

                                || (section.Offset - segment.Offset.Value

                                    <= segment.SizeInFile - 1))

                            && ((section.Offset - segment.Offset.Value
                                 + GetSectionSize(section, segment))

                                <= segment.SizeInFile)))
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

                             || (section.Offset > segment.Offset.Value

                                 && (section.Offset - segment.Offset.Value

                                     < segment.SizeInFile)))

                            && ((section.Flags & ElfSectionFlags.Alloc) == 0

                                || (section.VirtualAddress > segment.VirtualAddress

                                    && (section.VirtualAddress - segment.VirtualAddress

                                        < segment.SizeInMemory)))));
        }

        public static void PrintDynamicSections(ElfObjectFile elf, TextWriter writer)
        {
            writer.WriteLine();
            writer.WriteLine("There is no dynamic section in this file.");
            // TODO
        }

        private static string GetElfSegmentFlags(ElfSegmentFlags flags)
        {
            if (flags.Value == 0) return string.Empty;

            var builder = new StringBuilder();
            builder.Append((flags.Value & RawElf.PF_R) != 0 ? 'R' : ' ');
            builder.Append((flags.Value & RawElf.PF_W) != 0 ? 'W' : ' ');
            builder.Append((flags.Value & RawElf.PF_X) != 0 ? 'E' : ' ');
            // TODO: other flags
            return builder.ToString();
        }

        private static ulong GetElfSegmentOffset(ElfOffset offset)
        {
            return offset.Value;
        }

        public static string GetElfSegmentType(ElfSegmentType segmentType)
        {
            return segmentType.Value switch
            {
                RawElf.PT_NULL => "NULL",
                RawElf.PT_LOAD => "LOAD",
                RawElf.PT_DYNAMIC => "DYNAMIC",
                RawElf.PT_INTERP => "INTERP",
                RawElf.PT_NOTE => "NOTE",
                RawElf.PT_SHLIB => "SHLIB",
                RawElf.PT_PHDR => "PHDR",
                RawElf.PT_TLS => "TLS",
                RawElf.PT_GNU_EH_FRAME => "GNU_EH_FRAME",
                RawElf.PT_GNU_STACK => "GNU_STACK",
                RawElf.PT_GNU_RELRO => "GNU_RELRO",
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
                RawElf.EV_CURRENT => $"{version} (current)",
                RawElf.EV_NONE => "",
                _ => $"{version}  <unknown>",
            };
        }


        private static string GetElfOsAbi(ElfOSAbi osAbi)
        {
            return osAbi.Value switch
            {
                RawElf.ELFOSABI_NONE => "UNIX - System V",
                RawElf.ELFOSABI_HPUX => "UNIX - HP-UX",
                RawElf.ELFOSABI_NETBSD => "UNIX - NetBSD",
                RawElf.ELFOSABI_GNU => "UNIX - GNU",
                RawElf.ELFOSABI_SOLARIS => "UNIX - Solaris",
                RawElf.ELFOSABI_AIX => "UNIX - AIX",
                RawElf.ELFOSABI_IRIX => "UNIX - IRIX",
                RawElf.ELFOSABI_FREEBSD => "UNIX - FreeBSD",
                RawElf.ELFOSABI_TRU64 => "UNIX - TRU64",
                RawElf.ELFOSABI_MODESTO => "Novell - Modesto",
                RawElf.ELFOSABI_OPENBSD => "UNIX - OpenBSD",
                _ => $"<unknown: {osAbi.Value:x}"
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
                    if (e_type >= RawElf.ET_LOPROC && e_type <= RawElf.ET_HIPROC) return $"Processor Specific: ({e_type:x})";
                    else if (e_type >= RawElf.ET_LOOS && e_type <= RawElf.ET_HIOS)
                        return $"OS Specific: ({e_type:x})";
                    else
                        return $"<unknown>: {e_type:x}";
            }
        }

        static string GetElfArch(ElfArch arch)
        {
            switch (arch.Value)
            {
                case RawElf.EM_NONE: return "None";
                case RawElf.EM_M32: return "WE32100";
                case RawElf.EM_SPARC: return "Sparc";
                case RawElf.EM_386: return "Intel 80386";
                case RawElf.EM_68K: return "MC68000";
                case RawElf.EM_88K: return "MC88000";
                case RawElf.EM_860: return "Intel 80860";
                case RawElf.EM_MIPS: return "MIPS R3000";
                case RawElf.EM_S370: return "IBM System/370";
                case RawElf.EM_MIPS_RS3_LE: return "MIPS R4000 big-endian";
                case RawElf.EM_PARISC: return "HPPA";
                case RawElf.EM_SPARC32PLUS: return "Sparc v8+";
                case RawElf.EM_960: return "Intel 80960";
                case RawElf.EM_PPC: return "PowerPC";
                case RawElf.EM_PPC64: return "PowerPC64";
                case RawElf.EM_S390: return "IBM S/390";
                case RawElf.EM_V800: return "Renesas V850 (using RH850 ABI)";
                case RawElf.EM_FR20: return "Fujitsu FR20";
                case RawElf.EM_RH32: return "TRW RH32";
                case RawElf.EM_ARM: return "ARM";
                case RawElf.EM_SH: return "Renesas / SuperH SH";
                case RawElf.EM_SPARCV9: return "Sparc v9";
                case RawElf.EM_TRICORE: return "Siemens Tricore";
                case RawElf.EM_ARC: return "ARC";
                case RawElf.EM_H8_300: return "Renesas H8/300";
                case RawElf.EM_H8_300H: return "Renesas H8/300H";
                case RawElf.EM_H8S: return "Renesas H8S";
                case RawElf.EM_H8_500: return "Renesas H8/500";
                case RawElf.EM_IA_64: return "Intel IA-64";
                case RawElf.EM_MIPS_X: return "Stanford MIPS-X";
                case RawElf.EM_COLDFIRE: return "Motorola Coldfire";
                case RawElf.EM_68HC12: return "Motorola MC68HC12 Microcontroller";
                case RawElf.EM_MMA: return "Fujitsu Multimedia Accelerator";
                case RawElf.EM_PCP: return "Siemens PCP";
                case RawElf.EM_NCPU: return "Sony nCPU embedded RISC processor";
                case RawElf.EM_NDR1: return "Denso NDR1 microprocesspr";
                case RawElf.EM_STARCORE: return "Motorola Star*Core processor";
                case RawElf.EM_ME16: return "Toyota ME16 processor";
                case RawElf.EM_ST100: return "STMicroelectronics ST100 processor";
                case RawElf.EM_TINYJ: return "Advanced Logic Corp. TinyJ embedded processor";
                case RawElf.EM_X86_64: return "Advanced Micro Devices X86-64";
                case RawElf.EM_PDSP: return "Sony DSP processor";
                case RawElf.EM_FX66: return "Siemens FX66 microcontroller";
                case RawElf.EM_ST9PLUS: return "STMicroelectronics ST9+ 8/16 bit microcontroller";
                case RawElf.EM_ST7: return "STMicroelectronics ST7 8-bit microcontroller";
                case RawElf.EM_68HC16: return "Motorola MC68HC16 Microcontroller";
                case RawElf.EM_68HC11: return "Motorola MC68HC11 Microcontroller";
                case RawElf.EM_68HC08: return "Motorola MC68HC08 Microcontroller";
                case RawElf.EM_68HC05: return "Motorola MC68HC05 Microcontroller";
                case RawElf.EM_SVX: return "Silicon Graphics SVx";
                case RawElf.EM_ST19: return "STMicroelectronics ST19 8-bit microcontroller";
                case RawElf.EM_VAX: return "Digital VAX";
                case RawElf.EM_CRIS: return "Axis Communications 32-bit embedded processor";
                case RawElf.EM_JAVELIN: return "Infineon Technologies 32-bit embedded cpu";
                case RawElf.EM_FIREPATH: return "Element 14 64-bit DSP processor";
                case RawElf.EM_ZSP: return "LSI Logic's 16-bit DSP processor";
                case RawElf.EM_MMIX: return "Donald Knuth's educational 64-bit processor";
                case RawElf.EM_HUANY: return "Harvard Universitys's machine-independent object format";
                case RawElf.EM_PRISM: return "Vitesse Prism";
                case RawElf.EM_AVR: return "Atmel AVR 8-bit microcontroller";
                case RawElf.EM_FR30: return "Fujitsu FR30";
                case RawElf.EM_D10V: return "d10v";
                case RawElf.EM_D30V: return "d30v";
                case RawElf.EM_V850: return "Renesas V850";
                case RawElf.EM_M32R: return "Renesas M32R (formerly Mitsubishi M32r)";
                case RawElf.EM_MN10300: return "mn10300";
                case RawElf.EM_MN10200: return "mn10200";
                case RawElf.EM_PJ: return "picoJava";
                case RawElf.EM_XTENSA: return "Tensilica Xtensa Processor";
                default:
                    return $"<unknown>: 0x{arch.Value:x}";
            }
        }
    }
}