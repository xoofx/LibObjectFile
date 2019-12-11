// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;

namespace LibObjectFile.Dwarf
{
    public sealed class DwarfAbbreviation : ObjectFileNode<DwarfDebugAbbrevTable>
    {
        private readonly List<DwarfAbbreviationItem> _items;
        private Dictionary<ulong, DwarfAbbreviationItem> _mapItems;

        public DwarfAbbreviation()
        {
            _items = new List<DwarfAbbreviationItem>();
        }

        public bool TryFindByCode(ulong code, out DwarfAbbreviationItem item)
        {
            item = null;
            if (code == 0)
            {
                return false;
            }

            code--;

            if (_mapItems != null)
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
            abbrev.Offset = abbreviationOffset;
            diagnostics = new DiagnosticBag();
            return abbrev.TryReadInternal(reader, abbreviationOffset, diagnostics);
        }

        private bool TryReadInternal(Stream reader, ulong abbreviationOffset, DiagnosticBag diagnostics)
        {
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

            var item = new DwarfAbbreviationItem();

            var index = code - 1;
            bool canAddToList = _mapItems == null && index < int.MaxValue &&_items.Count == (int)index;
            
            item.Tag = new DwarfTagEx(reader.ReadULEB128AsU32());
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

            item.Code = code;

            item.HasChildren = hasChildren;

            if (canAddToList)
            {
                _items.Add(item);
            }
            else
            {
                if (_mapItems == null)
                {
                    _mapItems = new Dictionary<ulong, DwarfAbbreviationItem>();
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
            }

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

            if (descriptors != null)
            {
                item.Descriptors = new DwarfAttributeDescriptors(descriptors);
            }
            
            return true;
        }

        public override bool TryUpdateLayout(DiagnosticBag diagnostics)
        {
            return true;
        }
    }
}