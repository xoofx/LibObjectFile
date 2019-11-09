using System;

namespace LibObjectFile.Elf
{
    public readonly partial struct ElfArch : IEquatable<ElfArch>
    {
        public ElfArch(ushort value)
        {
            Value = value;
        }

        public readonly ushort Value;

        public override string ToString()
        {
            return $"{ToStringInternal()} (0x{Value:X4})";
        }

        public bool Equals(ElfArch other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is ElfArch other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public static bool operator ==(ElfArch left, ElfArch right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ElfArch left, ElfArch right)
        {
            return !left.Equals(right);
        }
    }
}