// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.Elf
{
    /// <summary>
    /// A shadow section is a section that will not be saved to the section header table but can contain data
    /// that will be saved with the <see cref="ElfObjectFile"/>.
    /// A shadow section is usually associated with an <see cref="ElfSegment"/> that is referencing a portion of
    /// data that is not owned by a visible section.
    /// </summary>
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
}