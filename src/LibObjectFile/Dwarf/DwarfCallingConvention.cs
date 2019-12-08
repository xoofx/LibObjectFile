// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.Dwarf
{
    public readonly partial struct DwarfCallingConvention : IEquatable<DwarfCallingConvention>
    {
        public DwarfCallingConvention(byte value)
        {
            Value = value;
        }
        
        public readonly byte Value;

        public override string ToString()
        {
            return ToStringInternal() ?? $"Unknown {nameof(DwarfCallingConvention)} (0x{Value:x2})";
        }

        public bool Equals(DwarfCallingConvention other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is DwarfCallingConvention other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public static bool operator ==(DwarfCallingConvention left, DwarfCallingConvention right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DwarfCallingConvention left, DwarfCallingConvention right)
        {
            return !left.Equals(right);
        }
    }
}