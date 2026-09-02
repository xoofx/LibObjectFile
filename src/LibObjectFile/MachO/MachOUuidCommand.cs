// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Text;
using LibObjectFile.Diagnostics;
using LibObjectFile.MachO.Internal;

namespace LibObjectFile.MachO;

/// <summary>
/// The image identifier load command (<c>LC_UUID</c>).
/// </summary>
/// <remarks>
/// The identifier is what pairs a binary with its separate debug companion, so a debugger will
/// refuse a dSYM whose value does not match.
/// </remarks>
public sealed class MachOUuidCommand : MachOLoadCommand
{
    /// <summary>
    /// The size of this command, which is fixed.
    /// </summary>
    public const uint CommandSize = 24;

    /// <summary>
    /// Gets or sets the identifier of this image.
    /// </summary>
    /// <remarks>
    /// The 16 bytes are stored in the order they appear in the file, which is the big-endian
    /// order <see cref="Guid(ReadOnlySpan{byte}, bool)"/> reads with <c>bigEndian: true</c>.
    /// Reading them the other way round would reverse the first three fields and print an
    /// identifier that no other tool agrees with.
    /// </remarks>
    public Guid Uuid { get; set; }

    /// <inheritdoc />
    public override uint MinimumCommandSize => CommandSize;

    /// <inheritdoc />
    protected override void UpdateLayoutCore(MachOVisitorContext context) => Size = CommandSize;

    /// <inheritdoc />
    public override unsafe void Read(MachOReader reader)
    {
        if (!reader.TryReadData(sizeof(RawUuidCommand), out RawUuidCommand raw))
        {
            reader.Diagnostics.Error(DiagnosticId.MACHO_ERR_TruncatedLoadCommand, $"Truncated LC_UUID at 0x{Position:X}");
            return;
        }

        Uuid = new Guid(new ReadOnlySpan<byte>(raw.Uuid, 16), bigEndian: true);
    }

    /// <inheritdoc />
    public override unsafe void Write(MachOWriter writer)
    {
        var raw = new RawUuidCommand
        {
            Cmd = (uint)Type,
            CmdSize = (uint)Size,
        };
        Uuid.TryWriteBytes(new Span<byte>(raw.Uuid, 16), bigEndian: true, out _);
        writer.Write(raw);
    }

    /// <inheritdoc />
    protected override bool PrintMembers(StringBuilder builder)
    {
        builder.Append($"Uuid = {Uuid:D}");
        return true;
    }
}
