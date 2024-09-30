// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Text;

namespace LibObjectFile.Dwarf;

public class DwarfLocationListEntry : DwarfObject<DwarfLocationList>
{
    public ulong Start;

    public ulong End;

    public DwarfExpression? Expression;

    public DwarfLocationListEntry()
    {
    }

    public override void Read(DwarfReader reader)
    {
        Start = reader.ReadUInt();
        End = reader.ReadUInt();

        if (Start == 0 && End == 0)
        {
            // End of list
            return;
        }

        bool isBaseAddress =
            (reader.AddressSize == DwarfAddressSize.Bit64 && Start == ulong.MaxValue) ||
            (reader.AddressSize == DwarfAddressSize.Bit32 && Start == uint.MaxValue);
        if (isBaseAddress)
        {
            // Sets new base address for following entries
            return;
        }

        Expression = new DwarfExpression();
        Expression.ReadInternal(reader, inLocationSection: true);
    }

    protected override void UpdateLayoutCore(DwarfLayoutContext layoutContext)
    {
        var endOffset = Position;

        endOffset += 2 * DwarfHelper.SizeOfUInt(layoutContext.CurrentUnit!.AddressSize);
        if (Expression != null)
        {
            Expression.Position = endOffset;
            Expression.UpdateLayout(layoutContext, inLocationSection: true);
            endOffset += Expression.Size;
        }

        Size = endOffset - Position;
    }

    public override void Write(DwarfWriter writer)
    {
        bool isBaseAddress =
            (writer.AddressSize == DwarfAddressSize.Bit64 && Start == ulong.MaxValue) ||
            (writer.AddressSize == DwarfAddressSize.Bit32 && Start == uint.MaxValue);
        if (isBaseAddress)
        {
            writer.WriteUInt(Start);
            writer.WriteAddress(DwarfRelocationTarget.Code, End);
        }
        else
        {
            writer.WriteAddress(DwarfRelocationTarget.Code, Start);
            writer.WriteAddress(DwarfRelocationTarget.Code, End);
        }

        if (Expression != null)
        {
            Expression.WriteInternal(writer, inLocationSection: true);
        }
    }

    protected override bool PrintMembers(StringBuilder builder)
    {
        builder.Append($"Location: {Start:x} - {End:x} {Expression}");
        return true;
    }
}