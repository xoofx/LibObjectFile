// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace LibObjectFile.Dwarf
{
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
    public sealed class DwarfAbbreviation : ObjectFileNode<DwarfDebugAbbrevTable>
    {
        private readonly List<DwarfAbbreviationItem> _items;
        private readonly Dictionary<ulong, DwarfAbbreviationItem> _mapItems; // Only used if code are non contiguous
        private readonly Dictionary<DwarfAbbreviationItemKey, DwarfAbbreviationItem> _mapKeyToItem;
        private ulong _nextCode;

        public DwarfAbbreviation()
        {
            _items = new List<DwarfAbbreviationItem>();
            _mapItems = new Dictionary<ulong, DwarfAbbreviationItem>();
            _mapKeyToItem = new Dictionary<DwarfAbbreviationItemKey, DwarfAbbreviationItem>();
            _nextCode = 1;
        }

        public void Reset()
        {
            _items.Clear();
            _mapItems.Clear();
            _mapKeyToItem.Clear();
            _nextCode = 1;
        }

        public IEnumerable<DwarfAbbreviationItem> Items => _mapItems.Count > 0 ? GetMapItems() : _items;

        private IEnumerable<DwarfAbbreviationItem> GetMapItems()
        {
            foreach (var item in _mapItems.Values)
            {
                yield return item;
            }
        }
        
        public DwarfAbbreviationItem GetOrCreate(DwarfAbbreviationItemKey itemKey)
        {
            if (!_mapKeyToItem.TryGetValue(itemKey, out var item))
            {
                item = new DwarfAbbreviationItem(_nextCode, this, itemKey.Tag, itemKey.HasChildren, itemKey.Descriptors);

                if (_mapItems.Count > 0)
                {

                    _mapItems[_nextCode] = item;
                    _mapKeyToItem[itemKey] = item;
                }
                else
                {
                    _items.Add(item);
                }

                _nextCode++;
            }

            return item;
        }

        public bool TryFindByCode(ulong code, out DwarfAbbreviationItem item)
        {
            item = null;
            if (code == 0)
            {
                return false;
            }

            code--;

            if (_mapItems.Count > 0)
            {
                return _mapItems.TryGetValue(code, out item);
            }

            if (code < int.MaxValue && (int)code < _items.Count)
            {
                item = _items[(int) code];
                return true;
            }

            item = null;
            return false;
        }

        private string DebuggerDisplay => $"Count = {(_mapItems.Count > 0 ? _mapItems.Count : _items.Count)}";

        public static DwarfAbbreviation Read(Stream reader, ulong abbreviationOffset)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));
            if (TryRead(reader, abbreviationOffset, out var abbrev, out var diagnostics))
            {
                return abbrev;
            }
            throw new ObjectFileException($"Unexpected error while trying to read abbreviation at offset {abbreviationOffset}", diagnostics);
        }

        public static bool TryRead(Stream reader, ulong abbreviationOffset, out DwarfAbbreviation abbrev, out DiagnosticBag diagnostics)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));
            abbrev = new DwarfAbbreviation();
            diagnostics = new DiagnosticBag();
            return abbrev.TryReadInternal(reader, abbreviationOffset, diagnostics);
        }

        internal bool TryReadInternal(Stream reader, ulong abbreviationOffset, DiagnosticBag diagnostics)
        {
            Offset = abbreviationOffset;
            reader.Position = (long)abbreviationOffset;
            while (TryReadNext(reader, diagnostics))
            {
            }

            Size = (ulong) reader.Position - abbreviationOffset;

            return !diagnostics.HasErrors;
        }

        private bool TryReadNext(Stream reader, DiagnosticBag diagnostics)
        {
            var code = reader.ReadULEB128();
            if (code == 0)
            {
                return false;
            }

            var index = code - 1;
            bool canAddToList = _mapItems.Count == 0 && index < int.MaxValue &&_items.Count == (int)index;
            
            var itemTag = new DwarfTagEx(reader.ReadULEB128AsU32());
            var hasChildrenRaw = reader.ReadU8();
            bool hasChildren = false;
            if (hasChildrenRaw == DwarfNative.DW_CHILDREN_yes)
            {
                hasChildren = true;
            }
            else if (hasChildrenRaw != DwarfNative.DW_CHILDREN_no)
            {
                diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidData, $"Invalid children {hasChildrenRaw}. Must be either {DwarfNative.DW_CHILDREN_yes} or {DwarfNative.DW_CHILDREN_no}");
                return false;
            }

            var itemCode = code;
            var itemHasChildren = hasChildren;

            List<DwarfAttributeDescriptor> descriptors = null;

            while (true)
            {
                var attributeName = new DwarfAttributeKindEx(reader.ReadULEB128AsU32());
                var attributeForm = new DwarfAttributeFormEx(reader.ReadULEB128AsU32());

                if (attributeForm.Value == 0 && attributeForm.Value == 0)
                {
                    break;
                }

                if (descriptors == null) descriptors = new List<DwarfAttributeDescriptor>(1);
                descriptors.Add(new DwarfAttributeDescriptor(attributeName, attributeForm));
            }

            var itemDescriptors = descriptors != null ? new DwarfAttributeDescriptors(descriptors.ToArray()) : new DwarfAttributeDescriptors();

            var item = new DwarfAbbreviationItem(itemCode, this, itemTag, itemHasChildren, itemDescriptors);

            if (canAddToList)
            {
                _items.Add(item);
                _nextCode++;
            }
            else
            {
                if (_mapItems.Count == 0)
                {
                    for (var i = 0; i < _items.Count; i++)
                    {
                        var previousItem = _items[i];
                        _mapItems.Add((ulong)i + 1, previousItem);
                    }
                    _items.Clear();
                }

                // TODO: check collisions
                if (_mapItems.ContainsKey(code))
                {
                    diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidData, $"Invalid code {code} found while another code already exists in this abbreviation.");
                    return false;
                }
                _mapItems.Add(code, item);

                _nextCode = Math.Max(code, _nextCode) + 1;
            }

            var key = new DwarfAbbreviationItemKey(item.Tag, item.HasChildren, item.Descriptors);
            _mapKeyToItem.Add(key, item);
            
            return true;
        }

        public override bool TryUpdateLayout(DiagnosticBag diagnostics)
        {
            return true;
        }
    }
}