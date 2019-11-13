using System;

namespace LibObjectFile.Elf
{
    public struct ElfSegment : IEquatable<ElfSegment>
    {
        public static readonly ElfSegment Empty = new ElfSegment();

        public bool IsEmpty => this == Empty;

        public ElfSegmentType Type { get; set; }

        public ElfOffset Offset { get; set; }

        public ulong VirtualAddress { get; set; }

        public ulong PhysicalAddress { get; set; }

        public ulong SizeInFile { get; set; } 

        public ulong SizeInMemory { get; set; }

        public ElfSegmentFlags Flags { get; set; }

        public ulong Align { get; set; }
        
        public override string ToString()
        {
            return $"{nameof(Type)}: {Type}, {nameof(Offset)}: ({Offset}), {nameof(VirtualAddress)}: 0x{VirtualAddress:X16}, {nameof(PhysicalAddress)}: 0x{PhysicalAddress:X16}, {nameof(SizeInFile)}: {SizeInFile}, {nameof(SizeInMemory)}: {SizeInMemory}, {nameof(Flags)}: {Flags}, {nameof(Align)}: {Align}";
        }

        public bool Equals(ElfSegment other)
        {
            return Type.Equals(other.Type) && Offset.Equals(other.Offset) && VirtualAddress == other.VirtualAddress && PhysicalAddress == other.PhysicalAddress && SizeInFile == other.SizeInFile && SizeInMemory == other.SizeInMemory && Flags.Equals(other.Flags) && Align == other.Align;
        }

        public override bool Equals(object obj)
        {
            return obj is ElfSegment other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Type.GetHashCode();
                hashCode = (hashCode * 397) ^ Offset.GetHashCode();
                hashCode = (hashCode * 397) ^ VirtualAddress.GetHashCode();
                hashCode = (hashCode * 397) ^ PhysicalAddress.GetHashCode();
                hashCode = (hashCode * 397) ^ SizeInFile.GetHashCode();
                hashCode = (hashCode * 397) ^ SizeInMemory.GetHashCode();
                hashCode = (hashCode * 397) ^ Flags.GetHashCode();
                hashCode = (hashCode * 397) ^ Align.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(ElfSegment left, ElfSegment right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ElfSegment left, ElfSegment right)
        {
            return !left.Equals(right);
        }
    }
}