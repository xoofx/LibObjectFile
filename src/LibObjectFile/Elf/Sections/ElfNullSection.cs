// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.Elf
{
    /// <summary>
    /// A null section with the type <see cref="ElfSectionType.Null"/>.
    /// </summary>
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