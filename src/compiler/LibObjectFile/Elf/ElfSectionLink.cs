using System;

namespace LibObjectFile.Elf
{
    public readonly struct ElfSectionLink : IEquatable<ElfSectionLink>
    {
        public static ElfSectionLink SectionAbsolute = new ElfSectionLink(RawElf.SHN_ABS);

        public static ElfSectionLink SectionCommon = new ElfSectionLink(RawElf.SHN_COMMON);


        public ElfSectionLink(uint specialSectionIndex)
        {
            Section = null;
            SpecialSectionIndex = specialSectionIndex;
        }

        public ElfSectionLink(ElfSection section)
        {
            Section = section;
            SpecialSectionIndex = 0;
        }

        public readonly ElfSection Section;

        public readonly uint SpecialSectionIndex;

        public uint GetSectionIndex()
        {
            return Section?.Index ?? SpecialSectionIndex;
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
    }
}