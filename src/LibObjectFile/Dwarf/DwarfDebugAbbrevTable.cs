// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;

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

        public void Reset()
        {
            foreach(var abbreviation in _abbreviations)
            {
                abbreviation.Reset();
            }
            _abbreviations.Clear();
        }

        public override bool TryUpdateLayout(DiagnosticBag diagnostics)
        {
            ulong offset = 0;
            foreach (var abbreviation in _abbreviations)
            {
                abbreviation.Offset = offset;
                if (!abbreviation.TryUpdateLayout(diagnostics))
                {
                    return false;
                }
                offset += abbreviation.Size;
            }

            Size = offset;

            return true;
        }

        internal void Write(DwarfWriter writer)
        {
            if (writer.Context.DebugAbbrevStream.Stream == null)
            {
                return;
            }

            var previousStream = writer.Stream;
            writer.Stream = writer.Context.DebugAbbrevStream;
            try
            {
                var startOffset = writer.Offset;
                foreach (var abbreviation in _abbreviations)
                {
                    abbreviation.Write(writer);
                }

                Debug.Assert(writer.Offset - startOffset == Size);
            }
            finally
            {
                writer.Stream = previousStream;
            }
        }
    }
}