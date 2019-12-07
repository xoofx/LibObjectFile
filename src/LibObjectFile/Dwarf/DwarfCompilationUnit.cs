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

        public bool Is64BitEncoding { get; set; }

        public bool Is64BitAddress { get; set; }

        public ushort Version { get; set; }
        
        /// <summary>
        /// Gets or sets the root <see cref="DwarfDIE"/> of this compilation unit.
        /// </summary>
        public DwarfDIE Root
        {
            get => _root;
            set => AttachChild<DwarfContainer, DwarfDIE>(this, value, ref _root);
        }

        /// <summary>
        /// Gets or sets the abbreviation associated with the <see cref="Root"/> <see cref="DwarfDIE"/>
        /// </summary>
        public DwarfAbbreviation Abbreviation { get; set; }
    }
}