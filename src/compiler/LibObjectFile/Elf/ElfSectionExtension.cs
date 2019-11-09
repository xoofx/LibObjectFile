using System;

namespace LibObjectFile.Elf
{
    public static class ElfSectionExtension
    {
        public static TElfSection ConfigureAs<TElfSection>(this TElfSection section, ElfSectionSpecialType sectionSpecialType, string relOrRelaName = null) where TElfSection : ElfSection
        {
            section.SpecialType = sectionSpecialType;

            switch (sectionSpecialType)
            {
                case ElfSectionSpecialType.None:
                    break;
                case ElfSectionSpecialType.Bss:
                    section.Name = ".bss";
                    section.Type = ElfSectionType.NoBits;
                    section.Flags = ElfSectionFlags.Alloc | ElfSectionFlags.Write;
                    break;
                case ElfSectionSpecialType.Comment:
                    section.Name = ".comment";
                    section.Type = ElfSectionType.ProgBits;
                    section.Flags = ElfSectionFlags.None;
                    break;
                case ElfSectionSpecialType.Data:
                    section.Name = ".data";
                    section.Type = ElfSectionType.ProgBits;
                    section.Flags = ElfSectionFlags.Alloc | ElfSectionFlags.Write;
                    break;
                case ElfSectionSpecialType.Data1:
                    section.Name = ".data1";
                    section.Type = ElfSectionType.ProgBits;
                    section.Flags = ElfSectionFlags.Alloc | ElfSectionFlags.Write;
                    break;
                case ElfSectionSpecialType.Debug:
                    section.Name = ".debug";
                    section.Type = ElfSectionType.ProgBits;
                    section.Flags = ElfSectionFlags.None;
                    break;
                case ElfSectionSpecialType.Dynamic:
                    section.Name = ".dynamic";
                    section.Type = ElfSectionType.DynamicLinking;
                    section.Flags = ElfSectionFlags.Alloc;
                    break;
                case ElfSectionSpecialType.DynamicStringTable:
                    section.Name = ".dynstr";
                    section.Type = ElfSectionType.StringTable;
                    section.Flags = ElfSectionFlags.Alloc;
                    break;
                case ElfSectionSpecialType.DynamicSymbolTable:
                    section.Name = ".dynsym";
                    section.Type = ElfSectionType.DynamicLinkerSymbolTable;
                    section.Flags = ElfSectionFlags.Alloc;
                    break;
                case ElfSectionSpecialType.Fini:
                    section.Name = ".fini";
                    section.Type = ElfSectionType.ProgBits;
                    section.Flags = ElfSectionFlags.Alloc | ElfSectionFlags.Executable;
                    break;
                case ElfSectionSpecialType.Got:
                    section.Name = ".got";
                    section.Type = ElfSectionType.ProgBits;
                    section.Flags = ElfSectionFlags.Alloc;
                    break;
                case ElfSectionSpecialType.Hash:
                    section.Name = ".hash";
                    section.Type = ElfSectionType.SymbolHashTable;
                    section.Flags = ElfSectionFlags.Alloc;
                    break;
                case ElfSectionSpecialType.Init:
                    section.Name = ".init";
                    section.Type = ElfSectionType.ProgBits;
                    section.Flags = ElfSectionFlags.Alloc | ElfSectionFlags.Executable;
                    break;
                case ElfSectionSpecialType.Interp:
                    section.Name = ".interp";
                    section.Type = ElfSectionType.ProgBits;
                    section.Flags = ElfSectionFlags.Alloc;
                    break;
                case ElfSectionSpecialType.Line:
                    section.Name = ".line";
                    section.Type = ElfSectionType.ProgBits;
                    section.Flags = ElfSectionFlags.None;
                    break;
                case ElfSectionSpecialType.Note:
                    section.Name = ".note";
                    section.Type = ElfSectionType.Note;
                    section.Flags = ElfSectionFlags.None;
                    break;
                case ElfSectionSpecialType.Plt:
                    section.Name = ".plt";
                    section.Type = ElfSectionType.ProgBits;
                    section.Flags = ElfSectionFlags.None;
                    break;
                case ElfSectionSpecialType.Relocation:
                    if (relOrRelaName == null) throw new ArgumentNullException(nameof(relOrRelaName));
                    section.Name = ".rel" + relOrRelaName;
                    section.Type = ElfSectionType.Relocation;
                    section.Flags = ElfSectionFlags.Alloc;
                    break;
                case ElfSectionSpecialType.RelocationAddends:
                    if (relOrRelaName == null) throw new ArgumentNullException(nameof(relOrRelaName));
                    section.Name = ".rela" + relOrRelaName;
                    section.Type = ElfSectionType.RelocationAddends;
                    section.Flags = ElfSectionFlags.Alloc;
                    break;
                case ElfSectionSpecialType.ReadOnlyData:
                    section.Name = ".rodata";
                    section.Type = ElfSectionType.ProgBits;
                    section.Flags = ElfSectionFlags.Alloc;
                    break;
                case ElfSectionSpecialType.ReadOnlyData1:
                    section.Name = ".rodata1";
                    section.Type = ElfSectionType.ProgBits;
                    section.Flags = ElfSectionFlags.Alloc;
                    break;
                case ElfSectionSpecialType.SectionHeaderStringTable:
                    section.Name = ".shstrtab";
                    section.Type = ElfSectionType.StringTable;
                    section.Flags = ElfSectionFlags.None;
                    break;
                case ElfSectionSpecialType.StringTable:
                    section.Name = ".strtab";
                    section.Type = ElfSectionType.StringTable;
                    section.Flags = ElfSectionFlags.Alloc;
                    break;
                case ElfSectionSpecialType.SymbolTable:
                    section.Name = ".symtab";
                    section.Type = ElfSectionType.SymbolTable;
                    section.Flags = ElfSectionFlags.Alloc;
                    break;
                case ElfSectionSpecialType.Text:
                    section.Name = ".text";
                    section.Type = ElfSectionType.ProgBits;
                    section.Flags = ElfSectionFlags.Alloc | ElfSectionFlags.Executable;
                    break;
                default:
                    throw ThrowHelper.InvalidEnum(sectionSpecialType);
            }
            return section;
        }
    }
}