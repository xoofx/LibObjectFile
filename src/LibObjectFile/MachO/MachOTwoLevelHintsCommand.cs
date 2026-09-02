// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Text;
using LibObjectFile.Diagnostics;
using LibObjectFile.MachO.Internal;

namespace LibObjectFile.MachO;

/// <summary>
/// The two-level namespace hint table load command (<c>LC_TWOLEVEL_HINTS</c>).
/// </summary>
/// <remarks>
/// The table lets the loader skip searching for a symbol it already knows the library and index
/// of. It is a relic of the prebinding era and current linkers no longer emit it, but images
/// from that era still carry one, and it records a file offset that has to move with the table.
/// </remarks>
public sealed class MachOTwoLevelHintsCommand : MachOLoadCommand
{
    /// <summary>
    /// The size of this command, which is fixed.
    /// </summary>
    public const uint CommandSize = 16;

    /// <summary>The size of one hint entry (<c>twolevel_hint</c>).</summary>
    public const uint HintSize = 4;

    /// <summary>
    /// Gets or sets the file offset of the hint table.
    /// </summary>
    public uint Offset { get; set; }

    /// <summary>
    /// Gets or sets the number of hints, each a 4-byte entry.
    /// </summary>
    public uint HintCount { get; set; }

    /// <inheritdoc />
    public override uint MinimumCommandSize => CommandSize;

    /// <inheritdoc />
    protected override void UpdateLayoutCore(MachOVisitorContext context) => Size = CommandSize;

    /// <inheritdoc />
    public override void UpdateFileOffsets(Func<uint, uint> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);
        if (Offset != 0) Offset = mapper(Offset);
    }

    /// <inheritdoc />
    public override unsafe void Read(MachOReader reader)
    {
        if (!reader.TryReadData(sizeof(RawTwoLevelHintsCommand), out RawTwoLevelHintsCommand raw))
        {
            reader.Diagnostics.Error(DiagnosticId.MACHO_ERR_TruncatedLoadCommand, $"Truncated LC_TWOLEVEL_HINTS at 0x{Position:X}");
            return;
        }

        Offset = raw.Offset;
        HintCount = raw.HintCount;
    }

    /// <inheritdoc />
    public override void Write(MachOWriter writer)
        => writer.Write(new RawTwoLevelHintsCommand
        {
            Cmd = (uint)Type,
            CmdSize = (uint)Size,
            Offset = Offset,
            HintCount = HintCount,
        });

    /// <inheritdoc />
    protected override bool PrintMembers(StringBuilder builder)
    {
        builder.Append($"Hints = {HintCount} at 0x{Offset:X}");
        return true;
    }
}
