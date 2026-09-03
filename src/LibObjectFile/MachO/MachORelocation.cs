// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.MachO;

/// <summary>
/// A relocation entry, telling the linker how to fix up one place in a section.
/// </summary>
/// <remarks>
/// Two forms share the same eight bytes. The ordinary form names a symbol or a section by number.
/// The scattered form carries the target address outright, for when the fixed-up value does not
/// land inside the thing it refers to and a section number would be ambiguous; it has no room
/// left for an external flag, so it is always local.
/// </remarks>
public sealed class MachORelocation
{
    /// <summary>Bit of the first word marking an entry as scattered (<c>R_SCATTERED</c>).</summary>
    public const uint ScatteredMask = 0x80000000;

    /// <summary>The size of one entry, which both forms share.</summary>
    public const uint EntrySize = 8;

    /// <summary>
    /// Gets or sets the offset of the fixed-up field from the start of its section.
    /// </summary>
    public int Address { get; set; }

    /// <summary>
    /// Gets or sets the symbol index when <see cref="IsExternal"/> is true, and the one-based
    /// section number otherwise. Always zero for a scattered entry.
    /// </summary>
    public uint SymbolOrSectionNumber { get; set; }

    /// <summary>
    /// Gets or sets the address of the target, for a scattered entry only.
    /// </summary>
    public int Value { get; set; }

    /// <summary>
    /// Gets or sets whether the fixed-up field holds a displacement from the program counter
    /// rather than an address.
    /// </summary>
    public bool IsPcRelative { get; set; }

    /// <summary>
    /// Gets or sets the width of the fixed-up field as a power of two, so 2 means four bytes.
    /// </summary>
    public byte LengthLog2 { get; set; }

    /// <summary>
    /// Gets or sets whether <see cref="SymbolOrSectionNumber"/> is a symbol index. Never true
    /// for a scattered entry, which has no room for the flag.
    /// </summary>
    public bool IsExternal { get; set; }

    /// <summary>
    /// Gets or sets whether this entry carries its target address rather than a symbol or
    /// section number.
    /// </summary>
    public bool IsScattered { get; set; }

    /// <summary>
    /// Gets or sets the type, whose meaning depends on the image's architecture. Interpret it as
    /// <see cref="MachOX86_64RelocationType"/>, <see cref="MachOArm64RelocationType"/> or
    /// <see cref="MachOGenericRelocationType"/> accordingly.
    /// </summary>
    public byte RawType { get; set; }

    /// <summary>
    /// Gets the width of the fixed-up field in bytes.
    /// </summary>
    public int LengthInBytes => 1 << LengthLog2;

    /// <summary>
    /// Decodes an entry from the two words it occupies.
    /// </summary>
    /// <param name="word0">The first word, which is the address unless the top bit marks it scattered.</param>
    /// <param name="word1">The second word.</param>
    /// <returns>The decoded entry.</returns>
    public static MachORelocation Decode(uint word0, uint word1)
    {
        if ((word0 & ScatteredMask) != 0)
        {
            return new MachORelocation
            {
                IsScattered = true,
                Address = (int)(word0 & 0x00ffffff),
                RawType = (byte)((word0 >> 24) & 0xf),
                LengthLog2 = (byte)((word0 >> 28) & 0x3),
                IsPcRelative = ((word0 >> 30) & 1) != 0,
                Value = (int)word1,
            };
        }

        return new MachORelocation
        {
            Address = (int)word0,
            SymbolOrSectionNumber = word1 & 0x00ffffff,
            IsPcRelative = ((word1 >> 24) & 1) != 0,
            LengthLog2 = (byte)((word1 >> 25) & 0x3),
            IsExternal = ((word1 >> 27) & 1) != 0,
            RawType = (byte)((word1 >> 28) & 0xf),
        };
    }

    /// <summary>
    /// Encodes this entry back into the two words it occupies.
    /// </summary>
    /// <returns>The first and second words.</returns>
    public (uint Word0, uint Word1) Encode()
    {
        if (IsScattered)
        {
            var packed = ScatteredMask
                | ((uint)Address & 0x00ffffff)
                | ((uint)(RawType & 0xf) << 24)
                | ((uint)(LengthLog2 & 0x3) << 28)
                | (IsPcRelative ? 1u << 30 : 0u);
            return (packed, (uint)Value);
        }

        var info = (SymbolOrSectionNumber & 0x00ffffff)
            | (IsPcRelative ? 1u << 24 : 0u)
            | ((uint)(LengthLog2 & 0x3) << 25)
            | (IsExternal ? 1u << 27 : 0u)
            | ((uint)(RawType & 0xf) << 28);
        return ((uint)Address, info);
    }

    /// <inheritdoc />
    public override string ToString()
        => $"{nameof(MachORelocation)} {{ Address = 0x{Address:X}, Type = {RawType}, {LengthInBytes} bytes{(IsPcRelative ? ", pcrel" : string.Empty)}{(IsExternal ? ", external" : string.Empty)}{(IsScattered ? ", scattered" : string.Empty)} }}";
}
