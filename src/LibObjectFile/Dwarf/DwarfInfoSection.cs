// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using LibObjectFile.Collections;

namespace LibObjectFile.Dwarf;

public sealed class DwarfInfoSection : DwarfRelocatableSection
{
    private readonly ObjectList<DwarfUnit> _units;

    public DwarfInfoSection()
    {
        _units = new ObjectList<DwarfUnit>(this);
    }

    public ObjectList<DwarfUnit> Units => _units;

    public override void Read(DwarfReader reader)
    {
        var addressRangeTable = reader.File.AddressRangeTable;
            
        while (reader.Position < reader.Length)
        {
            // 7.5 Format of Debugging Information
            // - Each such contribution consists of a compilation unit header

            var startOffset = Position;

            reader.ClearResolveAttributeReferenceWithinCompilationUnit();

            var cu = DwarfUnit.ReadInstance(reader, out var offsetEndOfUnit);
            if (cu == null)
            {
                reader.Position = offsetEndOfUnit;
                continue;
            }

            reader.CurrentUnit = cu;

            // Link AddressRangeTable to Unit
            if (addressRangeTable.DebugInfoOffset == cu.Position)
            {
                addressRangeTable.Unit = cu;
            }
                
            _units.Add(cu);
        }

        reader.ResolveAttributeReferenceWithinSection();
    }

    public override void Verify(DwarfVerifyContext context)
    {
        foreach (var unit in _units)
        {
            unit.Verify(context);
        }
    }

    public override void UpdateLayout(DwarfLayoutContext layoutContext)
    {
        var offset = Position;
        foreach (var unit in Units)
        {
            layoutContext.CurrentUnit = unit;
            unit.Position = offset;
            unit.UpdateLayout(layoutContext);
            offset += unit.Size;
        }
        Size = offset - Position;
    }

    public override void Write(DwarfWriter writer)
    {
        Debug.Assert(Position == writer.Position);
        foreach (var unit in _units)
        {
            writer.CurrentUnit = unit;
            unit.Write(writer);
        }
        writer.CurrentUnit = null;
        Debug.Assert(Size == writer.Position - Position);
    }
}