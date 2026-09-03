// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.MachO;

/// <summary>
/// The magic values a Mach-O or universal binary can start with.
/// </summary>
/// <remarks>
/// The <c>Cigam</c> spellings are the byte-swapped forms, written by a host of the opposite
/// endianness. The fat magics work the other way round from the thin ones: a universal binary
/// header is always big-endian, so <see cref="FatMagic"/> is what a big-endian reader sees and
/// <see cref="FatCigam"/> is what a little-endian host reads out of a well-formed file.
/// </remarks>
public static class MachOMagic
{
    /// <summary>32-bit Mach-O, host-endian (<c>MH_MAGIC</c>).</summary>
    public const uint Magic32 = 0xfeedface;

    /// <summary>32-bit Mach-O, byte-swapped (<c>MH_CIGAM</c>).</summary>
    public const uint Cigam32 = 0xcefaedfe;

    /// <summary>64-bit Mach-O, host-endian (<c>MH_MAGIC_64</c>).</summary>
    public const uint Magic64 = 0xfeedfacf;

    /// <summary>64-bit Mach-O, byte-swapped (<c>MH_CIGAM_64</c>).</summary>
    public const uint Cigam64 = 0xcffaedfe;

    /// <summary>Universal binary with 32-bit slice offsets (<c>FAT_MAGIC</c>).</summary>
    public const uint FatMagic = 0xcafebabe;

    /// <summary>Universal binary with 32-bit slice offsets, byte-swapped (<c>FAT_CIGAM</c>).</summary>
    public const uint FatCigam = 0xbebafeca;

    /// <summary>Universal binary with 64-bit slice offsets (<c>FAT_MAGIC_64</c>).</summary>
    public const uint FatMagic64 = 0xcafebabf;

    /// <summary>Universal binary with 64-bit slice offsets, byte-swapped (<c>FAT_CIGAM_64</c>).</summary>
    public const uint FatCigam64 = 0xbfbafeca;
}
