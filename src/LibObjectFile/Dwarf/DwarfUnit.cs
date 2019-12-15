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

        public ulong DebugAbbreviationOffset { get; internal set; }

        public ulong UnitLength { get; internal set; }

        protected override void ValidateParent(ObjectFileNode parent)
        {
            if (!(parent is DwarfSection))
            {
                throw new ArgumentException($"Parent must inherit from type {nameof(DwarfSection)}");
            }
        }
        
        /// <summary>
        /// Gets or sets the root <see cref="DwarfDIE"/> of this compilation unit.
        /// </summary>
        public DwarfDIE Root
        {
            get => _root;
            set => AttachChild<DwarfContainer, DwarfDIE>(this, value, ref _root, true);
        }

        /// <summary>
        /// Gets or sets the abbreviation associated with the <see cref="Root"/> <see cref="DwarfDIE"/>
        /// </summary>
        public DwarfAbbreviation Abbreviation { get; internal set; }
        
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
        
        protected override void UpdateLayout(DwarfLayoutContext layoutContext)
        {
            var offset = this.Offset;

            // 1. unit_length 
            offset += DwarfHelper.SizeOfUnitLength(Is64BitEncoding);
            // 2. version (uhalf) 
            offset += sizeof(ushort); // WriteU16(unit.Version);

            if (Version >= 5)
            {
                // 3. unit_type (ubyte)
                offset += 1; // WriteU8(unit.Kind.Value);
            }

            // Update the layout specific to the Unit instance
            offset += GetLayoutHeaderSize();

            Abbreviation = null;

            // Compute the full layout of all DIE and attributes (once abbreviation are calculated)
            if (Root != null)
            {
                // Before updating the layout, we need to compute the abbreviation
                Abbreviation = new DwarfAbbreviation();
                layoutContext.File.AbbreviationTable.AddAbbreviation(Abbreviation);
                
                Root.UpdateAbbreviationItem(layoutContext);
                
                DebugAbbreviationOffset = Abbreviation.Offset;

                Root.Offset = offset;
                Root.UpdateLayoutInternal(layoutContext);
                offset += Root.Size;
            }

            Size = offset - Offset;
            UnitLength = Size - DwarfHelper.SizeOfUnitLength(Is64BitEncoding);
        }

        protected abstract ulong GetLayoutHeaderSize();

        protected abstract bool TryReadHeader(DwarfReader reader);

        protected abstract void WriteHeader(DwarfReaderWriter writer);


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

        protected override void Read(DwarfReader reader)
        {
            reader.CurrentUnit = this;

            foreach (var abbreviation in reader.File.AbbreviationTable.Abbreviations)
            {
                if (abbreviation.Offset == DebugAbbreviationOffset)
                {
                    Abbreviation = abbreviation;
                    break;
                }
            }

            var startDIEOffset = Offset;
            Root = DwarfDIE.ReadInstance(reader);

            reader.ResolveAttributeReferenceWithinCompilationUnit();

            Size = reader.Offset - startDIEOffset;
        }

        internal static DwarfUnit ReadInstance(DwarfReader reader, out ulong offsetEndOfUnit)
        {
            var startOffset = reader.Offset;

            DwarfUnit unit = null;

            // 1. unit_length 
            var unit_length = reader.ReadUnitLength();

            offsetEndOfUnit = (ulong)reader.Offset + unit_length;

            // 2. version (uhalf) 
            var version = reader.ReadU16();

            DwarfUnitKindEx unitKind = reader.DefaultUnitKind;

            if (version <= 2 || version > 5)
            {
                reader.Diagnostics.Error(DiagnosticId.DWARF_ERR_VersionNotSupported, $"Version {version} is not supported");
                return null;
            }

            if (version >= 5)
            {
                // 3. unit_type (ubyte)
                unitKind = new DwarfUnitKindEx(reader.ReadU8());
            }

            switch (unitKind.Value)
            {
                case DwarfUnitKind.Compile:
                case DwarfUnitKind.Partial:
                    unit = new DwarfCompilationUnit();
                    break;

                default:
                    reader.Diagnostics.Error(DiagnosticId.DWARF_ERR_UnsupportedUnitType, $"Unit Type {unitKind} is not supported");
                    return null;
            }

            unit.Kind = unitKind;
            unit.Is64BitEncoding = reader.Is64BitEncoding;
            unit.Offset = startOffset;
            unit.Version = version;

            unit.TryReadHeaderInternal(reader);

            unit.ReadInternal(reader);

            return unit;
        }

        protected override void Write(DwarfWriter writer)
        {
            // 1. unit_length 
            Is64BitEncoding = Is64BitEncoding;
            writer.WriteUnitLength(Size);
            // 2. version (uhalf) 
            writer.WriteU16(Version);

            if (Version >= 5)
            {
                // 3. unit_type (ubyte)
                writer.WriteU8((byte)Kind.Value);
            }

            WriteHeaderInternal(writer);

            Root?.WriteInternal(writer);
            // TODO: check size of unit length
        }
    }
}