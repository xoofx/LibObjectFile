using System;
using System.IO;

namespace LibObjectFile.Elf
{
    internal abstract class ElfWriter
    {
        protected ElfWriter(ElfObjectFile objectFile, Stream stream)
        {
            ObjectFile = objectFile ?? throw new ArgumentNullException(nameof(objectFile));
            Stream = stream ?? throw new ArgumentNullException(nameof(stream));
            SectionHeaderNames = new ElfStringTableSection().ConfigureAs(ElfSectionSpecialType.SectionHeaderStringTable);
        }

        public ElfObjectFile ObjectFile { get; }

        public Stream Stream { get; }

        public abstract void Write();

        /// <summary>
        /// Contains the names used for the sections
        /// </summary>
        protected ElfStringTableSection SectionHeaderNames { get; }

        public static ElfWriter Create(ElfObjectFile objectFile, Stream stream)
        {
            if (objectFile == null) throw new ArgumentNullException(nameof(objectFile));
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            var thisComputerEncoding = BitConverter.IsLittleEndian ? ElfEncoding.Lsb : ElfEncoding.Msb;
            return objectFile.Encoding == thisComputerEncoding ? (ElfWriter) new ElfWriterDirect(objectFile, stream) : new ElfWriterSwap(objectFile, stream);
        }
    }
}