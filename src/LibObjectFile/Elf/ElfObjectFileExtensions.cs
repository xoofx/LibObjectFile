// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

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
            ident[EI_CLASS] = (byte) objectFile.FileClass;
            ident[EI_DATA] = (byte) objectFile.Encoding;
            ident[EI_VERSION] = (byte)objectFile.Version;
            ident[EI_OSABI] = objectFile.OSABI.Value;
            ident[EI_ABIVERSION] = objectFile.AbiVersion;
        }

        public static bool TryCopyIdentFrom(this ElfObjectFile objectFile, ReadOnlySpan<byte> ident, DiagnosticBag diagnostics)
        {
            if (objectFile == null) throw new ArgumentNullException(nameof(objectFile));
            if (ident.Length < EI_NIDENT)
            {
                diagnostics.Error(DiagnosticId.ELF_ERR_InvalidHeaderIdentLength, $"Invalid ELF Ident length found. Must be >= {EI_NIDENT}");
                return false;
            }

            if (ident[EI_MAG0] != ELFMAG0 || ident[EI_MAG1] != ELFMAG1 || ident[EI_MAG2] != ELFMAG2 || ident[EI_MAG3] != ELFMAG3)
            {
                diagnostics.Error(DiagnosticId.ELF_ERR_InvalidHeaderMagic, "Invalid ELF Magic found");
                return false;
            }

            objectFile.FileClass = (ElfFileClass)ident[EI_CLASS];
            objectFile.Encoding = (ElfEncoding)ident[EI_DATA];
            objectFile.Version = ident[EI_VERSION];
            objectFile.OSABI = new ElfOSABI(ident[EI_OSABI]);
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