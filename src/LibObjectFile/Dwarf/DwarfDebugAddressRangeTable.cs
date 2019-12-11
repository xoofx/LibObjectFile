// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using LibObjectFile.Utils;

namespace LibObjectFile.Dwarf
{
    [DebuggerDisplay("Count = {Ranges.Count,nq}")]
    public class DwarfDebugAddressRangeTable : DwarfSection
    {
        public DwarfDebugAddressRangeTable()
        {
            Ranges = new List<DwarfDebugAddressRange>();
        }

        public ushort Version { get; set; }

        public bool Is64BitEncoding { get; set; }

        public bool Is64BitAddress { get; set; }

        public byte SegmentSelectorSize { get; set; }

        internal ulong DebugInfoOffset { get; set; }

        public List<DwarfDebugAddressRange> Ranges { get; }

        public static DwarfDebugAddressRangeTable Read(Stream stream, bool isLittleEndian, TextWriter rawDump = null)
        {
            var dwarfDebugAddressRangeTable = new DwarfDebugAddressRangeTable();
            var reader = new DwarfReader(new DwarfReaderContext()
            {
                IsLittleEndian = isLittleEndian, 
                DebugAddressRangeStream = new DwarfStreamAndPrint(stream, rawDump)
            }, new DiagnosticBag());
            dwarfDebugAddressRangeTable.Read(reader);
            if (reader.Diagnostics.HasErrors)
            {
                throw new ObjectFileException("Unexpected error while reading address range table", reader.Diagnostics);
            }
            return dwarfDebugAddressRangeTable;
        }

        internal void Read(DwarfReaderWriter reader)
        {
            if (reader.Context.DebugAddressRangeStream.Stream == null)
            {
                return;
            }

            var currentStream = reader.Stream;
            try
            {
                reader.Stream = reader.Context.DebugAddressRangeStream;
                ReadInternal(reader, reader.Context.DebugAddressRangeStream.Printer);
            }
            finally
            {
                reader.Stream = currentStream;
            }
        }

        private void ReadInternal(DwarfReaderWriter reader, TextWriter dumpLog)
        {
            var unitLength = reader.ReadUnitLength();
            Is64BitEncoding = reader.Is64BitEncoding;
            var startPosition = reader.Offset;
            Version = reader.ReadU16();

            if (Version != 2)
            {
                reader.Diagnostics.Error(DiagnosticId.DWARF_ERR_VersionNotSupported, $"Version {Version} for .debug_aranges not supported");
                return;
            }

            DebugInfoOffset = reader.ReadUIntFromEncoding();

            var address_size = reader.ReadU8();
            if (address_size != 4 && address_size != 8)
            {
                reader.Diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidAddressSize, $"Unsupported address size {address_size}. Must be 4 (32 bits) or 8 (64 bits).");
                return;
            }
            Is64BitAddress = address_size == 8;

            var segment_selector_size = reader.ReadU8();
            SegmentSelectorSize = segment_selector_size;

            var align = (ulong)segment_selector_size + (ulong)address_size * 2;

            // SPECS 7.21: The first tuple following the header in each set begins at an offset that is a multiple of the size of a single tuple
            var nextOffset = AlignHelper.AlignToUpper(reader.Offset - startPosition, align);
            reader.Offset = nextOffset;
            
            while (true)
            {
                ulong segment = 0;
                switch (segment_selector_size)
                {
                    case 4:
                        segment = reader.ReadU32();
                        break;

                    case 8:
                        segment = reader.ReadU64();
                        break;

                    case 0:
                        break;
                }

                ulong address = 0;
                ulong length = 0;
                switch (address_size)
                {
                    case 4:
                        address = reader.ReadU32();
                        length = reader.ReadU32();
                        break;
                    case 8:
                        address = reader.ReadU64();
                        length = reader.ReadU64();
                        break;
                }

                if (segment == 0 && address == 0 && length == 0)
                {
                    break;
                }

                Ranges.Add(new DwarfDebugAddressRange(segment, address, length));
            }
        }

        public override bool TryUpdateLayout(DiagnosticBag diagnostics)
        {
            return true;
        }

        internal void Write(DwarfReaderWriter writer)
        {
        }
    }
}