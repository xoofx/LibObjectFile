// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Diagnostics;

namespace LibObjectFile.Dwarf
{
    [DebuggerDisplay("{Name} {Form}")]
    public readonly struct DwarfAttributeDescriptor : IEquatable<DwarfAttributeDescriptor>
    {
        public static readonly DwarfAttributeDescriptor Empty = new DwarfAttributeDescriptor();

        public DwarfAttributeDescriptor(DwarfAttributeName name, DwarfAttributeForm form)
        {
            Name = name;
            Form = form;
        }
        
        public readonly DwarfAttributeName Name;

        public readonly DwarfAttributeForm Form;

        public bool IsNull => Name.Value == 0 && Form.Value == 0;

        public bool Equals(DwarfAttributeDescriptor other)
        {
            return Name.Equals(other.Name) && Form.Equals(other.Form);
        }

        public override bool Equals(object obj)
        {
            return obj is DwarfAttributeDescriptor other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Name.GetHashCode() * 397) ^ Form.GetHashCode();
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