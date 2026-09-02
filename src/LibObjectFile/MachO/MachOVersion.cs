// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.MachO;

/// <summary>
/// Helpers for the packed version numbers Mach-O stores.
/// </summary>
public static class MachOVersion
{
    /// <summary>
    /// Unpacks a 32-bit version, which stores its three parts in 16, 8 and 8 bits. This is the
    /// encoding used by dylib versions and by the minimum OS version commands.
    /// </summary>
    /// <param name="packed">The packed value.</param>
    /// <returns>The version it encodes.</returns>
    public static Version Decode(uint packed)
        => new((int)(packed >> 16), (int)((packed >> 8) & 0xff), (int)(packed & 0xff));

    /// <summary>
    /// Packs a version into the 16.8.8 encoding.
    /// </summary>
    /// <param name="version">The version to pack. A negative build or revision is treated as zero.</param>
    /// <returns>The packed value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="version"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A component does not fit in the encoding.</exception>
    public static uint Encode(Version version)
    {
        ArgumentNullException.ThrowIfNull(version);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(version.Major, 0xffff);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(version.Minor, 0xff);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(Math.Max(version.Build, 0), 0xff);

        return ((uint)version.Major << 16) | ((uint)version.Minor << 8) | (uint)Math.Max(version.Build, 0);
    }

    /// <summary>
    /// Unpacks a 64-bit source version, which stores five parts in 24, 10, 10, 10 and 10 bits.
    /// The last part is dropped, since <see cref="Version"/> holds only four.
    /// </summary>
    /// <param name="packed">The packed value.</param>
    /// <returns>The first four components of the version.</returns>
    public static Version DecodeSource(ulong packed)
        => new((int)(packed >> 40), (int)((packed >> 30) & 0x3ff), (int)((packed >> 20) & 0x3ff), (int)((packed >> 10) & 0x3ff));
}
