using System;

namespace LibObjectFile.Elf
{
    public readonly partial struct ElfOSAbi : IEquatable<ElfOSAbi>
    {
        public ElfOSAbi(byte value)
        {
            Value = value;
        }

        public readonly byte Value;

        public override string ToString()
        {
            return $"{ToStringInternal()} (0x{Value:X4})";
        }

        public bool Equals(ElfOSAbi other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is ElfOSAbi other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public static bool operator ==(ElfOSAbi left, ElfOSAbi right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ElfOSAbi left, ElfOSAbi right)
        {
            return !left.Equals(right);
        }
    }
}