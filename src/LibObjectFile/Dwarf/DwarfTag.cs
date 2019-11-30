// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.Dwarf
{
    /// <summary>
    /// Defines the tag of an <see cref="DwarfDIE"/>.
    /// </summary>
    public readonly partial struct DwarfTag : IEquatable<DwarfTag>
    {
        public DwarfTag(uint value)
        {
            Value = value;
        }

        public readonly uint Value;


        public bool Equals(DwarfTag other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is DwarfTag other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (int) Value;
        }

        public static bool operator ==(DwarfTag left, DwarfTag right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DwarfTag left, DwarfTag right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return ToStringInternal() ?? $"Unknown {nameof(DwarfTag)} (0x{Value:X4})";
        }

        public static explicit operator uint(DwarfTag flags) => flags.Value;

        public static implicit operator DwarfTag(uint flags) => new DwarfTag(flags);
    }
}