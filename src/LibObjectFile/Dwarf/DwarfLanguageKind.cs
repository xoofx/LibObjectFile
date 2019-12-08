// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.Dwarf
{
    public readonly partial struct DwarfLanguageKind : IEquatable<DwarfLanguageKind>
    {
        public DwarfLanguageKind(ushort value)
        {
            Value = value;
        }
        
        public readonly ushort Value;

        public override string ToString()
        {
            return ToStringInternal() ?? $"Unknown {nameof(DwarfLanguageKind)} (0x{Value:x4})";
        }

        public bool Equals(DwarfLanguageKind other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is DwarfLanguageKind other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public static bool operator ==(DwarfLanguageKind left, DwarfLanguageKind right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DwarfLanguageKind left, DwarfLanguageKind right)
        {
            return !left.Equals(right);
        }
    }
}