// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.Elf
{
    public readonly struct ElfSectionLink : IEquatable<ElfSectionLink>
    {
        public static readonly ElfSectionLink Empty = new ElfSectionLink(RawElf.SHN_UNDEF);

        public static readonly ElfSectionLink SectionAbsolute = new ElfSectionLink(RawElf.SHN_ABS);

        public static readonly ElfSectionLink SectionCommon = new ElfSectionLink(RawElf.SHN_COMMON);
        
        public ElfSectionLink(uint index)
        {
            Section = null;
            SpecialSectionIndex = index;
        }

        public ElfSectionLink(ElfSection section)
        {
            Section = section;
            SpecialSectionIndex = 0;
        }

        public readonly ElfSection Section;

        public readonly uint SpecialSectionIndex;

        /// <summary>
        /// <c>true</c> if this link to a section is a special section.
        /// </summary>
        public bool IsSpecial => Section == null && (SpecialSectionIndex == RawElf.SHN_UNDEF || SpecialSectionIndex >= RawElf.SHN_LORESERVE);
        
        public uint GetIndex()
        {
            return Section?.SectionIndex ?? SpecialSectionIndex;
        }

        public bool Equals(ElfSectionLink other)
        {
            return Equals(Section, other.Section) && SpecialSectionIndex == other.SpecialSectionIndex;
        }

        public override bool Equals(object obj)
        {
            return obj is ElfSectionLink other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Section != null ? Section.GetHashCode() : 0) * 397) ^ SpecialSectionIndex.GetHashCode();
            }
        }

        public static bool operator ==(ElfSectionLink left, ElfSectionLink right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ElfSectionLink left, ElfSectionLink right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            if (Section != null)
            {
                return Section.ToString();
            }

            if (SpecialSectionIndex == 0) return "Special Section Undefined";

            if (SpecialSectionIndex > RawElf.SHN_BEFORE)
            {
                if (SpecialSectionIndex == RawElf.SHN_ABS)
                {
                    return "Special Section Absolute";
                }
                
                if (SpecialSectionIndex == RawElf.SHN_COMMON)
                {
                    return "Special Section Common";
                }

                if (SpecialSectionIndex == RawElf.SHN_XINDEX)
                {
                    return "Special Section XIndex";
                }
            }

            return $"Unknown Section Value 0x{SpecialSectionIndex:X8}";
        }

        public static implicit operator ElfSectionLink(ElfSection section)
        {
            return new ElfSectionLink(section);
        }
        
        
        public bool TryGetSectionSafe<TSection>(string className, string propertyName, object context, DiagnosticBag diagnostics, out TSection section, params ElfSectionType[] sectionTypes) where TSection : ElfSection
        {
            section = null;

            if (Section == null)
            {
                diagnostics.Error(DiagnosticId.ELF_ERR_LinkOrInfoSectionNull, $"`{className}.{propertyName}` cannot be null for this instance", context);
                return false;
            }

            bool foundValid = false;
            foreach (var elfSectionType in sectionTypes)
            {
                if (Section.Type == elfSectionType)
                {
                    foundValid = true;
                    break;
                }
            }

            if (!foundValid)
            {
                diagnostics.Error(DiagnosticId.ELF_ERR_LinkOrInfoInvalidSectionType, $"The type `{Section.Type}` of `{className}.{propertyName}` must be a {string.Join(" or ", sectionTypes)}", context);
                return false;
            }
            section = Section as TSection;

            if (section == null)
            {
                diagnostics.Error(DiagnosticId.ELF_ERR_LinkOrInfoInvalidSectionInstance, $"The `{className}.{propertyName}` must be an instance of {typeof(TSection).Name}");
                return false;
            }
            return true;
        }
    }
}