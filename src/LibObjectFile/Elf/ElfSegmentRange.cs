using System;

namespace LibObjectFile.Elf
{
    public readonly struct ElfSegmentRange : IEquatable<ElfSegmentRange>
    {
        public static readonly ElfSegmentRange Empty = new ElfSegmentRange();

        public ElfSegmentRange(ElfSection section)
        {
            BeginSection = section ?? throw new ArgumentNullException(nameof(section));
            BeginOffset = 0;
            EndSection = section;
            EndOffset = -1;
        }

        public ElfSegmentRange(ElfSection beginSection, ulong beginOffset, ElfSection endSection, long endOffset)
        {
            BeginSection = beginSection ?? throw new ArgumentNullException(nameof(beginSection));
            BeginOffset = beginOffset;
            EndSection = endSection ?? throw new ArgumentNullException(nameof(endSection));
            EndOffset = endOffset;
            if (BeginSection.Index > EndSection.Index)
            {
                throw new ArgumentOutOfRangeException(nameof(beginSection), $"The {nameof(beginSection)}.{nameof(ElfSection.Index)} = {BeginSection.Index} is > {nameof(endSection)}.{nameof(ElfSection.Index)} = {EndSection.Index}, while it must be <=");
            }
        }
        
        public readonly ElfSection BeginSection;

        public readonly ulong BeginOffset;

        public readonly ElfSection EndSection;

        public readonly long EndOffset;

        public bool IsEmpty => this == Empty;

        public ulong Offset
        {
            get
            {
                // If this Begin/End section are not attached we can't calculate any meaningful size
                if (BeginSection?.Parent == null || EndSection?.Parent == null || BeginSection?.Parent != EndSection?.Parent)
                {
                    return 0;
                }

                return BeginSection.Offset + BeginOffset;
            }
        }

        public ulong Size
        {
            get
            {
                // If this Begin/End section are not attached we can't calculate any meaningful size
                if (BeginSection?.Parent == null || EndSection?.Parent == null || BeginSection?.Parent != EndSection?.Parent)
                {
                    return 0;
                }

                var parent = BeginSection.Parent;
                ulong size = 0;
                for (uint i = BeginSection.Index; i < EndSection.Index; i++)
                {
                    var section = parent.Sections[(int)i];
                    if (section.HasContent)
                    {
                        size += section.Size;
                    }
                }

                size -= BeginOffset;
                size += EndOffset < 0 ? (ulong)((long)EndSection.Size + EndOffset + 1) : (ulong)(EndOffset + 1);
                return size;
            }
        }
        
        public bool Equals(ElfSegmentRange other)
        {
            return Equals(BeginSection, other.BeginSection) && BeginOffset == other.BeginOffset && Equals(EndSection, other.EndSection) && EndOffset == other.EndOffset;
        }

        public override bool Equals(object obj)
        {
            return obj is ElfSegmentRange other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (BeginSection != null ? BeginSection.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ BeginOffset.GetHashCode();
                hashCode = (hashCode * 397) ^ (EndSection != null ? EndSection.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ EndOffset.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(ElfSegmentRange left, ElfSegmentRange right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ElfSegmentRange left, ElfSegmentRange right)
        {
            return !left.Equals(right);
        }

        public static implicit operator ElfSegmentRange(ElfSection section)
        {
            return section == null ? Empty : new ElfSegmentRange(section);
        }
    }
}