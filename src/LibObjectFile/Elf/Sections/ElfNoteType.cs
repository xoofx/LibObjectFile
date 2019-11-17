// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.Elf
{
    /// <summary>
    /// Gets the type of a <see cref="ElfNote"/>.
    /// </summary>
    public readonly partial struct ElfNoteType : IEquatable<ElfNoteType>
    {
        public ElfNoteType(uint value)
        {
            Value = value;
        }

        /// <summary>
        /// The value of this note type.
        /// </summary>
        public readonly uint Value;

        public override string ToString()
        {
            return $"{ToStringInternal()} 0x{Value:x8}";
        }

        public bool Equals(ElfNoteType other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is ElfNoteType other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (int) Value;
        }

        public static bool operator ==(ElfNoteType left, ElfNoteType right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ElfNoteType left, ElfNoteType right)
        {
            return !left.Equals(right);
        }
    }
}