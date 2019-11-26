// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using LibObjectFile.Utils;

namespace LibObjectFile.Dwarf
{
    public class DwarfReaderWriter : ObjectFileReaderWriter
    {
        public DwarfReaderWriter(Stream stream, bool isLittleEndian) : base(stream)
        {
            IsReadOnly = true;
            IsLittleEndian = isLittleEndian;
        }

        public override bool IsReadOnly { get; }

        public bool Is64Bit { get; set; }
       
        public ulong ReadUnitLength()
        {
            Is64Bit = false;
            ulong length = ReadU32();
            if (length >= 0xFFFFFFF0 && length <= 0xFFFFFFFF)
            {
                if (length == 0xFFFFFFFF)
                {
                    Is64Bit = true;
                    return ReadU64();
                }
            }
            return length;
        }

        public ulong ReadNativeUInt()
        {
            return Is64Bit ? ReadU64() : ReadU32();
        }

        // https://wiki.osdev.org/DWARF

        public ulong ReadLEB128()
        {
            ulong value = 0;
            int shift = 0;
            while (true)
            {
                var b = ReadU8();
                value = (ulong)(b & 0x7f) << shift;
                if ((b & 0x80) == 0)
                {
                    break;
                }
                shift += 7;
            }
            return value;
        }

        public unsafe T ReadLEB128As<T>() where T : unmanaged
        {
            if (sizeof(T) > sizeof(ulong)) throw new ArgumentException($"Invalid sizeof(T) = {sizeof(T)} cannot be bigger than 8 bytes");
            bool isU32 = sizeof(T) == sizeof(uint);
            if (!isU32 && sizeof(T) != sizeof(ulong))
                throw new ArgumentException($"Invalid sizeof(T) = {sizeof(T)} must be either 4 bytes or 8 bytes");

            var offset = Offset;
            var rawLEB = ReadLEB128();
            T* value = (T*)&rawLEB;
            if (isU32)
            {
                if (rawLEB > uint.MaxValue) throw new InvalidOperationException($"The LEB128 0x{rawLEB:x16} read from stream at offset {offset} is out of range of UInt");
            }
            return *value;
        }

        public object ReadAttributeFormRawValue(DwarfAttributeForm attributeForm, in DwarfReadAttributeFormContext context)
        {
            switch (attributeForm.Value)
            {
                case DwarfNative.DW_FORM_addr:
                    {
                        return context.AddressSize == 8 ? ReadU64() : ReadU32();
                    }

                case DwarfNative.DW_FORM_block2: throw new NotSupportedException("DW_FORM_block2");
                case DwarfNative.DW_FORM_block4: throw new NotSupportedException("DW_FORM_block4");
                case DwarfNative.DW_FORM_data2:
                    {
                        return ReadU16();
                    }
                case DwarfNative.DW_FORM_data4:
                    {
                        return ReadU32();
                    }
                case DwarfNative.DW_FORM_data8:
                    {
                        return ReadU64();
                    }
                case DwarfNative.DW_FORM_string:
                    {
                        return ReadStringUTF8NullTerminated();
                    }
                case DwarfNative.DW_FORM_block: throw new NotSupportedException("DW_FORM_block");
                case DwarfNative.DW_FORM_block1: throw new NotSupportedException("DW_FORM_block1");
                case DwarfNative.DW_FORM_data1:
                    {
                        return ReadU8();
                    }
                case DwarfNative.DW_FORM_flag:
                {
                    return ReadU8() != 0;
                }
                case DwarfNative.DW_FORM_sdata: throw new NotSupportedException("DW_FORM_sdata");
                case DwarfNative.DW_FORM_strp:
                    {
                        context.StringTable.Stream.Position = (long)ReadNativeUInt();
                        return context.StringTable.Stream.ReadStringUTF8NullTerminated();
                    }
                case DwarfNative.DW_FORM_udata: throw new NotSupportedException("DW_FORM_udata");
                case DwarfNative.DW_FORM_ref_addr: throw new NotSupportedException("DW_FORM_ref_addr");
                case DwarfNative.DW_FORM_ref1:
                    {
                        return ReadU8();
                    }
                case DwarfNative.DW_FORM_ref2:
                    {
                        return ReadU16();
                    }
                case DwarfNative.DW_FORM_ref4:
                    {
                        return ReadU32();
                    }
                case DwarfNative.DW_FORM_ref8:
                    {
                        return ReadU64();
                    }
                case DwarfNative.DW_FORM_ref_udata:
                    {
                        return ReadLEB128();
                    }
                case DwarfNative.DW_FORM_indirect: throw new NotSupportedException("DW_FORM_indirect");
                case DwarfNative.DW_FORM_sec_offset:
                    {
                        return ReadNativeUInt();
                    }
                case DwarfNative.DW_FORM_exprloc:
                {
                    var length = ReadLEB128();
                    return ReadAsStream(length);
                }
                case DwarfNative.DW_FORM_flag_present:
                {
                    return true;
                }
                case DwarfNative.DW_FORM_strx: throw new NotSupportedException("DW_FORM_strx - DWARF5");
                case DwarfNative.DW_FORM_addrx: throw new NotSupportedException("DW_FORM_addrx - DWARF5");
                case DwarfNative.DW_FORM_ref_sup4: throw new NotSupportedException("DW_FORM_ref_sup4 - DWARF5");
                case DwarfNative.DW_FORM_strp_sup: throw new NotSupportedException("DW_FORM_strp_sup - DWARF5");
                case DwarfNative.DW_FORM_data16: throw new NotSupportedException("DW_FORM_data16 - DWARF5");
                case DwarfNative.DW_FORM_line_strp: throw new NotSupportedException("DW_FORM_line_strp - DWARF5");
                case DwarfNative.DW_FORM_ref_sig8: throw new NotSupportedException("DW_FORM_ref_sig8 - DWARF4");
                case DwarfNative.DW_FORM_implicit_const: throw new NotSupportedException("DW_FORM_implicit_const - DWARF5");
                case DwarfNative.DW_FORM_loclistx: throw new NotSupportedException("DW_FORM_loclistx - DWARF5");
                case DwarfNative.DW_FORM_rnglistx: throw new NotSupportedException("DW_FORM_rnglistx - DWARF5");
                case DwarfNative.DW_FORM_ref_sup8: throw new NotSupportedException("DW_FORM_ref_sup8 - DWARF5");
                case DwarfNative.DW_FORM_strx1: throw new NotSupportedException("DW_FORM_strx1 - DWARF5");
                case DwarfNative.DW_FORM_strx2: throw new NotSupportedException("DW_FORM_strx2 - DWARF5");
                case DwarfNative.DW_FORM_strx3: throw new NotSupportedException("DW_FORM_strx3 - DWARF5");
                case DwarfNative.DW_FORM_strx4: throw new NotSupportedException("DW_FORM_strx4 - DWARF5");
                case DwarfNative.DW_FORM_addrx1: throw new NotSupportedException("DW_FORM_addrx1 - DWARF5");
                case DwarfNative.DW_FORM_addrx2: throw new NotSupportedException("DW_FORM_addrx2 - DWARF5");
                case DwarfNative.DW_FORM_addrx3: throw new NotSupportedException("DW_FORM_addrx3 - DWARF5");
                case DwarfNative.DW_FORM_addrx4: throw new NotSupportedException("DW_FORM_addrx4 - DWARF5");
                case DwarfNative.DW_FORM_GNU_addr_index: throw new NotSupportedException("DW_FORM_GNU_addr_index - GNU extension in debug_info.dwo.");
                case DwarfNative.DW_FORM_GNU_str_index: throw new NotSupportedException("DW_FORM_GNU_str_index - GNU extension, somewhat like DW_FORM_strp");
                case DwarfNative.DW_FORM_GNU_ref_alt: throw new NotSupportedException("DW_FORM_GNU_ref_alt - GNU extension. Offset in .debug_info.");
                case DwarfNative.DW_FORM_GNU_strp_alt: throw new NotSupportedException("DW_FORM_GNU_strp_alt - GNU extension. Offset in .debug_str of another object file.");
                default: throw new NotSupportedException("Unknown DwarfAttributeForm");
            }
        }
    }

    public readonly struct DwarfReadAttributeFormContext
    {
        public DwarfReadAttributeFormContext(uint addressSize, DwarfDebugStringTable stringTable)
        {
            AddressSize = addressSize;
            StringTable = stringTable;
        }

        public readonly uint AddressSize;

        public readonly DwarfDebugStringTable StringTable;
    }
}