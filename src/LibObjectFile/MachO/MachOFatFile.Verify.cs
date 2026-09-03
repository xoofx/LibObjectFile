// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using LibObjectFile.Diagnostics;

namespace LibObjectFile.MachO;

partial class MachOFatFile
{
    /// <summary>
    /// Checks this universal binary for inconsistencies.
    /// </summary>
    /// <returns>What was found. Empty if nothing was.</returns>
    public DiagnosticBag Verify()
    {
        var diagnostics = new DiagnosticBag();
        Verify(diagnostics);
        return diagnostics;
    }

    /// <summary>
    /// Checks this universal binary for inconsistencies, including each slice's own image.
    /// </summary>
    /// <param name="diagnostics">Receives what was found.</param>
    /// <exception cref="ArgumentNullException"><paramref name="diagnostics"/> is null.</exception>
    public void Verify(DiagnosticBag diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);

        var entrySize = (ulong)GetSliceEntrySize(Is64BitOffsets);
        var tableEnd = (ulong)HeaderSize + (ulong)Slices.Count * entrySize;

        var architectures = new HashSet<(MachOCpuType, uint)>();
        var ranges = new List<(ulong Start, ulong End, int Index)>();

        for (var i = 0; i < Slices.Count; i++)
        {
            var slice = Slices[i];

            if (slice.File is null)
            {
                diagnostics.Error(
                    DiagnosticId.MACHO_ERR_MissingFatSliceImage,
                    $"Slice {i} ({slice.CpuType}) has no image to write");
                continue;
            }

            if (slice.AlignLog2 > MachOFatSlice.MaxAlignLog2)
            {
                diagnostics.Error(
                    DiagnosticId.MACHO_ERR_InvalidFatSliceAlignment,
                    $"Slice {i} ({slice.CpuType}) has an alignment exponent of {slice.AlignLog2}, past the {MachOFatSlice.MaxAlignLog2} a universal binary allows");
            }
            else if ((slice.FileOffset & (slice.Alignment - 1)) != 0)
            {
                // A slice is mapped straight out of the containing file, so an offset off its own
                // page boundary is one the loader cannot map.
                diagnostics.Error(
                    DiagnosticId.MACHO_ERR_InvalidFatSliceAlignment,
                    $"Slice {i} ({slice.CpuType}) starts at 0x{slice.FileOffset:X}, which is not a multiple of its 0x{slice.Alignment:X} alignment");
            }

            if (slice.FileOffset < tableEnd)
            {
                diagnostics.Error(
                    DiagnosticId.MACHO_ERR_OverlappingFatSlices,
                    $"Slice {i} ({slice.CpuType}) starts at 0x{slice.FileOffset:X}, inside the slice table that ends at 0x{tableEnd:X}");
            }

            // The 32-bit table cannot express these, and casting on the way out would record a
            // different slice rather than fail.
            if (!Is64BitOffsets && (slice.FileOffset > uint.MaxValue || slice.Size > uint.MaxValue))
            {
                diagnostics.Error(
                    DiagnosticId.MACHO_ERR_ValueTooLargeFor32Bit,
                    $"Slice {i} ({slice.CpuType}) spans [0x{slice.FileOffset:X}, 0x{slice.FileOffset + slice.Size:X}) which a 32-bit slice table cannot record. Set {nameof(Is64BitOffsets)}.");
            }

            if (!architectures.Add((slice.CpuType, slice.CpuSubType)))
            {
                // The loader takes the first slice matching the host, so a duplicate is either
                // dead weight or a different image than the one that will be run.
                diagnostics.Error(
                    DiagnosticId.MACHO_ERR_DuplicateFatSlice,
                    $"Slice {i} repeats the architecture {slice.CpuType} (subtype 0x{slice.CpuSubType:X}) of an earlier slice");
            }

            if (slice.CpuType != slice.File.CpuType || slice.CpuSubType != slice.File.CpuSubType)
            {
                diagnostics.Error(
                    DiagnosticId.MACHO_ERR_FatSliceArchitectureMismatch,
                    $"Slice {i} is recorded as {slice.CpuType} (subtype 0x{slice.CpuSubType:X}) but contains {slice.File.CpuType} (subtype 0x{slice.File.CpuSubType:X})");
            }

            ranges.Add((slice.FileOffset, slice.FileOffset + slice.Size, i));
            slice.File.Verify(diagnostics);
        }

        // The header may list slices in any order, so overlap is checked in file order.
        ranges.Sort((a, b) => a.Start.CompareTo(b.Start));
        for (var i = 1; i < ranges.Count; i++)
        {
            if (ranges[i].Start < ranges[i - 1].End)
            {
                diagnostics.Error(
                    DiagnosticId.MACHO_ERR_OverlappingFatSlices,
                    $"Slice {ranges[i].Index} starts at 0x{ranges[i].Start:X}, inside slice {ranges[i - 1].Index} which ends at 0x{ranges[i - 1].End:X}");
            }
        }
    }
}
