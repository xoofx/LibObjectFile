// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.Dwarf
{
    public class DwarfCompilationUnit : DwarfUnit
    {
        private DwarfDIE _root;

        public DwarfCompilationUnit()
        {
        }

        public bool Is64 { get; set; }

        public ushort Version { get; set; }
        
        public byte AddressSize { get; set; }

        public DwarfDIE Root
        {
            get => _root;
            set
            {
                if (_root != null)
                {
                    _root.Parent = null;
                }

                if (value?.Parent != null) throw new InvalidOperationException($"Cannot set the {value.GetType()} as it already belongs to another {value.Parent.GetType()} instance");
                _root = value;

                if (value != null)
                {
                    value.Parent = this;
                }
            }
        }
    }
}