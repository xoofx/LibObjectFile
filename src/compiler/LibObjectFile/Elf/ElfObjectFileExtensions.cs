using System;
using System.IO;

namespace LibObjectFile.Elf
{
    using static RawElf;

    public static class ElfObjectFileExtensions
    {
        public static void CopyIdentTo(this ElfObjectFile objectFile, Span<byte> ident)
        {
            if (objectFile == null) throw new ArgumentNullException(nameof(objectFile));
            if (ident.Length < EI_NIDENT)
            {
                throw new ArgumentException($"Expecting span length to be >= {EI_NIDENT}");
            }
            
            // Clear ident
            for (int i = 0; i < EI_NIDENT; i++)
            {
                ident[i] = 0;
            }

            ident[EI_MAG0] = ELFMAG0;
            ident[EI_MAG1] = ELFMAG1;
            ident[EI_MAG2] = ELFMAG2;
            ident[EI_MAG3] = ELFMAG3;

            switch (objectFile.FileClass)
            {
                case ElfFileClass.None:
                    ident[EI_CLASS] = ELFCLASSNONE;
                    break;
                case ElfFileClass.Is32:
                    ident[EI_CLASS] = ELFCLASS32;
                    break;
                case ElfFileClass.Is64:
                    ident[EI_CLASS] = ELFCLASS64;
                    break;
                default:
                    throw ThrowHelper.InvalidEnum(objectFile.FileClass);
            }

            switch (objectFile.Encoding)
            {
                case ElfEncoding.None:
                    ident[EI_DATA] = ELFDATANONE;
                    break;
                case ElfEncoding.Lsb:
                    ident[EI_DATA] = ELFDATA2LSB;
                    break;
                case ElfEncoding.Msb:
                    ident[EI_DATA] = ELFDATA2MSB;
                    break;
                default:
                    throw ThrowHelper.InvalidEnum(objectFile.Encoding);
            }

            ident[EI_VERSION] = (byte)objectFile.Version;

            ident[EI_OSABI] = objectFile.OSAbi.Value;

            ident[EI_ABIVERSION] = objectFile.AbiVersion;
        }

        public static bool TryCopyIdentFrom(this ElfObjectFile objectFile, ReadOnlySpan<byte> ident, DiagnosticBag diagnostics)
        {
            if (objectFile == null) throw new ArgumentNullException(nameof(objectFile));
            if (ident.Length < EI_NIDENT)
            {
                diagnostics.Error($"Invalid ELF Ident length found. Must be >= {EI_NIDENT}");
                return false;
            }

            if (ident[EI_MAG0] != ELFMAG0 || ident[EI_MAG1] != ELFMAG1 || ident[EI_MAG2] != ELFMAG2 || ident[EI_MAG3] != ELFMAG3)
            {
                diagnostics.Error("Invalid ELF Magic found");
                return false;
            }

            switch (ident[EI_CLASS])
            {
                case ELFCLASSNONE:
                    objectFile.FileClass = ElfFileClass.None;
                    break;
                case ELFCLASS32:
                    objectFile.FileClass = ElfFileClass.Is32;
                    break;
                case ELFCLASS64:
                    objectFile.FileClass = ElfFileClass.Is64;
                    break;
                default:
                    diagnostics.Error($"Invalid EI_CLASS found 0x`{ident[EI_CLASS]:x2}`");
                    return false;
            }

            switch (ident[EI_DATA])
            {
                case ELFDATANONE:
                    objectFile.Encoding = ElfEncoding.None;
                    break;
                case ELFDATA2LSB:
                    objectFile.Encoding = ElfEncoding.Lsb;
                    break;
                case ELFDATA2MSB:
                    objectFile.Encoding = ElfEncoding.Msb;
                    break;
                default:
                    diagnostics.Error($"Invalid EI_DATA found 0x`{ident[EI_DATA]:x2}`");
                    return false;
            }

            objectFile.Version = ident[EI_VERSION];

            objectFile.OSAbi = new ElfOSAbi(ident[EI_OSABI]);

            objectFile.AbiVersion = ident[EI_ABIVERSION];
            return true;
        }

        public static void Write(this ElfObjectFile objectFile, Stream stream)
        {
            var elfWriter = ElfWriter.Create(objectFile, stream);
            elfWriter.Write();
        }
    }
}