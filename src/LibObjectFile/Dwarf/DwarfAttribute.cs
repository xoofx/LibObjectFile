// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.Dwarf
{
    public sealed class DwarfAttribute : ObjectFileNode<DwarfDIE>, IComparable<DwarfAttribute>
    {
        private ulong _valueAsU64;
        public DwarfAttributeKey Key { get; set; }

        public bool ValueAsBoolean
        {
            get => _valueAsU64 != 0;
            set => _valueAsU64 = value ? 1U : 0;
        }

        public int ValueAsI32
        {
            get => (int)_valueAsU64;
            set => _valueAsU64 = (ulong)(long)value;
        }

        public uint ValueAsU32
        {
            get => (uint)_valueAsU64;
            set => _valueAsU64 = value;
        }

        public long ValueAsI64
        {
            get => (long)_valueAsU64;
            set => _valueAsU64 = (ulong)value;
        }

        public ulong ValueAsU64
        {
            get => _valueAsU64;
            set => _valueAsU64 = value;
        }

        public object ValueAsObject { get; set; }
        
        public int CompareTo(DwarfAttribute other)
        {
            return this.Key.Value.CompareTo(other.Key.Value);
        }

        public override string ToString()
        {
            if (ValueAsObject != null)
            {
                return ValueAsU64 != 0 ? $"{nameof(Key)}: {Key}, Value: {ValueAsObject} Offset: {ValueAsU64}" : $"{nameof(Key)}: {Key}, Value: {ValueAsObject}";
            }
            else
            {
                return $"{nameof(Key)}: {Key}, Value: {ValueAsU64}";
            }
        }

        public override void Verify(DiagnosticBag diagnostics)
        {
            base.Verify(diagnostics);
        }

        public override bool TryUpdateLayout(DiagnosticBag diagnostics)
        {
            return true;
        }
    }
}