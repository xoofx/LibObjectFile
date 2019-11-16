using System;
using System.IO;

namespace LibObjectFile.Elf
{
    public abstract class ElfShadowSection : ElfSection
    {
        protected ElfShadowSection() : base(ElfSectionType.Null)
        {
        }

        public override ElfSectionType Type
        {
            get => base.Type;
            set
            {
                if (value != ElfSectionType.Null) throw new InvalidOperationException($"Cannot change the type of a {nameof(ElfShadowSection)}");
            }
        }

        public override ElfSectionFlags Flags
        {
            get => ElfSectionFlags.None;
            set => throw CannotModifyThisPropertyForShadow();
        }

        public override ulong VirtualAddress
        {
            get => 0;
            set => throw CannotModifyThisPropertyForShadow();
        }

        public override ulong Alignment
        {
            get => 0;
            set => throw CannotModifyThisPropertyForShadow();
        }

        private static InvalidOperationException CannotModifyThisPropertyForShadow()
        {
            return new InvalidOperationException($"Cannot modify this property for a {nameof(ElfShadowSection)}");
        }
    }


    public sealed class ElfCustomShadowSection : ElfShadowSection
    {
        public ElfCustomShadowSection()
        {
        }

        public Stream Stream { get; set; }

        protected override ulong GetSize() => Stream != null ? (ulong)Stream.Length : 0;

        protected override void Read(ElfReader reader)
        {
            Stream = reader.ReadAsStream(Size);
            SizeKind = ElfValueKind.Absolute;
        }

        protected override void Write(ElfWriter writer)
        {
            if (Stream == null) return;
            Stream.Position = 0;
            writer.Write(Stream);
        }
    }
}