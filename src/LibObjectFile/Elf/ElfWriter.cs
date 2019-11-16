// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Buffers;
using System.IO;

namespace LibObjectFile.Elf
{
    public abstract class ElfWriter : ObjectFileReaderWriter, IElfEncoder
    {
        protected ElfWriter(ElfObjectFile objectFile, Stream stream) : base(stream)
        {
            ObjectFile = objectFile ?? throw new ArgumentNullException(nameof(objectFile));
        }

        public ElfObjectFile ObjectFile { get; }

        internal abstract void Write();


        public void Write(Stream stream, ulong size = 0, int bufferSize = 4096)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (bufferSize <= 0) throw new ArgumentOutOfRangeException(nameof(bufferSize));
            
            size = size == 0 ? (ulong)stream.Length - (ulong)stream.Position : size;
            var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);

            while (size != 0)
            {
                var sizeToRead = size >= (ulong) buffer.Length ? buffer.Length : (int)size;
                var sizeRead = stream.Read(buffer, 0, sizeToRead);
                if (sizeRead <= 0) break;

                Stream.Write(buffer, 0, sizeRead);
                size -= (ulong)sizeRead;
            }

            if (size != 0)
            {
                throw new InvalidOperationException("Unable to write stream entirely");
            }
        }

        public static ElfWriter Create(ElfObjectFile objectFile, Stream stream)
        {
            if (objectFile == null) throw new ArgumentNullException(nameof(objectFile));
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            var thisComputerEncoding = BitConverter.IsLittleEndian ? ElfEncoding.Lsb : ElfEncoding.Msb;
            return objectFile.Encoding == thisComputerEncoding ? (ElfWriter) new ElfWriterDirect(objectFile, stream) : new ElfWriterSwap(objectFile, stream);
        }

        public abstract void Encode(out RawElf.Elf32_Half dest, ushort value);
        public abstract void Encode(out RawElf.Elf64_Half dest, ushort value);
        public abstract void Encode(out RawElf.Elf32_Word dest, uint value);
        public abstract void Encode(out RawElf.Elf64_Word dest, uint value);
        public abstract void Encode(out RawElf.Elf32_Sword dest, int value);
        public abstract void Encode(out RawElf.Elf64_Sword dest, int value);
        public abstract void Encode(out RawElf.Elf32_Xword dest, ulong value);
        public abstract void Encode(out RawElf.Elf32_Sxword dest, long value);
        public abstract void Encode(out RawElf.Elf64_Xword dest, ulong value);
        public abstract void Encode(out RawElf.Elf64_Sxword dest, long value);
        public abstract void Encode(out RawElf.Elf32_Addr dest, uint value);
        public abstract void Encode(out RawElf.Elf64_Addr dest, ulong value);
        public abstract void Encode(out RawElf.Elf32_Off dest, uint offset);
        public abstract void Encode(out RawElf.Elf64_Off dest, ulong offset);
        public abstract void Encode(out RawElf.Elf32_Section dest, ushort index);
        public abstract void Encode(out RawElf.Elf64_Section dest, ushort index);
        public abstract void Encode(out RawElf.Elf32_Versym dest, ushort value);
        public abstract void Encode(out RawElf.Elf64_Versym dest, ushort value);
    }
}