// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.IO;

namespace LibObjectFile.Dwarf
{
    public abstract class DwarfReaderWriter : ObjectFileReaderWriter
    {
        internal DwarfReaderWriter(DwarfReaderWriterContext context, DiagnosticBag diagnostics) : base(context.DebugInfoStream, diagnostics)
        {
            Context = context;
            IsLittleEndian = context.IsLittleEndian;
            Is64BitAddress = context.Is64BitAddress;
        }

        public DwarfReaderWriterContext Context { get; }

        public bool Is64BitEncoding { get; set; }

        public bool Is64BitAddress { get; }
       
        public ulong ReadUnitLength()
        {
            Is64BitEncoding = false;
            uint length = ReadU32();
            if (length >= 0xFFFFFFF0)
            {
                if (length != 0xFFFFFFFF)
                {
                    throw new InvalidOperationException($"Unsupported unit length prefix 0x{length:x8}");
                }

                Is64BitEncoding = true;
                return ReadU64();
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

        public ulong ReadUIntFromEncoding()
        {
            return Is64BitEncoding ? ReadU64() : ReadU32();
        }

        public void WriteUIntFromEncoding(ulong value)
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

        public void WriteUInt(ulong target)
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
            return Stream.ReadULEB128();
        }

        public uint ReadLEB128AsU32()
        {
            return Stream.ReadULEB128AsU32();
        }

        public int ReadLEB128AsI32()
        {
            return Stream.ReadLEB128AsI32();
        }

        public long ReadILEB128()
        {
            return Stream.ReadSignedLEB128();
        }

        public void WriteULEB128(ulong value)
        {
            Stream.WriteULEB128(value);
        }
        public void WriteILEB128(long value)
        {
            Stream.WriteILEB128(value);
        }
    }
}