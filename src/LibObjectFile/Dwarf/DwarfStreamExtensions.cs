// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.IO;

namespace LibObjectFile.Dwarf
{
    public static class DwarfStreamExtensions
    {
        public static ulong ReadLEB128(this Stream stream)
        {
            ulong value = 0;
            int shift = 0;
            while (true)
            {
                var b = stream.ReadU8();
                value = ((ulong)(b & 0x7f) << shift) | value;
                if ((b & 0x80) == 0)
                {
                    break;
                }
                shift += 7;
            }
            return value;
        }

        public static void WriteLEB128(this Stream stream, ulong value)
        {
            do
            {
                var b = (byte)(value & 0x7f);
                value >>= 7;
                if (value != 0)
                    b |= 0x80;
                stream.WriteU8(b);
            } while (value != 0);
        }

        public static void WriteSignedLEB128(this Stream stream, long value)
        {
            bool cont = true;
            while (cont)
            {
                var b = (byte)((byte)value & 0x7f);
                value >>= 7;
                bool isSignBitSet = (b & 0x40) != 0;
                if ((value == 0 && !isSignBitSet) || (value == -1 && isSignBitSet))
                {
                    cont = false;
                }
                else
                {
                    b |= 0x80;
                }
                stream.WriteU8(b);
            }
        }

        public static uint ReadLEB128AsU32(this Stream stream)
        {
            var offset = stream.Position;
            var value = stream.ReadLEB128();
            if (value >= uint.MaxValue) throw new InvalidOperationException($"The LEB128 0x{value:x16} read from stream at offset {offset} is out of range of uint >= {uint.MaxValue}");
            return (uint)value;
        }

        public static int ReadLEB128AsI32(this Stream stream)
        {
            var offset = stream.Position;
            var value = stream.ReadLEB128();
            if (value >= int.MaxValue) throw new InvalidOperationException($"The LEB128 0x{value:x16} read from stream at offset {offset} is out of range of int >= {int.MaxValue}");
            return (int)value;
        }

        public static long ReadSignedLEB128(this Stream stream)
        {
            long value = 0;
            int shift = 0;
            byte b = 0;
            do
            {
                b = stream.ReadU8();
                value |= ((long) (b & 0x7f) << shift);
                shift += 7;
            } while ((b & 0x80) != 0);

            if (shift < 64 && (b & 0x40) != 0)
            {
                value |= (long)(~0UL << shift);
            }

            return value;
        }

        public static unsafe T ReadLEB128As<T>(this Stream stream) where T : unmanaged
        {
            if (sizeof(T) > sizeof(ulong)) throw new ArgumentException($"Invalid sizeof(T) = {sizeof(T)} cannot be bigger than 8 bytes");
            bool isU32 = sizeof(T) == sizeof(uint);
            if (!isU32 && sizeof(T) != sizeof(ulong))
                throw new ArgumentException($"Invalid sizeof(T) = {sizeof(T)} must be either 4 bytes or 8 bytes");

            var offset = stream.Position;
            var rawLEB = ReadLEB128(stream);
            T* value = (T*)&rawLEB;
            if (isU32)
            {
                if (rawLEB >= uint.MaxValue) throw new InvalidOperationException($"The LEB128 0x{rawLEB:x16} read from stream at offset {offset} is out of range of UInt");
            }
            return *value;
        }
    }
}