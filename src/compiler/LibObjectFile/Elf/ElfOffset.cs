using System;

namespace LibObjectFile.Elf
{
    public struct ElfOffset : IEquatable<ElfOffset>
    {
        internal ElfOffset(ulong offset)
        {
            Section = null;
            Delta = offset;
        }

        public ElfOffset(ElfSection section, ulong relativeOffset)
        {
            Section = section ?? throw new ArgumentNullException(nameof(section));
            Delta = relativeOffset;
        }

        public readonly ElfSection Section;

        public readonly ulong Delta;
        
        public ulong Value => Section?.Offset + Delta ?? Delta;

        public bool IsRelative => Section != null;

        public override string ToString()
        {
            return IsRelative ? $"{nameof(Section)}: {Section}, {nameof(Delta)}: 0x{Delta:X16}" : $"Offset: 0x{Delta:X16}";
        }

        public bool Equals(ElfOffset other)
        {
            return Equals(Section, other.Section) && Delta == other.Delta;
        }

        public override bool Equals(object obj)
        {
            return obj is ElfOffset other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Section != null ? Section.GetHashCode() : 0) * 397) ^ Delta.GetHashCode();
            }
        }

        public static bool operator ==(ElfOffset left, ElfOffset right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ElfOffset left, ElfOffset right)
        {
            return !left.Equals(right);
        }
    }
}