// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;

namespace LibObjectFile.Dwarf
{
    public partial class DwarfDebugInfoSection : DwarfSection
    {
        private readonly List<DwarfUnit> _units;

        public DwarfDebugInfoSection()
        {
            _units = new List<DwarfUnit>();
        }
        
        public IReadOnlyList<DwarfUnit> Units => _units;
        
        public void AddUnit(DwarfUnit unit)
        {
            _units.Add<DwarfContainer, DwarfUnit>(this, unit);
        }
        
        public override bool TryUpdateLayout(DiagnosticBag diagnostics)
        {
            return true;
        }

        internal void Write(DwarfReaderWriter writer)
        {
            
        }
    }
}