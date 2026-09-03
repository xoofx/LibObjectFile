// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using LibObjectFile.Diagnostics;
using LibObjectFile.MachO.Internal;

namespace LibObjectFile.MachO;

/// <summary>
/// A load command carrying a single path, covering <c>LC_RPATH</c>, <c>LC_LOAD_DYLINKER</c>,
/// <c>LC_ID_DYLINKER</c> and <c>LC_DYLD_ENVIRONMENT</c>.
/// </summary>
public sealed class MachOPathCommand : MachOPathLoadCommand
{
    /// <summary>
    /// Gets or sets the path carried by this command. For <c>LC_RPATH</c> this is a directory
    /// that <c>@rpath</c> expands to, and for the dylinker commands it is the path of the loader.
    /// </summary>
    public string Path
    {
        get => Value;
        set => Value = value;
    }

    /// <inheritdoc />
    protected override unsafe uint FixedSize => (uint)sizeof(RawPathCommand);

    /// <inheritdoc />
    public override unsafe void Read(MachOReader reader)
    {
        var commandPosition = reader.Position;
        if (!reader.TryReadData(sizeof(RawPathCommand), out RawPathCommand raw))
        {
            reader.Diagnostics.Error(DiagnosticId.MACHO_ERR_TruncatedLoadCommand, $"Truncated path command at 0x{commandPosition:X}");
            return;
        }

        ReadValue(reader, commandPosition, raw.PathOffset);
    }

    /// <inheritdoc />
    public override unsafe void Write(MachOWriter writer)
    {
        writer.Write(new RawPathCommand
        {
            Cmd = (uint)Type,
            CmdSize = (uint)Size,
            PathOffset = (uint)sizeof(RawPathCommand),
        });
        WriteValue(writer);
    }
}
