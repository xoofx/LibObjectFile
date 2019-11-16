// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.Elf
{
    public readonly struct ElfHeaderFlags : IEquatable<ElfHeaderFlags>
    {
        public ElfHeaderFlags(uint value)
        {
            Value = value;
        }

        public readonly uint Value;


        public bool Equals(ElfHeaderFlags other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is ElfHeaderFlags other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (int) Value;
        }

        public static bool operator ==(ElfHeaderFlags left, ElfHeaderFlags right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ElfHeaderFlags left, ElfHeaderFlags right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return $"0x{Value:x}";
        }

        public static explicit operator uint(ElfHeaderFlags flags) => flags.Value;

        public static implicit operator ElfHeaderFlags(uint flags) => new ElfHeaderFlags(flags);
    }
}