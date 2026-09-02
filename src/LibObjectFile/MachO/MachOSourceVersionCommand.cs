// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Text;
using LibObjectFile.Diagnostics;
using LibObjectFile.MachO.Internal;

namespace LibObjectFile.MachO;

/// <summary>
/// The source version load command (<c>LC_SOURCE_VERSION</c>), recording the version of the
/// source the image was built from.
/// </summary>
public sealed class MachOSourceVersionCommand : MachOLoadCommand
{
    /// <summary>
    /// The size of this command, which is fixed.
    /// </summary>
    public const uint CommandSize = 16;

    /// <summary>
    /// Gets or sets the packed source version, which holds five parts in 24, 10, 10, 10 and 10 bits.
    /// </summary>
    public ulong SourceVersion { get; set; }

    /// <summary>
    /// Gets the first four components of the source version.
    /// </summary>
    public Version Version => MachOVersion.DecodeSource(SourceVersion);

    /// <inheritdoc />
    protected override void UpdateLayoutCore(MachOVisitorContext context) => Size = CommandSize;

    /// <inheritdoc />
    public override unsafe void Read(MachOReader reader)
    {
        if (!reader.TryReadData(sizeof(RawSourceVersionCommand), out RawSourceVersionCommand raw))
        {
            reader.Diagnostics.Error(DiagnosticId.MACHO_ERR_TruncatedLoadCommand, $"Truncated LC_SOURCE_VERSION at 0x{Position:X}");
            return;
        }

        SourceVersion = raw.Version;
    }

    /// <inheritdoc />
    public override void Write(MachOWriter writer)
        => writer.Write(new RawSourceVersionCommand
        {
            Cmd = (uint)Type,
            CmdSize = (uint)Size,
            Version = SourceVersion,
        });

    /// <inheritdoc />
    protected override bool PrintMembers(StringBuilder builder)
    {
        builder.Append($"Version = {Version}");
        return true;
    }
}
