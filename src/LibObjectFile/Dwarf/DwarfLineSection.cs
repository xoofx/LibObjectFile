// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using LibObjectFile.Collections;

namespace LibObjectFile.Dwarf;

[DebuggerDisplay("Count = {LineTables.Count,nq}")]
public sealed class DwarfLineSection : DwarfRelocatableSection
{
    private readonly ObjectList<DwarfLineProgramTable> _tables;


    public DwarfLineSection()
    {
        _tables = new ObjectList<DwarfLineProgramTable>(this);
    }

    public ObjectList<DwarfLineProgramTable> LineTables => _tables;

    public override void Read(DwarfReader reader)
    {
        while (reader.Position < reader.Length)
        {
            var programTable = new DwarfLineProgramTable();
            programTable.Position = reader.Position;
            programTable.Read(reader);
            _tables.Add(programTable);
        }
    }

    public override void Verify(DwarfVerifyContext context)
    {
        foreach (var dwarfLineProgramTable in _tables)
        {
            dwarfLineProgramTable.Verify(context);
        }
    }

    public override void UpdateLayout(DwarfLayoutContext layoutContext)
    {
        ulong sizeOf = 0;

        foreach (var dwarfLineProgramTable in _tables)
        {
            dwarfLineProgramTable.Position = Position + sizeOf;
            dwarfLineProgramTable.UpdateLayout(layoutContext);
            sizeOf += dwarfLineProgramTable.Size;
        }
        Size = sizeOf;
    }

    public override void Write(DwarfWriter writer)
    {
        var startOffset = writer.Position;

        foreach (var dwarfLineProgramTable in _tables)
        {
            dwarfLineProgramTable.Write(writer);
        }

        Debug.Assert(Size == writer.Position - startOffset, $"Expected Size: {Size} != Written Size: {writer.Position - startOffset}");
    }

    protected override bool PrintMembers(StringBuilder builder)
    {
        builder.Append($"Section .debug_line, Entries: {_tables.Count}");
        return true;
    }
}