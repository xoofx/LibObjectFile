// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.Dwarf
{
    public abstract class DwarfReaderWriterContext
    {
        public bool IsLittleEndian { get; set; }

        public DwarfAddressSize AddressSize { get; set; }
        
        public DwarfStreamAndPrint DebugAbbrevStream;

        public DwarfStreamAndPrint DebugStringStream;

        public DwarfStreamAndPrint DebugAddressRangeStream;

        public DwarfStreamAndPrint DebugLineStream;

        public DwarfStreamAndPrint DebugInfoStream;
    }
}