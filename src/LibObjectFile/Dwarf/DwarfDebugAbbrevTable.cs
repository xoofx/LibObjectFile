// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Collections.Generic;

namespace LibObjectFile.Dwarf
{
    public class DwarfDebugAbbrevTable : DwarfSection
    {
        private readonly List<DwarfAbbreviation> _abbreviations;

        public DwarfDebugAbbrevTable()
        {
            _abbreviations = new List<DwarfAbbreviation>();
        }

        public IReadOnlyList<DwarfAbbreviation> Abbreviations => _abbreviations;

        public void AddAbbreviation(DwarfAbbreviation abbreviation)
        {
            _abbreviations.Add(this, abbreviation);
        }

        public void RemoveAbbreviation(DwarfAbbreviation abbreviation)
        {
            _abbreviations.Remove(this, abbreviation);
        }

        public DwarfAbbreviation RemoveAbbreviationAt(int index)
        {
            return _abbreviations.RemoveAt(this, index);
        }

        public override bool TryUpdateLayout(DiagnosticBag diagnostics)
        {
            return true;
        }
    }
}