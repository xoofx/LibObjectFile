using System;

namespace LibObjectFile.Elf
{
    public sealed class ElfNullSection : ElfSection
    {
        public override ElfSectionType Type
        {
            get => ElfSectionType.Null;
            set => throw CannotModifyNullSection();
        }

        public override ElfSectionFlags Flags
        {
            get => ElfSectionFlags.None;
            set => throw CannotModifyNullSection();
        }

        public override ElfString Name
        {
            get => null;
            set => throw CannotModifyNullSection();
        }

        public override ulong VirtualAddress
        {
            get => 0;
            set => throw CannotModifyNullSection();
        }

        public override ulong Alignment
        {
            get => 0;
            set => throw CannotModifyNullSection();
        }


        public override ElfSectionLink Link
        {
            get => ElfSectionLink.Empty;
            set => throw CannotModifyNullSection();
        }


        public override ElfSectionLink Info
        {
            get => ElfSectionLink.Empty;
            set => throw CannotModifyNullSection();
        }

        public override ulong Offset
        {
            get => 0;
            set => throw CannotModifyNullSection();
        }

        public override ulong Size
        {
            get => 0;
            set => throw CannotModifyNullSection();
        }

        public override bool HasContent => false;

        protected override void Read(ElfReader reader)
        {
        }

        protected override void Write(ElfWriter writer)
        {
        }

        private static InvalidOperationException CannotModifyNullSection()
        {
            return new InvalidOperationException("Cannot modify a Null Section");
        }
    }
}