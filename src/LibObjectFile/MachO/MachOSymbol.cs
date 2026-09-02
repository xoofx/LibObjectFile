// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.MachO;

/// <summary>
/// An entry of the symbol table.
/// </summary>
/// <remarks>
/// The type byte packs four things at once: whether the entry is debug information, whether the
/// symbol is external or private external, and what it refers to. They are separated here, since
/// reading the byte as a single value gets the common cases wrong.
/// </remarks>
public sealed class MachOSymbol
{
    /// <summary>Bits of the type byte holding debug information rather than a symbol (<c>N_STAB</c>).</summary>
    public const byte StabMask = 0xe0;

    /// <summary>Bit of the type byte marking a symbol as private external (<c>N_PEXT</c>).</summary>
    public const byte PrivateExternalMask = 0x10;

    /// <summary>Bits of the type byte holding the <see cref="MachOSymbolKind"/> (<c>N_TYPE</c>).</summary>
    public const byte KindMask = 0x0e;

    /// <summary>Bit of the type byte marking a symbol as external (<c>N_EXT</c>).</summary>
    public const byte ExternalMask = 0x01;

    /// <summary>
    /// Gets or sets the name of the symbol.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the offset of the name in the string table.
    /// </summary>
    public uint NameOffset { get; set; }

    /// <summary>
    /// Gets or sets the raw type byte.
    /// </summary>
    public byte RawType { get; set; }

    /// <summary>
    /// Gets or sets the one-based index of the section defining this symbol, or zero when no
    /// section does (<c>NO_SECT</c>).
    /// </summary>
    public byte SectionIndex { get; set; }

    /// <summary>
    /// Gets or sets the description field, whose meaning depends on the symbol. For an undefined
    /// symbol in a two-level image its high byte is the ordinal of the library defining it.
    /// </summary>
    public ushort Description { get; set; }

    /// <summary>
    /// Gets or sets the value of the symbol, which is its address when
    /// <see cref="Kind"/> is <see cref="MachOSymbolKind.Section"/>.
    /// </summary>
    public ulong Value { get; set; }

    /// <summary>
    /// Gets a value indicating whether this entry is debug information rather than a symbol.
    /// </summary>
    /// <remarks>
    /// A debug entry reuses the other fields for its own purposes, so the rest of this type does
    /// not describe it.
    /// </remarks>
    public bool IsDebug => (RawType & StabMask) != 0;

    /// <summary>
    /// Gets what this symbol refers to. Meaningless when <see cref="IsDebug"/> is true.
    /// </summary>
    public MachOSymbolKind Kind => (MachOSymbolKind)(RawType & KindMask);

    /// <summary>
    /// Gets a value indicating whether the symbol is visible outside this image.
    /// </summary>
    public bool IsExternal => (RawType & ExternalMask) != 0;

    /// <summary>
    /// Gets a value indicating whether the symbol is visible only to what was linked with it.
    /// </summary>
    public bool IsPrivateExternal => (RawType & PrivateExternalMask) != 0;

    /// <summary>
    /// Gets the ordinal of the library expected to define this symbol, for an undefined symbol
    /// in a two-level namespace image. The ordinal is a one-based index into the dylib commands
    /// in the order they appear, which is why inserting one anywhere but last renumbers them.
    /// </summary>
    public int LibraryOrdinal => (Description >> 8) & 0xff;

    /// <inheritdoc />
    public override string ToString()
        => $"{nameof(MachOSymbol)} {{ {Name}, {Kind}{(IsExternal ? ", external" : string.Empty)}, Value = 0x{Value:X} }}";
}
