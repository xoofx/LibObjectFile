// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

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
            set => AttachChild<DwarfContainer, DwarfDIE>(this, value, ref _root);
        }
    }
}