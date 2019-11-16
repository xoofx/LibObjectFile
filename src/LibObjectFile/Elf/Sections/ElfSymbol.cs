using System;

namespace LibObjectFile.Elf
{
    public struct ElfSymbol : IEquatable<ElfSymbol>
    {
        public static readonly ElfSymbol Empty = new ElfSymbol();

        public ulong Value { get; set; }

        public ulong Size { get; set; }
        
        public ElfSymbolType Type { get; set; }

        public ElfSymbolBind Bind { get; set; }

        public ElfSymbolVisibility Visibility { get; set; }

        public ElfSectionLink Section { get; set; }

        public ElfString Name { get; set; }

        public override string ToString()
        {
            return $"{nameof(Value)}: 0x{Value:X16}, {nameof(Size)}: {Size:#####}, {nameof(Type)}: {Type}, {nameof(Bind)}: {Bind}, {nameof(Visibility)}: {Visibility}, {nameof(Section)}: {Section}, {nameof(Name)}: {Name}";
        }

        public bool Equals(ElfSymbol other)
        {
            return Value == other.Value && Size == other.Size && Type == other.Type && Bind == other.Bind && Visibility == other.Visibility && Section.Equals(other.Section) && Name == other.Name;
        }

        public override bool Equals(object obj)
        {
            return obj is ElfSymbol other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Value.GetHashCode();
                hashCode = (hashCode * 397) ^ Size.GetHashCode();
                hashCode = (hashCode * 397) ^ (int) Type;
                hashCode = (hashCode * 397) ^ (int) Bind;
                hashCode = (hashCode * 397) ^ (int) Visibility;
                hashCode = (hashCode * 397) ^ Section.GetHashCode();
                hashCode = (hashCode * 397) ^ (Name != null ? Name.GetHashCode() : 0);
                return hashCode;
            }
        }

        public static bool operator ==(ElfSymbol left, ElfSymbol right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ElfSymbol left, ElfSymbol right)
        {
            return !left.Equals(right);
        }
    }
}