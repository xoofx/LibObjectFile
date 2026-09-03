// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Text;
using LibObjectFile.Diagnostics;
using LibObjectFile.MachO.Internal;

namespace LibObjectFile.MachO;

/// <summary>
/// A load command locating a blob inside <c>__LINKEDIT</c>, covering <c>LC_CODE_SIGNATURE</c>,
/// <c>LC_FUNCTION_STARTS</c>, <c>LC_DATA_IN_CODE</c>, <c>LC_DYLD_EXPORTS_TRIE</c>,
/// <c>LC_DYLD_CHAINED_FIXUPS</c>, <c>LC_SEGMENT_SPLIT_INFO</c>, <c>LC_DYLIB_CODE_SIGN_DRS</c>
/// and <c>LC_LINKER_OPTIMIZATION_HINT</c>.
/// </summary>
/// <remarks>
/// The command only records where the blob is; the bytes themselves live in the
/// <c>__LINKEDIT</c> segment's content and are not interpreted here.
/// </remarks>
public sealed class MachOLinkEditDataCommand : MachOLoadCommand
{
    /// <summary>
    /// The size of this command, which is fixed.
    /// </summary>
    public const uint CommandSize = 16;

    /// <summary>
    /// Gets or sets the file offset of the blob.
    /// </summary>
    public uint DataOffset { get; set; }

    /// <summary>
    /// Gets or sets the size in bytes of the blob.
    /// </summary>
    public uint DataSize { get; set; }

    /// <inheritdoc />
    public override uint MinimumCommandSize => CommandSize;

    /// <inheritdoc />
    protected override void UpdateLayoutCore(MachOVisitorContext context) => Size = CommandSize;

    /// <inheritdoc />
    public override void UpdateFileOffsets(Func<uint, uint> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);
        if (DataOffset != 0) DataOffset = mapper(DataOffset);
    }

    /// <inheritdoc />
    public override unsafe void Read(MachOReader reader)
    {
        if (!reader.TryReadData(sizeof(RawLinkEditDataCommand), out RawLinkEditDataCommand raw))
        {
            reader.Diagnostics.Error(DiagnosticId.MACHO_ERR_TruncatedLoadCommand, $"Truncated link edit data command at 0x{Position:X}");
            return;
        }

        DataOffset = raw.DataOffset;
        DataSize = raw.DataSize;
    }

    /// <inheritdoc />
    public override void Write(MachOWriter writer)
    {
        writer.Write(new RawLinkEditDataCommand
        {
            Cmd = (uint)Type,
            CmdSize = (uint)Size,
            DataOffset = DataOffset,
            DataSize = DataSize,
        });
    }

    /// <inheritdoc />
    protected override bool PrintMembers(StringBuilder builder)
    {
        builder.Append($"Type = {Type}, DataOffset = 0x{DataOffset:X}, DataSize = 0x{DataSize:X}");
        return true;
    }
}
