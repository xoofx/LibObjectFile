// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.Elf
{
    /// <summary>
    /// Defines an OS ABI.
    /// This is the value seen in the ident part of an Elf header at index <see cref="ElfNative.EI_OSABI"/>
    /// as well as the various machine defines (e.g <see cref="ElfNative.ELFOSABI_LINUX"/>).
    /// </summary>
    public readonly partial struct ElfOSABI : IEquatable<ElfOSABI>
    {
        public ElfOSABI(byte value)
        {
            Value = value;
        }

        public readonly byte Value;

        public override string ToString()
        {
            return $"{ToStringInternal()} (0x{Value:X4})";
        }

        public bool Equals(ElfOSABI other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is ElfOSABI other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public static bool operator ==(ElfOSABI left, ElfOSABI right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ElfOSABI left, ElfOSABI right)
        {
            return !left.Equals(right);
        }
    }
}