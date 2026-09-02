// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Text;
using LibObjectFile.Diagnostics;
using LibObjectFile.MachO.Internal;

namespace LibObjectFile.MachO;

/// <summary>
/// The entry point load command (<c>LC_MAIN</c>).
/// </summary>
/// <remarks>
/// This replaced <c>LC_UNIXTHREAD</c>, which specified the entry point by handing dyld a whole
/// register state to restore. <see cref="EntryOffset"/> is a file offset rather than an address,
/// so it moves with the content it points at.
/// </remarks>
public sealed class MachOMainCommand : MachOLoadCommand
{
    /// <summary>
    /// The size of this command, which is fixed.
    /// </summary>
    public const uint CommandSize = 24;

    /// <summary>
    /// Gets or sets the file offset of the entry point.
    /// </summary>
    public ulong EntryOffset { get; set; }

    /// <summary>
    /// Gets or sets the initial stack size, or zero to let the system choose.
    /// </summary>
    public ulong StackSize { get; set; }

    /// <inheritdoc />
    protected override void UpdateLayoutCore(MachOVisitorContext context) => Size = CommandSize;

    /// <inheritdoc />
    public override unsafe void Read(MachOReader reader)
    {
        if (!reader.TryReadData(sizeof(RawEntryPointCommand), out RawEntryPointCommand raw))
        {
            reader.Diagnostics.Error(DiagnosticId.MACHO_ERR_TruncatedLoadCommand, $"Truncated LC_MAIN at 0x{Position:X}");
            return;
        }

        EntryOffset = raw.EntryOffset;
        StackSize = raw.StackSize;
    }

    /// <inheritdoc />
    public override void Write(MachOWriter writer)
        => writer.Write(new RawEntryPointCommand
        {
            Cmd = (uint)Type,
            CmdSize = (uint)Size,
            EntryOffset = EntryOffset,
            StackSize = StackSize,
        });

    /// <inheritdoc />
    protected override bool PrintMembers(StringBuilder builder)
    {
        builder.Append($"EntryOffset = 0x{EntryOffset:X}, StackSize = 0x{StackSize:X}");
        return true;
    }
}
