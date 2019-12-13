// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Collections.Generic;
using System.IO;

namespace LibObjectFile.Dwarf
{
    public sealed class DwarfInfoSection : DwarfSection
    {
        private readonly List<DwarfUnit> _units;

        public DwarfInfoSection()
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

        internal void Read(DwarfReader reader, Stream stream, DwarfUnitKind defaultUnitKind)
        {
            if (stream == null) return;

            var previousStream = reader.Stream;
            reader.Stream = stream;
            try
            {
                reader.Read(this, defaultUnitKind);
            }
            finally
            {
                reader.Stream = previousStream;
            }
        }

        public override bool TryUpdateLayout(DiagnosticBag diagnostics)
        {
            return true;
        }

        internal void Write(DwarfWriter writer, Stream stream)
        {
            if (stream == null) return;

            var previousStream = stream;
            writer.Stream = stream;
            try
            {
                writer.Write(this);
            } 
            finally
            {
                writer.Stream = previousStream;
            }
        }
    }
}