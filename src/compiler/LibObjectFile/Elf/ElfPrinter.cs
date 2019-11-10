using System;
using System.Buffers;
using System.IO;

namespace LibObjectFile.Elf
{
    public static class ElfPrinter
    {
        public static void Print(this ElfObjectFile elf, TextWriter writer)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));
            PrintElfHeader(elf, writer);
        }

        public static void PrintElfHeader(this ElfObjectFile elf, TextWriter writer)
        {
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
            writer.WriteLine($"  Version:                           {GetElfVersion(elf.Version)}");
            writer.WriteLine($"  OS/ABI:                            {GetElfOsAbi(elf.OSAbi)}");
            writer.WriteLine($"  ABI Version:                       {elf.AbiVersion}");
            writer.WriteLine($"  Type:                              {GetElfFileType(elf.FileType)}");
            writer.WriteLine($"  Machine:                           {GetElfArch(elf.Arch)}");
            writer.WriteLine($"  Version:                           0x{elf.Version:x}");
            writer.WriteLine($"  Entry point address:               0x{elf.EntryPointAddress:x}");
            writer.WriteLine($"  Start of program headers:          {elf.Layout.OffsetOfProgramHeaderTable} (bytes into file)");
            writer.WriteLine($"  Start of section headers:          {elf.Layout.OffsetOfSectionHeaderTable} (bytes into file)");
            writer.WriteLine($"  Flags:                             0x{elf.Flags:x}");
            writer.WriteLine($"  Size of this header:               {elf.Layout.SizeOfElfHeader} (bytes)");
            writer.WriteLine($"  Size of program headers:           {elf.Layout.SizeOfProgramHeaderEntry} (bytes)");
            writer.WriteLine($"  Number of program headers:         {elf.ProgramHeaders.Count}");
            writer.WriteLine($"  Size of section headers:           {elf.Layout.SizeOfSectionHeaderEntry} (bytes)");
            writer.WriteLine($"  Number of section headers:         {(elf.Sections.Count == 0 ? 0 : elf.Sections.Count + 2)}");
            writer.WriteLine($"  Section header string table index: {elf.SectionHeaderStringTable.Index}");
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