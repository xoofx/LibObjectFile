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

        public override ulong TableEntrySize => OriginalTableEntrySize;
        
        public Stream Stream { get; set; }
        
        protected override ulong GetSizeAuto() => Stream != null ? (ulong)Stream.Length : 0;

        protected override void Read(ElfReader reader)
        {
            Stream = reader.ReadAsStream(Size);
            SizeKind = ElfValueKind.Auto;
        }

        protected override void Write(ElfWriter writer)
        {
            if (Stream == null) return;
            Stream.Position = 0;
            writer.Write(Stream);
        }
    }
}