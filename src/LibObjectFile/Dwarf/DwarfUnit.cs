// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Diagnostics;
using LibObjectFile.Diagnostics;

namespace LibObjectFile.Dwarf;

/// <summary>
/// Base class for a Dwarf Unit.
/// </summary>
public abstract class DwarfUnit : DwarfContainer
{
    private ObjectFileElementHolder<DwarfDIE> _root;

    /// <summary>
    /// Gets or sets the encoding of this unit.
    /// </summary>
    public bool Is64BitEncoding { get; set; }

    /// <summary>
    /// Gets or sets the address size used by this unit.
    /// </summary>
    public DwarfAddressSize AddressSize { get; set; }

    /// <summary>
    /// Gets or sets the version of this unit.
    /// </summary>
    public ushort Version { get; set; }

    /// <summary>
    /// Gets or sets the kind of this unit.
    /// </summary>
    public DwarfUnitKindEx Kind { get; set; }

    /// <summary>
    /// Gets the abbreviation offset, only valid once the layout has been calculated through <see cref="DwarfFile.UpdateLayout"/>.
    /// </summary>
    public ulong DebugAbbreviationOffset { get; internal set; }

    /// <summary>
    /// Gets the unit length, only valid once the layout has been calculated through <see cref="DwarfFile.UpdateLayout"/>.
    /// </summary>
    public ulong UnitLength { get; internal set; }

    /// <summary>
    /// Gets or sets the root <see cref="DwarfDIE"/> of this compilation unit.
    /// </summary>
    public DwarfDIE? Root
    {
        get => _root;
        set => _root.Set(this, value);
    }

    /// <summary>
    /// Gets the abbreviation associated with the <see cref="Root"/> <see cref="DwarfDIE"/>.
    /// </summary>
    /// <remarks>
    /// This abbreviation is automatically setup after reading or after updating the layout through <see cref="DwarfFile.UpdateLayout"/>.
    /// </remarks>
    public DwarfAbbreviation? Abbreviation { get; internal set; }

    public override void Verify(DwarfVerifyContext context)
    {
        var diagnostics = context.Diagnostics;
        if (Version < 2 || Version > 5)
        {
            diagnostics.Error(DiagnosticId.DWARF_ERR_VersionNotSupported, $"Version .debug_info {Version} not supported");
        }

        if (AddressSize == DwarfAddressSize.None)
        {
            diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidAddressSize, $"Address size for .debug_info cannot be None/0");
        }

        Root?.Verify(context);
    }

    protected override void ValidateParent(ObjectElement parent)
    {
        if (!(parent is DwarfSection))
        {
            throw new ArgumentException($"Parent must inherit from type {nameof(DwarfSection)}");
        }
    }

    protected override void UpdateLayoutCore(DwarfLayoutContext context)
    {
        var offset = this.Position;

        // 1. unit_length 
        offset += DwarfHelper.SizeOfUnitLength(Is64BitEncoding);

        var offsetAfterUnitLength = offset;

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
            context.File.AbbreviationTable.Abbreviations.Add(Abbreviation);
                
            Root.UpdateAbbreviationItem(context);
                
            DebugAbbreviationOffset = Abbreviation.Position;

            Root.Position = offset;
            Root.UpdateLayout(context);
            offset += Root.Size;
        }

        Size = offset - Position;
        UnitLength = offset - offsetAfterUnitLength;
    }

    protected abstract ulong GetLayoutHeaderSize();

    protected abstract void ReadHeader(DwarfReader reader);

    protected abstract void WriteHeader(DwarfWriter writer);

    public override void Read(DwarfReader reader)
    {
        reader.CurrentUnit = this;

        foreach (var abbreviation in reader.File.AbbreviationTable.Abbreviations)
        {
            if (abbreviation.Position == DebugAbbreviationOffset)
            {
                Abbreviation = abbreviation;
                break;
            }
        }

        Root = DwarfDIE.ReadInstance(reader);

        reader.ResolveAttributeReferenceWithinCompilationUnit();

        Size = reader.Position - Position;
    }

    internal static DwarfUnit? ReadInstance(DwarfReader reader, out ulong offsetEndOfUnit)
    {
        var startOffset = reader.Position;

        DwarfUnit? unit = null;

        // 1. unit_length 
        var unit_length = reader.ReadUnitLength();

        offsetEndOfUnit = (ulong)reader.Position + unit_length;

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

        unit.UnitLength = unit_length;
        unit.Kind = unitKind;
        unit.Is64BitEncoding = reader.Is64BitEncoding;
        unit.Position = startOffset;
        unit.Version = version;

        unit.ReadHeader(reader);

        unit.Read(reader);

        return unit;
    }

    public override void Write(DwarfWriter writer)
    {
        var startOffset = writer.Position;
        Debug.Assert(Position == writer.Position);

        // 1. unit_length 
        Is64BitEncoding = Is64BitEncoding;
        writer.WriteUnitLength(UnitLength);

        var offsetAfterUnitLength = writer.Position;

        // 2. version (uhalf) 
        writer.WriteU16(Version);

        if (Version >= 5)
        {
            // 3. unit_type (ubyte)
            writer.WriteU8((byte)Kind.Value);
        }

        WriteHeader(writer);
        writer.AddressSize = AddressSize;

        Root?.Write(writer);
        // TODO: check size of unit length

        Debug.Assert(Size == writer.Position - startOffset);
        Debug.Assert(UnitLength == writer.Position - offsetAfterUnitLength);
    }
}