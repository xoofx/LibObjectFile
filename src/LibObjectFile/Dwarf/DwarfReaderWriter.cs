// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.IO;

namespace LibObjectFile.Dwarf
{
    public sealed class DwarfReaderWriter : ObjectFileReaderWriter
    {
        internal DwarfReaderWriter(DwarfInputOutputContext inputOutputContext, DiagnosticBag diagnostics) : base(inputOutputContext.DebugInfoStream, diagnostics)
        {
            IsReadOnly = inputOutputContext.IsInputReadOnly;
            InputOutputContext = inputOutputContext;
            IsLittleEndian = inputOutputContext.IsLittleEndian;
            IsTargetAddress64Bit = inputOutputContext.IsTarget64Bit;
        }

        public DwarfInputOutputContext InputOutputContext { get; }


        public override bool IsReadOnly { get; }

        public bool Is64Bit { get; set; }

        public bool IsTargetAddress64Bit { get; private set; }
       
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

        public void WriteUnitLength(ulong length)
        {
            if (Is64Bit)
            {
                WriteU32(0xFFFFFFFF);
                WriteU64(length);
            }
            else
            {
                if (length >= 0xFFFFFFF0)
                {
                    throw new ArgumentOutOfRangeException(nameof(length), $"Must be < 0xFFFFFFF0 but is 0x{length:X}");
                }
                WriteU32((uint)length);
            }
        }

        public ulong ReadNativeUInt()
        {
            return Is64Bit ? ReadU64() : ReadU32();
        }

        public void WriteNativeUInt(ulong value)
        {
            if (Is64Bit)
            {
                WriteU64(value);
            }
            else
            {
                WriteU32((uint)value);
            }
        }

        public ulong ReadTargetUInt()
        {
            return IsTargetAddress64Bit ? ReadU64() : ReadU32();
        }

        public void WriteTargetUInt(ulong target)
        {
            if (IsTargetAddress64Bit)
            {
                WriteU64(target);
            }
            else
            {
                WriteU32((uint)target);
            }
        }

        public ulong ReadLEB128()
        {
            return Stream.ReadLEB128();
        }

        public uint ReadLEB128AsU32()
        {
            return Stream.ReadLEB128AsU32();
        }

        public int ReadLEB128AsI32()
        {
            return Stream.ReadLEB128AsI32();
        }

        public long ReadSignedLEB128()
        {
            return Stream.ReadSignedLEB128();
        }

        public unsafe T ReadLEB128As<T>() where T : unmanaged
        {
            return Stream.ReadLEB128As<T>();
        }
        
        public void WriteLEB128(ulong value)
        {
            Stream.WriteLEB128(value);
        }
        public void WriteSignedLEB128(long value)
        {
            Stream.WriteSignedLEB128(value);
        }
    }

    public readonly struct DwarfReadAttributeFormContext
    {
        public DwarfReadAttributeFormContext(uint addressSize, DwarfFile dwarf)
        {
            AddressSize = addressSize;
            DwarfFile = dwarf;
        }

        public readonly uint AddressSize;

        public readonly DwarfFile DwarfFile;
    }
}