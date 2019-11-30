// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Diagnostics;

namespace LibObjectFile.Dwarf
{
    [DebuggerDisplay("{Key} {Form}")]
    public readonly struct DwarfAttributeDescriptor : IEquatable<DwarfAttributeDescriptor>
    {
        public static readonly DwarfAttributeDescriptor Empty = new DwarfAttributeDescriptor();

        public DwarfAttributeDescriptor(DwarfAttributeKey key, DwarfAttributeForm form)
        {
            Key = key;
            Form = form;
        }
        
        public readonly DwarfAttributeKey Key;

        public readonly DwarfAttributeForm Form;

        public bool IsNull => Key.Value == 0 && Form.Value == 0;

        public bool Equals(DwarfAttributeDescriptor other)
        {
            return Key.Equals(other.Key) && Form.Equals(other.Form);
        }

        public override bool Equals(object obj)
        {
            return obj is DwarfAttributeDescriptor other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Key.GetHashCode() * 397) ^ Form.GetHashCode();
            }
        }

        public static bool operator ==(DwarfAttributeDescriptor left, DwarfAttributeDescriptor right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DwarfAttributeDescriptor left, DwarfAttributeDescriptor right)
        {
            return !left.Equals(right);
        }
    }
}