﻿// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using LibObjectFile.Utils;

namespace LibObjectFile.Dwarf
{
    public class DwarfAbbreviationTable : DwarfSection
    {
        private readonly List<DwarfAbbreviation> _abbreviations;

        public DwarfAbbreviationTable()
        {
            _abbreviations = new List<DwarfAbbreviation>();
        }

        public ReadOnlyList<DwarfAbbreviation> Abbreviations => _abbreviations;

        internal void AddAbbreviation(DwarfAbbreviation abbreviation)
        {
            _abbreviations.Add(this, abbreviation);
        }

        internal void RemoveAbbreviation(DwarfAbbreviation abbreviation)
        {
            _abbreviations.Remove(this, abbreviation);
        }

        internal DwarfAbbreviation RemoveAbbreviationAt(int index)
        {
            return _abbreviations.RemoveAt(this, index);
        }

        internal void Reset()
        {
            foreach(var abbreviation in _abbreviations)
            {
                abbreviation.Reset();
            }
            _abbreviations.Clear();
        }
        
        protected override void UpdateLayout(DwarfLayoutContext layoutContext)
        {
            ulong endOffset = Position;
            foreach (var abbreviation in _abbreviations)
            {
                abbreviation.Position = endOffset;
                abbreviation.UpdateLayoutInternal(layoutContext);
                endOffset += abbreviation.Size;
            }

            Size = endOffset - Position;
        }

        protected override void Read(DwarfReader reader)
        {
            var endOffset = reader.Position;
            while (reader.Position < reader.Length)
            {
                var abbreviation = new DwarfAbbreviation
                {
                    Position = endOffset
                };
                abbreviation.ReadInternal(reader);
                endOffset += abbreviation.Size;
                AddAbbreviation(abbreviation);
            }

            Size = endOffset - Position;
        }

        protected override void Write(DwarfWriter writer)
        {
            var startOffset = writer.Position;
            foreach (var abbreviation in _abbreviations)
            {
                abbreviation.WriteInternal(writer);
            }

            Debug.Assert(writer.Position - startOffset == Size);
        }
    }
}