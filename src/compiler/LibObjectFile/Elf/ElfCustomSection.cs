using System;
using System.IO;

namespace LibObjectFile.Elf
{
    public class ElfCustomSection : ElfSection
    {
        public ElfCustomSection()
        {
        }

        public ElfCustomSection(Stream stream)
        {
            Stream = stream ?? throw new ArgumentNullException(nameof(stream));
        }

        public Stream Stream { get; set; }

        protected override ulong GetSize() => Stream != null ? (ulong) Stream.Length : 0;

        protected override void Write(ElfWriter writer)
        {
            if (Stream == null) return;
            Stream.Position = 0;
            Stream.CopyTo(writer.Stream);
        }
    }
}