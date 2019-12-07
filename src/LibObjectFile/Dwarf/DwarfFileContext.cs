// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.IO;
using LibObjectFile.Elf;

namespace LibObjectFile.Dwarf
{
    public class DwarfFileContext
    {
        public bool IsLittleEndian { get; set; }

        public bool Is64BitAddress { get; set; }

        public bool IsInputReadOnly { get; set; }

        public DwarfStreamAndDump DebugAbbrevStream;

        public DwarfStreamAndDump DebugStringStream;

        public DwarfStreamAndDump DebugAddressRangeStream;

        public DwarfStreamAndDump DebugLineStream;

        public DwarfStreamAndDump DebugInfoStream;
        
        public static DwarfFileContext FromElf(ElfObjectFile elf)
        {
            if (elf == null) throw new ArgumentNullException(nameof(elf));

            var readerContext = new DwarfFileContext()
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
    
    public struct DwarfStreamAndDump
    {
        public DwarfStreamAndDump(Stream stream) : this()
        {
            Stream = stream;
        }

        public DwarfStreamAndDump(Stream stream, TextWriter rawDump)
        {
            Stream = stream;
            RawDump = rawDump;
        }

        public Stream Stream { get; set; }

        public TextWriter RawDump { get; set; }

        public static implicit operator DwarfStreamAndDump(Stream stream)
        {
            return new DwarfStreamAndDump(stream);
        }

        public static implicit operator Stream(DwarfStreamAndDump streamAndDump)
        {
            return streamAndDump.Stream;
        }
    }
}