using System;
using System.IO;

namespace LibObjectFile.Elf
{
    public class ElfStreamSection : ElfSection
    {
        public ElfStreamSection()
        {
        }

        public Stream Stream { get; set; }

        public override ulong Size => Stream != null ? (ulong) Stream.Length : 0;

        public override void Write(Stream stream)
        {
            Stream?.CopyTo(stream);
        }
    }
}