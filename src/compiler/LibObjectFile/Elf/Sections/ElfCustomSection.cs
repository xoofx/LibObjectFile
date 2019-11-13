using System;
using System.IO;
using LibObjectFile.Utils;

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

        public override ElfSectionType Type
        {
            get => base.Type;
            set
            {
                // Don't allow relocation or symbol table to enforce proper usage
                if (value == ElfSectionType.Relocation || value == ElfSectionType.RelocationAddends)
                {
                    throw new ArgumentException($"Invalid type `{Type}` of the section [{Index}]. Must be used on a `{nameof(ElfRelocationTable)}` instead");
                }

                if (value == ElfSectionType.SymbolTable)
                {
                    throw new ArgumentException($"Invalid type `{Type}` of the section [{Index}]. Must be used on a `{nameof(ElfSymbolTable)}` instead");
                }
                
                base.Type = value;
            }
        }
        
        public Stream Stream { get; set; }

        public override ulong Size => Stream != null ? (ulong) Stream.Length : 0;

        protected override void Read(ElfReader reader)
        {
            var length = (long) OriginalSize;
            var memoryStream = new MemoryStream((int)length);
            memoryStream.SetLength(length);

            var buffer = memoryStream.GetBuffer();
            reader.Stream.Read(buffer, 0, (int)length);
            
            Stream = memoryStream;
            Stream.Position = 0;

            // TODO: Add support for copy stream if necessary
            //Stream = new SliceStream(reader.Stream, reader.Stream.Position, (long)OriginalSize);
        }

        protected override void Write(ElfWriter writer)
        {
            if (Stream == null) return;
            Stream.Position = 0;
            Stream.CopyTo(writer.Stream);
        }
    }
}