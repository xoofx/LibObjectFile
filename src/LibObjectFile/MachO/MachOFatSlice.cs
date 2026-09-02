// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.MachO;

/// <summary>
/// One architecture's image inside a <see cref="MachOFatFile"/>.
/// </summary>
public sealed class MachOFatSlice
{
    /// <summary>
    /// Gets or sets the CPU architecture of this slice. It repeats what the slice's own header
    /// says, so that a loader can pick a slice without parsing it.
    /// </summary>
    public MachOCpuType CpuType { get; set; }

    /// <summary>
    /// Gets or sets the CPU subtype of this slice.
    /// </summary>
    public uint CpuSubType { get; set; }

    /// <summary>
    /// Gets or sets the alignment of this slice as a power of two. It is the page size of the
    /// architecture, because the slice is mapped directly out of the containing file.
    /// </summary>
    public uint AlignLog2 { get; set; }

    /// <summary>
    /// Gets or sets the offset of this slice in the containing file.
    /// </summary>
    public ulong FileOffset { get; set; }

    /// <summary>
    /// Gets or sets the size of this slice in the containing file.
    /// </summary>
    public ulong Size { get; set; }

    /// <summary>
    /// Gets or sets the image this slice holds.
    /// </summary>
    public MachOFile? File { get; set; }

    /// <summary>
    /// Gets the alignment of this slice in bytes.
    /// </summary>
    public ulong Alignment => 1ul << (int)AlignLog2;

    /// <inheritdoc />
    public override string ToString()
        => $"{nameof(MachOFatSlice)} {{ {CpuType}, FileOffset = 0x{FileOffset:X}, Size = 0x{Size:X}, Align = 2^{AlignLog2} }}";
}
