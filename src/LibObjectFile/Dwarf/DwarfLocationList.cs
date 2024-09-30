// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Collections.Generic;
using System.Text;
using LibObjectFile.Collections;

namespace LibObjectFile.Dwarf;

public class DwarfLocationList : DwarfContainer
{
    private readonly ObjectList<DwarfLocationListEntry> _locationListEntries;

    public DwarfLocationList()
    {
        _locationListEntries = new ObjectList<DwarfLocationListEntry>(this);
    }

    public ObjectList<DwarfLocationListEntry> LocationListEntries => _locationListEntries;

    protected override void UpdateLayoutCore(DwarfLayoutContext layoutContext)
    {
        var endOffset = Position;

        foreach (var locationListEntry in _locationListEntries)
        {
            locationListEntry.Position = endOffset;
            locationListEntry.UpdateLayout(layoutContext);
            endOffset += locationListEntry.Size;
        }

        // End of list
        endOffset += 2 * DwarfHelper.SizeOfUInt(layoutContext.CurrentUnit!.AddressSize);

        Size = endOffset - Position;
    }

    public override void Read(DwarfReader reader)
    {
        reader.OffsetToLocationList.Add(reader.Position, this);

        while (reader.Position < reader.Length)
        {
            var locationListEntry = new DwarfLocationListEntry();
            locationListEntry.Read(reader);

            if (locationListEntry.Start == 0 && locationListEntry.End == 0)
            {
                // End of list
                return;
            }

            _locationListEntries.Add(locationListEntry);
        }
    }

    public override void Write(DwarfWriter writer)
    {
        foreach (var locationListEntry in _locationListEntries)
        {
            locationListEntry.Write(writer);
        }

        // End of list
        writer.WriteUInt(0);
        writer.WriteUInt(0);
    }

    protected override bool PrintMembers(StringBuilder builder)
    {
        for (int i = 0; i < _locationListEntries.Count; i++)
        {
            if (i == 3)
            {
                builder.Append(", ...");
                break;
            }
            else if (i != 0)
            {
                builder.Append(", ");
            }

            builder.Append(_locationListEntries[i].ToString());
        }

        return true;
    }
}