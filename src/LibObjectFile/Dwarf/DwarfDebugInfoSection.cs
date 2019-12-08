// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Collections.Generic;

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

        public void RemoveUnit(DwarfUnit unit)
        {
            _units.Remove<DwarfContainer, DwarfUnit>(this, unit);
        }

        public DwarfUnit RemoveUnitAt(int index)
        {
            return _units.RemoveAt<DwarfContainer, DwarfUnit>(this, index);
        }
    }
}