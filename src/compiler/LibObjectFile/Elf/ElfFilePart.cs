using System;
using System.Diagnostics;

namespace LibObjectFile.Elf
{
    [DebuggerDisplay("{StartOffset,nq} - {EndOffset,nq} : {Section,nq}")]
    internal readonly struct ElfFilePart : IComparable<ElfFilePart>, IEquatable<ElfFilePart>
    {
        public ElfFilePart(ulong startOffset, ulong endOffset)
        {
            StartOffset = startOffset;
            EndOffset = endOffset;
            Section = null;
        }

        public ElfFilePart(ElfSection section)
        {
            Section = section ?? throw new ArgumentNullException(nameof(section));
            Debug.Assert(section.Size > 0);
            StartOffset = section.Offset;
            EndOffset = StartOffset + Section.Size - 1;
        }
        
        public readonly ulong StartOffset;

        public readonly ulong EndOffset;
        
        public readonly ElfSection Section;

        public int CompareTo(ElfFilePart other)
        {
            if (EndOffset < other.StartOffset)
            {
                return -1;
            }

            if (StartOffset > other.EndOffset)
            {
                return 1;
            }

            // May overlap or not
            return 0;
        }


        public bool Equals(ElfFilePart other)
        {
            return StartOffset == other.StartOffset && EndOffset == other.EndOffset;
        }

        public override bool Equals(object obj)
        {
            return obj is ElfFilePart other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (StartOffset.GetHashCode() * 397) ^ EndOffset.GetHashCode();
            }
        }

        public static bool operator ==(ElfFilePart left, ElfFilePart right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ElfFilePart left, ElfFilePart right)
        {
            return !left.Equals(right);
        }
    }
}