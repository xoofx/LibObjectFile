// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Diagnostics;

namespace LibObjectFile.Dwarf
{
    /// <summary>
    /// Defines the kind of an <see cref="DwarfAttribute"/>.
    /// This is the value seen in <see cref="DwarfNative.DW_AT_ALTIUM_loclist"/>
    /// </summary>
    [DebuggerDisplay("{ToString(),nq}")]
    public readonly partial struct DwarfAttributeKey : IEquatable<DwarfAttributeKey>
    {
        public DwarfAttributeKey(uint value)
        {
            Value = value;
        }

        public readonly uint Value;


        public bool Equals(DwarfAttributeKey other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is DwarfAttributeKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (int) Value;
        }

        public static bool operator ==(DwarfAttributeKey left, DwarfAttributeKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DwarfAttributeKey left, DwarfAttributeKey right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return ToStringInternal() ?? $"Unknown {nameof(DwarfAttributeKey)} (0x{Value:X4})";
        }

        public static explicit operator uint(DwarfAttributeKey flags) => flags.Value;

        public static implicit operator DwarfAttributeKey(uint flags) => new DwarfAttributeKey(flags);
    }
}