using System;

namespace LibObjectFile.Elf
{
    public readonly partial struct ElfRelocationType : IEquatable<ElfRelocationType>
    {
        public ElfRelocationType(ElfArch arch, uint value)
        {
            Arch = arch;
            Value = value;
        }

        public readonly ElfArch Arch;

        public readonly uint Value;

        public bool Equals(ElfRelocationType other)
        {
            return Arch.Equals(other.Arch) && Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is ElfRelocationType other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Arch.GetHashCode() * 397) ^ (int) Value;
            }
        }

        public static bool operator ==(ElfRelocationType left, ElfRelocationType right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ElfRelocationType left, ElfRelocationType right)
        {
            return !left.Equals(right);
        }

        public string Name => ToStringInternal();

        public override string ToString()
        {
            if (Arch.Value == 0 && Value == 0) return "Empty ElfRelocationType";

            return $"{ToStringInternal()} ({Value})";
        }
    }
}