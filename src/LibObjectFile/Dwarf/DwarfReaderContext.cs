// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
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
                Is64BitAddress = elf.FileClass == ElfFileClass.Is64
            };

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
                }
            }

            return readerContext;
        }
    }
}