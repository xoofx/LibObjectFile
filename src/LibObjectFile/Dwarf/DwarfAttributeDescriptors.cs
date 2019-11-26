// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace LibObjectFile.Dwarf
{
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
    public readonly struct DwarfAttributeDescriptors : IEquatable<DwarfAttributeDescriptors>
    {
        private readonly List<DwarfAttributeDescriptor> _descriptors;

        public DwarfAttributeDescriptors(List<DwarfAttributeDescriptor> descriptors)
        {
            _descriptors = descriptors;
        }
        
        public bool Equals(DwarfAttributeDescriptors other)
        {
            if (ReferenceEquals(_descriptors, other._descriptors)) return true;
            if (_descriptors == null || other._descriptors == null) return false;
            if (_descriptors.Count != other._descriptors.Count) return false;

            for (int i = 0; i < _descriptors.Count; i++)
            {
                if (_descriptors[i] != other._descriptors[i])
                {
                    return false;
                }
            }
            return true;
        }

        public override bool Equals(object obj)
        {
            return obj is DwarfAttributeDescriptors other && Equals(other);
        }

        public override int GetHashCode()
        {
            int hashCode = _descriptors == null ? 0 : _descriptors.Count;
            if (hashCode == 0) return hashCode;
            foreach (var descriptor in _descriptors)
            {
                hashCode = (hashCode * 397) ^ descriptor.GetHashCode();
            }
            return hashCode;
        }

        private string DebuggerDisplay => $"Count = {_descriptors.Count}";

        public static bool operator ==(DwarfAttributeDescriptors left, DwarfAttributeDescriptors right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DwarfAttributeDescriptors left, DwarfAttributeDescriptors right)
        {
            return !left.Equals(right);
        }
    }
}