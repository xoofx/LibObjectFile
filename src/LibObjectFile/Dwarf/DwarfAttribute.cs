// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Diagnostics;

namespace LibObjectFile.Dwarf
{
    public sealed class DwarfAttribute : ObjectFileNode<DwarfDIE>, IComparable<DwarfAttribute>
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ulong _valueAsU64;

        public DwarfAttributeKindEx Kind { get; set; }

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

        /// <summary>
        /// Gets or sets the encoding used for this attribute. Default is <c>null</c> meaning that the encoding
        /// is detected automatically. Some attributes may require to explicitly set this encoding to disambiguate
        /// between different encoding form (e.g boolean => <see cref="DwarfAttributeEncoding.Flag"/> instead of <see cref="DwarfAttributeEncoding.Constant"/>)
        /// </summary>
        public DwarfAttributeEncoding? Encoding { get; set; }

        public object ValueAsObject { get; set; }
        
        public int CompareTo(DwarfAttribute other)
        {
            return ((uint)Kind).CompareTo((uint)other.Kind);
        }

        public override string ToString()
        {
            if (ValueAsObject != null)
            {
                return ValueAsU64 != 0 ? $"{nameof(Kind)}: {Kind}, Value: {ValueAsObject} Offset: {ValueAsU64}" : $"{nameof(Kind)}: {Kind}, Value: {ValueAsObject}";
            }
            else
            {
                return $"{nameof(Kind)}: {Kind}, Value: {ValueAsU64}";
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