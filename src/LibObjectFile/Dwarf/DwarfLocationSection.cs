// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using LibObjectFile.Collections;

namespace LibObjectFile.Dwarf;

[DebuggerDisplay("Count = {LineTables.Count,nq}")]
public sealed class DwarfLocationSection : DwarfRelocatableSection
{
    private readonly ObjectList<DwarfLocationList> _locationLists;

    public DwarfLocationSection()
    {
        _locationLists = new ObjectList<DwarfLocationList>(this);

    }

    public ObjectList<DwarfLocationList> LocationLists => _locationLists;

    public override void Read(DwarfReader reader)
    {
        while (reader.Position < reader.Length)
        {
            var locationList = new DwarfLocationList();
            locationList.Position = reader.Position;
            locationList.Read(reader);
            _locationLists.Add(locationList);
        }
    }

    public override void Verify(DwarfVerifyContext context)
    {
        foreach (var locationList in _locationLists)
        {
            locationList.Verify(context);
        }
    }

    protected override void UpdateLayoutCore(DwarfLayoutContext context)
    {
        ulong sizeOf = 0;

        foreach (var locationList in _locationLists)
        {
            locationList.Position = Position + sizeOf;
            locationList.UpdateLayout(context);
            sizeOf += locationList.Size;
        }
        Size = sizeOf;
    }

    public override void Write(DwarfWriter writer)
    {
        var startOffset = writer.Position;

        foreach (var locationList in _locationLists)
        {
            locationList.Write(writer);
        }

        Debug.Assert(Size == writer.Position - startOffset, $"Expected Size: {Size} != Written Size: {writer.Position - startOffset}");
    }

    protected override bool PrintMembers(StringBuilder builder)
    {
        builder.Append($"Section .debug_loc, Entries: {_locationLists.Count}, ");
        base.PrintMembers(builder);
        return true;
    }
}