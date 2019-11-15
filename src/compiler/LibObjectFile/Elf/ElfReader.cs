using System;
using System.Buffers;
using System.IO;

namespace LibObjectFile.Elf
{
    public abstract class ElfReader : ObjectFileReaderWriter, IElfDecoder
    {
        protected ElfReader(ElfObjectFile objectFile, Stream stream) : base(stream)
        {
            ObjectFile = objectFile ?? throw new ArgumentNullException(nameof(objectFile));
        }

        public ElfObjectFile ObjectFile { get; }

        internal abstract void Read();

        public MemoryStream ReadAsMemoryStream(ulong size)
        {
            var memoryStream = new MemoryStream((int)size);
            if (size == 0) return memoryStream;

            memoryStream.SetLength((long)size);

            var buffer = memoryStream.GetBuffer();
            while (size != 0)
            {
                var lengthToRead = size >= int.MaxValue ? int.MaxValue : (int) size;
                var lengthRead = Stream.Read(buffer, 0, lengthToRead);
                if (lengthRead < 0) break;
                if ((uint)lengthRead >= size)
                {
                    size -= (uint)lengthRead;
                }
                else
                {
                    break;
                }
            }
            return memoryStream;
        }

        public abstract ElfSectionLink ResolveLink(ElfSectionLink link, string errorMessageFormat);

        internal static ElfReader Create(ElfObjectFile objectFile, Stream stream)
        {
            if (objectFile == null) throw new ArgumentNullException(nameof(objectFile));
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            var ident = ArrayPool<byte>.Shared.Rent(RawElf.EI_NIDENT);
            var startPosition = stream.Position;
            var length = stream.Read(ident, 0, RawElf.EI_NIDENT);
            
            var span = new ReadOnlySpan<byte>(ident, 0, length);

            var diagnostics = new DiagnosticBag();

            if (!objectFile.TryCopyIdentFrom(span, diagnostics))
            {
                throw new ObjectFileException($"Invalid ELF object file while trying to decode Elf Header", diagnostics);
            }

            // Rewind
            stream.Position = startPosition;

            var thisComputerEncoding = BitConverter.IsLittleEndian ? ElfEncoding.Lsb : ElfEncoding.Msb;
            return objectFile.Encoding == thisComputerEncoding ? (ElfReader) new ElfReaderDirect(objectFile, stream) : new ElfReaderSwap(objectFile, stream);
        }

        public abstract ushort Decode(RawElf.Elf32_Half src);
        public abstract ushort Decode(RawElf.Elf64_Half src);
        public abstract uint Decode(RawElf.Elf32_Word src);
        public abstract uint Decode(RawElf.Elf64_Word src);
        public abstract int Decode(RawElf.Elf32_Sword src);
        public abstract int Decode(RawElf.Elf64_Sword src);
        public abstract ulong Decode(RawElf.Elf32_Xword src);
        public abstract long Decode(RawElf.Elf32_Sxword src);
        public abstract ulong Decode(RawElf.Elf64_Xword src);
        public abstract long Decode(RawElf.Elf64_Sxword src);
        public abstract uint Decode(RawElf.Elf32_Addr src);
        public abstract ulong Decode(RawElf.Elf64_Addr src);
        public abstract uint Decode(RawElf.Elf32_Off src);
        public abstract ulong Decode(RawElf.Elf64_Off src);
        public abstract ushort Decode(RawElf.Elf32_Section src);
        public abstract ushort Decode(RawElf.Elf64_Section src);
        public abstract ushort Decode(RawElf.Elf32_Versym src);
        public abstract ushort Decode(RawElf.Elf64_Versym src);
    }
}