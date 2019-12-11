// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.Dwarf
{
    public abstract class DwarfUnit : DwarfContainer, IRelocatable
    {
        private DwarfDIE _root;

        public bool Is64BitEncoding { get; set; }

        public bool Is64BitAddress { get; set; }

        public ushort Version { get; set; }

        public DwarfUnitKindEx Kind { get; set; }

        internal ulong DebugAbbreviationOffset { get; set; }
        
        /// <summary>
        /// Gets or sets the root <see cref="DwarfDIE"/> of this compilation unit.
        /// </summary>
        public DwarfDIE Root
        {
            get => _root;
            set => AttachChild<DwarfContainer, DwarfDIE>(this, value, ref _root);
        }

        /// <summary>
        /// Gets or sets the abbreviation associated with the <see cref="Root"/> <see cref="DwarfDIE"/>
        /// </summary>
        public DwarfAbbreviation Abbreviation { get; set; }

        public ulong GetRelocatableValue(ulong relativeOffset, RelocationSize size)
        {
            throw new NotImplementedException();
        }

        public void SetRelocatableValue(ulong relativeOffset, RelocationSize size)
        {
            throw new NotImplementedException();
        }

        internal bool TryReadHeaderInternal(DwarfReader reader)
        {
            return TryReadHeader(reader);
        }

        internal void WriteHeaderInternal(DwarfReaderWriter writer)
        {
            WriteHeader(writer);
        }

        internal void UpdateLayoutInternal(DwarfWriter writer, ref ulong sizeOf)
        {
            UpdateLayout(writer, ref sizeOf);
        }

        protected abstract bool TryReadHeader(DwarfReader reader);

        protected abstract void WriteHeader(DwarfReaderWriter writer);

        protected abstract void UpdateLayout(DwarfWriter writer, ref ulong sizeOf);

        protected bool TryReadAddressSize(DwarfReaderWriter reader)
        {
            var address_size = reader.ReadU8();

            // TODO: We don't support anything else than 32 or 64bit for now
            if (address_size != 4 && address_size != 8)
            {
                reader.Diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidAddressSize, $"Unsupported address size {address_size} for unit at offset {Offset}. Must be 4 (32 bits) or 8 (64 bits).");
                return false;
            }

            Is64BitAddress = address_size == 8;
            return true;
        }

        protected void WriteAddressSize(DwarfReaderWriter writer)
        {
            if (Is64BitAddress)
            {
                writer.WriteU8(8);
            }
            else
            {
                writer.WriteU8(4);
            }
        }
    }
}