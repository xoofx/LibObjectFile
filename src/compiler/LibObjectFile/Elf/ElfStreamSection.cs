using System;
using System.IO;

namespace LibObjectFile.Elf
{
    public class ElfStreamSection : ElfSection
    {
        public ElfStreamSection()
        {
        }

        public ElfStreamSection(Stream stream)
        {
            Stream = stream ?? throw new ArgumentNullException(nameof(stream));
        }

        public Stream Stream { get; set; }

        public override ulong Size => Stream != null ? (ulong) Stream.Length : 0;

        public override void Write(Stream stream)
        {
            if (Stream == null) return;
            Stream.Position = 0;
            Stream.CopyTo(stream);
        }
    }
}