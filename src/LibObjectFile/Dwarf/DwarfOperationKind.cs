// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.Dwarf
{
    public readonly partial struct DwarfOperationKind : IEquatable<DwarfOperationKind>
    {
        public DwarfOperationKind(byte value)
        {
            Value = value;
        }
        
        public readonly byte Value;

        public override string ToString()
        {
            return ToStringInternal() ?? $"Unknown {nameof(DwarfOperationKind)} (0x{Value:X4})";
        }

        public bool Equals(DwarfOperationKind other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is DwarfOperationKind other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public static bool operator ==(DwarfOperationKind left, DwarfOperationKind right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DwarfOperationKind left, DwarfOperationKind right)
        {
            return !left.Equals(right);
        }
    }
}