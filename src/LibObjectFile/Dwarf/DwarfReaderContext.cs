// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using LibObjectFile.Elf;

namespace LibObjectFile.Dwarf
{
    public class DwarfReaderContext : DwarfReaderWriterContext
    {
        public bool IsInputReadOnly { get; set; }

        public static DwarfReaderContext FromElf(ElfObjectFile elf)
        {
            if (elf == null) throw new ArgumentNullException(nameof(elf));

            var readerContext = new DwarfReaderContext()
            {
                IsLittleEndian = elf.Encoding == ElfEncoding.Lsb,
                AddressSize = elf.FileClass == ElfFileClass.Is64 ? DwarfAddressSize.Bit64 : DwarfAddressSize.Bit32
            };

            ElfRelocationTable debugInfoReloc = null;

            List<StreamRelocation> relocations = null;

            foreach (var section in elf.Sections)
            {
                switch (section.Name.Value)
                {
                    case ".debug_info":
                        readerContext.DebugInfoStream = ((ElfBinarySection)section).Stream;
                        break;
                    case ".debug_abbrev":
                        readerContext.DebugAbbrevStream = ((ElfBinarySection)section).Stream;
                        break;
                    case ".debug_aranges":
                        readerContext.DebugAddressRangeStream = ((ElfBinarySection)section).Stream;
                        break;
                    case ".debug_str":
                        readerContext.DebugStringStream = ((ElfBinarySection)section).Stream;
                        break;
                    case ".debug_line":
                        readerContext.DebugLineStream = ((ElfBinarySection)section).Stream;
                        break;

                    case ".rela.debug_aranges":
                    case ".rel.debug_aranges":
                    case ".rela.debug_line":
                    case ".rel.debug_line":
                    case ".rela.debug_info":
                    case ".rel.debug_info":
                        if (relocations == null)
                            relocations = new List<StreamRelocation>();
                        relocations.Add(new StreamRelocation(((ElfBinarySection)section.Info.Section).Stream, (ElfRelocationTable)section));
                        break;
                }
            }

            // Apply relocation
            if (relocations != null)
            {
                foreach (var relocationStream in relocations)
                {
                    relocationStream.RelocationTable.Apply(relocationStream.Stream, new ElfRelocationContext());
                }
            }

            return readerContext;
        }

        private struct StreamRelocation
        {
            public StreamRelocation(Stream stream, ElfRelocationTable relocationTable)
            {
                Stream = stream;
                RelocationTable = relocationTable;
            }

            public readonly Stream Stream;

            public readonly ElfRelocationTable RelocationTable;
        }
    }
}