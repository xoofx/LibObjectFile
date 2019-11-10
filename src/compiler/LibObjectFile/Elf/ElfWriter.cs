using System;
using System.IO;

namespace LibObjectFile.Elf
{
    public abstract class ElfWriter : ObjectFileWriter, IElfEncoder
    {
        protected ElfWriter(ElfObjectFile objectFile, Stream stream) : base(stream)
        {
            ObjectFile = objectFile ?? throw new ArgumentNullException(nameof(objectFile));
            SectionHeaderNames = new ElfStringTable().ConfigureAs(ElfSectionSpecialType.SectionHeaderStringTable);
            SectionHeaderNames.Parent = ObjectFile;
        }

        public ElfObjectFile ObjectFile { get; }

        public abstract void Write();

        /// <summary>
        /// Contains the names used for the sections
        /// </summary>
        protected ElfStringTable SectionHeaderNames { get; }

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