// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Collections.Generic;

namespace LibObjectFile.Dwarf
{
    public abstract class DwarfRelocatableSection : DwarfSection
    {
        protected DwarfRelocatableSection()
        {
            Relocations = new List<DwarfRelocation>();
        }


        public List<DwarfRelocation> Relocations { get; }
    }
}