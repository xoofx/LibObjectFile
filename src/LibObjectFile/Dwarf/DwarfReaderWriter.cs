// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.IO;

namespace LibObjectFile.Dwarf
{
    public sealed class DwarfReaderWriter : ObjectFileReaderWriter
    {
        internal DwarfReaderWriter(DwarfFileContext fileContext, DiagnosticBag diagnostics) : base(fileContext.DebugInfoStream, diagnostics)
        {
            IsReadOnly = fileContext.IsInputReadOnly;
            FileContext = fileContext;
            IsLittleEndian = fileContext.IsLittleEndian;
            Is64BitAddress = fileContext.Is64BitAddress;
        }

        public DwarfFileContext FileContext { get; }
        
        public override bool IsReadOnly { get; }

        public bool Is64BitEncoding { get; set; }

        public bool Is64BitAddress { get; }
       
        public ulong ReadUnitLength()
        {
            Is64BitEncoding = false;
            ulong length = ReadU32();
            if (length >= 0xFFFFFFF0 && length <= 0xFFFFFFFF)
            {
                if (length == 0xFFFFFFFF)
                {
                    Is64BitEncoding = true;
                    return ReadU64();
                }
            }
            return length;
        }

        public void WriteUnitLength(ulong length)
        {
            if (Is64BitEncoding)
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

        public ulong ReadDwarfUInt()
        {
            return Is64BitEncoding ? ReadU64() : ReadU32();
        }

        public void WriteNativeUInt(ulong value)
        {
            if (Is64BitEncoding)
            {
                WriteU64(value);
            }
            else
            {
                WriteU32((uint)value);
            }
        }

        public ulong ReadUInt()
        {
            return Is64BitAddress ? ReadU64() : ReadU32();
        }

        public void WriteTargetUInt(ulong target)
        {
            if (Is64BitAddress)
            {
                WriteU64(target);
            }
            else
            {
                WriteU32((uint)target);
            }
        }

        public ulong ReadULEB128()
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

        public long ReadILEB128()
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
}