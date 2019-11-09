using System;

namespace LibObjectFile.Elf
{
    public struct ElfSectionOffset : IEquatable<ElfSectionOffset>
    {
        public ElfSectionOffset(ElfSection section, ulong offset)
        {
            Section = section ?? throw new ArgumentNullException(nameof(section));
            LocalOffset = offset;
        }

        public readonly ElfSection Section;

        public readonly ulong LocalOffset;

        public override string ToString()
        {
            return $"{nameof(Section)}: {Section}, {nameof(LocalOffset)}: 0x{LocalOffset:X16}";
        }

        public bool Equals(ElfSectionOffset other)
        {
            return Equals(Section, other.Section) && LocalOffset == other.LocalOffset;
        }

        public override bool Equals(object obj)
        {
            return obj is ElfSectionOffset other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Section != null ? Section.GetHashCode() : 0) * 397) ^ LocalOffset.GetHashCode();
            }
        }

        public static bool operator ==(ElfSectionOffset left, ElfSectionOffset right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ElfSectionOffset left, ElfSectionOffset right)
        {
            return !left.Equals(right);
        }
    }
}