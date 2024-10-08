﻿// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Buffers;
using LibObjectFile.Diagnostics;
using LibObjectFile.Utils;

namespace LibObjectFile.Elf;

/// <summary>
/// A shadow section allowing to align the following section from <see cref="ElfObjectFile"/>
/// to respect the <see cref="UpperAlignment"/> of this section.
/// This section is used to make sure the offset of the following section will be respect
/// a specific alignment.
/// </summary>
public sealed class ElfAlignedShadowSection : ElfShadowSection
{
    public ElfAlignedShadowSection() : this(0x1000)
    {
    }

    public ElfAlignedShadowSection(uint upperAlignment)
    {
        UpperAlignment = upperAlignment;
    }

    /// <summary>
    /// Gets or sets teh alignment requirement that this section will ensure for the
    /// following sections placed after this section, so that the offset of the following
    /// section is respecting the alignment.
    /// </summary>
    public uint UpperAlignment { get; set; }

    protected override void UpdateLayoutCore(ElfVisitorContext context)
    {
        var nextSectionOffset = AlignHelper.AlignUp(Position, UpperAlignment);
        Size = nextSectionOffset - Position;
        if (Size >= int.MaxValue)
        {
            context.Diagnostics.Error(DiagnosticId.ELF_ERR_InvalidAlignmentOutOfRange, $"Invalid alignment 0x{UpperAlignment:x} resulting in an offset beyond int.MaxValue");
        }
    }

    public override void Read(ElfReader reader)
    {
        throw new NotSupportedException($"An {nameof(ElfAlignedShadowSection)} does not support read and is only used for writing");
    }

    public override void Write(ElfWriter writer)
    {
        if (Size == 0) return;

        var sharedBuffer = ArrayPool<byte>.Shared.Rent((int)Size);
        Array.Clear(sharedBuffer, 0, sharedBuffer.Length);
        try
        {
            writer.Stream.Write(sharedBuffer, 0, (int) Size);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(sharedBuffer);
        }
    }
}