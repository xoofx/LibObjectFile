// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using LibObjectFile.Diagnostics;

namespace LibObjectFile.Dwarf;

[DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
public sealed class DwarfAbbreviation : DwarfObject<DwarfAbbreviationTable>
{
    private readonly Dictionary<ulong, DwarfAbbreviationItem> _mapUlongToItems;

    private readonly Dictionary<DwarfAbbreviationItemKey, DwarfAbbreviationItem> _mapKeyToItem;
    private ulong _nextCode;

    public DwarfAbbreviation()
    {
        _mapUlongToItems = new Dictionary<ulong, DwarfAbbreviationItem>();
        _mapKeyToItem = new Dictionary<DwarfAbbreviationItemKey, DwarfAbbreviationItem>();
        _nextCode = 1;
    }

    public void Reset()
    {
        // Reset parent dependency
        foreach (var keyPair in _mapUlongToItems.Values)
        {
            keyPair.Parent = null;
        }
        _mapUlongToItems.Clear();
        _mapKeyToItem.Clear();
        _nextCode = 1;
    }

    public IEnumerable<DwarfAbbreviationItem> Items => _mapUlongToItems.Values;

    public DwarfAbbreviationItem GetOrCreate(DwarfAbbreviationItemKey itemKey)
    {
        if (!TryFindByKey(itemKey, out var item))
        {
            item = new DwarfAbbreviationItem(_nextCode, itemKey.Tag, itemKey.HasChildren, itemKey.Descriptors)
            {
                Parent = this
            };

            // insert or update new item
            _mapUlongToItems[_nextCode] = item;

            // not found insert new item
            _mapKeyToItem[itemKey] = item;

            _nextCode++;
        }

        return item;
    }

    public bool TryFindByCode(ulong code, [NotNullWhen(true)] out DwarfAbbreviationItem? item)
    {
        item = null;
        if (code == 0)
        {
            return false;
        }

        return _mapUlongToItems.TryGetValue(code, out item);
    }

    public bool TryFindByKey(DwarfAbbreviationItemKey key, [NotNullWhen(true)] out DwarfAbbreviationItem? item)
    {
        item = null;
        return _mapKeyToItem.TryGetValue(key, out item);
    }

    private string DebuggerDisplay => $"Count = {_mapUlongToItems.Count}";

    private bool TryReadNext(DwarfReader reader)
    {
        var startOffset = (ulong)reader.Position;
        var code = reader.ReadULEB128();
        if (code == 0)
        {
            return false;
        }

        var item = new DwarfAbbreviationItem
        {
            Position = startOffset,
            Code = code
        };

        item.Read(reader);

        if (_mapUlongToItems.ContainsKey(code))
        {
            reader.Diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidData, $"Invalid code {code} found while another code already exists in this abbreviation.");
            return false;
        }

        _mapUlongToItems.Add(code, item);
        _nextCode = Math.Max(code, _nextCode) + 1;

        var key = new DwarfAbbreviationItemKey(item.Tag, item.HasChildren, item.Descriptors);

        if (!_mapKeyToItem.ContainsKey(key))
        {
            _mapKeyToItem.Add(key, item);
        }

        return true;
    }

    public override void Read(DwarfReader reader)
    {
        Position = reader.Position;
        while (TryReadNext(reader))
        {
        }

        Size = (ulong)reader.Position - Position;
    }

    public override void Write(DwarfWriter writer)
    {
        var startOffset = writer.Position;
        Debug.Assert(startOffset == Position);
        foreach (var item in _mapUlongToItems.Values)
        {
            item.Write(writer);
        }

        // End of abbreviation item
        writer.WriteULEB128(0);

        Debug.Assert(writer.Position - startOffset == Size);
    }

    protected override void UpdateLayoutCore(DwarfLayoutContext context)
    {
        var endOffset = Position;

        foreach (var item in _mapUlongToItems.Values)
        {
            item.Position = endOffset;
            item.UpdateLayout(context);
            endOffset += item.Size;
        }

        endOffset += DwarfHelper.SizeOfULEB128(0);

        Size = endOffset - Position;
    }
}