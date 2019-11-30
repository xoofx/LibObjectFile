// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Collections.Generic;
using System.IO;

namespace LibObjectFile.Dwarf
{
    public class DwarfDebugAbbrevTable : DwarfSection
    {
        private readonly Dictionary<ulong, DwarfAbbreviation> _offsetToAbbreviation;

        public DwarfDebugAbbrevTable()
        {
            _offsetToAbbreviation = new Dictionary<ulong, DwarfAbbreviation>();
        }

        internal DwarfAbbreviation Read(Stream stream, ulong abbreviationOffset)
        {
            if (_offsetToAbbreviation.TryGetValue(abbreviationOffset, out var abbreviation))
            {
                return abbreviation;
            }
            
            abbreviation = DwarfAbbreviation.Read(stream, abbreviationOffset);
            _offsetToAbbreviation[abbreviationOffset] = abbreviation;
            return abbreviation;
        }

        public override bool TryUpdateLayout(DiagnosticBag diagnostics)
        {
            return true;
        }
    }
}