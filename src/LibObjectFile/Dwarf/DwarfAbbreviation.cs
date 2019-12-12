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
            // Reset parent dependency
            foreach (var dwarfAbbreviationItem in _items)
            {
                dwarfAbbreviationItem.Parent = null;
            }

            if (_mapItems.Count > 0)
            {
                foreach (var keyPair in _mapItems)
                {
                    keyPair.Value.Parent = null;
                }
            }

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
            var startOffset = (ulong)reader.Position;
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

            item.Offset = startOffset;
            item.Size = (ulong) reader.Position - startOffset;
            
            return true;
        }

        public override bool TryUpdateLayout(DiagnosticBag diagnostics)
        {
            if (diagnostics == null) throw new ArgumentNullException(nameof(diagnostics));

            var offset = Offset;

            if (_mapItems.Count > 0)
            {
                foreach(var itemPair in _mapItems)
                {
                    var item = itemPair.Value;
                    item.Offset = offset;
                    TryUpdateLayout(item);
                    offset += item.Size;
                }

            }
            else
            {
                if (_items.Count > 0)
                {
                    foreach(var item in _items)
                    {
                        item.Offset = offset;
                        TryUpdateLayout(item);
                        offset += item.Size;
                    }
                }
            }

            offset += DwarfHelper.SizeOfULEB128(0);
            
            Size = offset;

            return true;
        }

        private void TryUpdateLayout(DwarfAbbreviationItem item)
        {
            ulong offset = 0;

            // Code
            offset += DwarfHelper.SizeOfULEB128(item.Code);

            // Tag
            offset += DwarfHelper.SizeOfULEB128((uint) item.Tag.Value);

            // HasChildren
            offset += 1;

            var descriptors = item.Descriptors;
            for (int i = 0; i < descriptors.Length; i++)
            {
                var descriptor = descriptors[i];
                offset += DwarfHelper.SizeOfULEB128((uint) descriptor.Kind.Value);
                offset += DwarfHelper.SizeOfULEB128((uint) descriptor.Form.Value);
            }

            // Null Kind and Form
            offset += DwarfHelper.SizeOfULEB128(0) * 2;

            item.Size = offset;
        }
        

        internal void Write(DwarfWriter writer)
        {
            var startOffset = writer.Offset;
            Debug.Assert(startOffset == Offset);
            if (_mapItems.Count > 0)
            {
                foreach (var itemPair in _mapItems)
                {
                    var item = itemPair.Value;
                    Write(writer, item);
                }

            }
            else
            {
                if (_items.Count > 0)
                {
                    foreach (var item in _items)
                    {
                        Write(writer, item);
                    }
                }
            }

            // End of abbreviation item
            writer.WriteULEB128(0);

            Debug.Assert(writer.Offset - startOffset == Size);
        }

        private void Write(DwarfWriter writer, DwarfAbbreviationItem item)
        {
            var startOffset = writer.Offset;
            Debug.Assert(startOffset == item.Offset);

            // Code
            writer.WriteULEB128(item.Code);
            
            // Tag
            writer.WriteULEB128((uint)item.Tag.Value);

            // HasChildren
            writer.WriteU8(item.HasChildren ? DwarfNative.DW_CHILDREN_yes : DwarfNative.DW_CHILDREN_no);

            var descriptors = item.Descriptors;
            for (int i = 0; i < descriptors.Length; i++)
            {
                var descriptor = descriptors[i];
                writer.WriteULEB128((uint)descriptor.Kind.Value);
                writer.WriteULEB128((uint)descriptor.Form.Value);
            }
            writer.WriteULEB128(0);
            writer.WriteULEB128(0);
            
            Debug.Assert(writer.Offset - startOffset == item.Size);
        }
    }
}