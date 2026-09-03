// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using LibObjectFile.Diagnostics;

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
    /// The largest alignment exponent a slice can carry.
    /// </summary>
    /// <remarks>
    /// A shift count is masked to the width of the value it shifts, so an exponent past this
    /// would quietly alias to a different alignment instead of being rejected. Real images use
    /// 12 to 14, the page size of the architecture.
    /// </remarks>
    public const uint MaxAlignLog2 = 63;

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
    /// <remarks>
    /// Only meaningful while <see cref="AlignLog2"/> is within <see cref="MaxAlignLog2"/>, which
    /// the reader enforces and <see cref="MachOFatFile.Verify(DiagnosticBag)"/> checks.
    /// </remarks>
    public ulong Alignment => 1ul << (int)AlignLog2;

    /// <inheritdoc />
    public override string ToString()
        => $"{nameof(MachOFatSlice)} {{ {CpuType}, FileOffset = 0x{FileOffset:X}, Size = 0x{Size:X}, Align = 2^{AlignLog2} }}";
}
