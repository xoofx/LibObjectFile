// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.Dwarf
{
    public class DwarfCompilationUnit : DwarfUnit
    {
        public DwarfCompilationUnit()
        {
            Kind = DwarfUnitKind.compile;
        }

        protected override bool TryReadHeader(DwarfReader reader)
        {
            bool result;
            if (Version < 5)
            {
                // 3. debug_abbrev_offset (section offset) 
                DebugAbbreviationOffset = reader.ReadUIntFromEncoding();

                // 4. address_size (ubyte) 
                result = TryReadAddressSize(reader);
            }
            else
            {
                // NOTE: order of address_size/debug_abbrev_offset are different from Dwarf 4

                // 4. address_size (ubyte) 
                result = TryReadAddressSize(reader);

                // 5. debug_abbrev_offset (section offset) 
                DebugAbbreviationOffset = reader.ReadUIntFromEncoding();
            }

            return result;
        }

        protected override void WriteHeader(DwarfReaderWriter writer)
        {
            bool result;
            if (Version < 5)
            {
                // 3. debug_abbrev_offset (section offset) 
                writer.WriteUIntFromEncoding(Abbreviation.Offset);

                // 4. address_size (ubyte) 
                WriteAddressSize(writer);
            }
            else
            {
                // NOTE: order of address_size/debug_abbrev_offset are different from Dwarf 4

                // 4. address_size (ubyte) 
                WriteAddressSize(writer);

                // 5. debug_abbrev_offset (section offset) 
                writer.WriteUIntFromEncoding(Abbreviation.Offset);
            }
        }

        protected override void UpdateLayout(DwarfWriter writer, ref ulong sizeOf)
        {
            // 3. debug_abbrev_offset (section offset) 
            sizeOf += DwarfHelper.SizeOfUInt(writer.Is64BitEncoding); // writer.WriteUIntFromEncoding(Abbreviation.Offset);
            // 4. address_size (ubyte) 
            sizeOf += 1; // WriteAddressSize(writer);
        }
    }
}