// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Text;
using LibObjectFile.Diagnostics;
using LibObjectFile.MachO.Internal;

namespace LibObjectFile.MachO;

/// <summary>
/// The compressed dyld information load command (<c>LC_DYLD_INFO</c> or <c>LC_DYLD_INFO_ONLY</c>).
/// </summary>
/// <remarks>
/// Each offset and size pair locates an opcode stream in <c>__LINKEDIT</c> telling dyld how to
/// rebase and bind the image. The streams are not decoded here; they address their targets by
/// segment index and offset within that segment rather than by file offset, so moving them only
/// means updating the offsets recorded in this command.
/// <para>
/// The <c>ONLY</c> spelling means the image carries no classic relocations as a fallback, so a
/// loader that does not understand these streams cannot load it at all.
/// </para>
/// </remarks>
public sealed class MachODyldInfoCommand : MachOLoadCommand
{
    /// <summary>
    /// The size of this command, which is fixed.
    /// </summary>
    public const uint CommandSize = 48;

    /// <summary>Gets or sets the file offset of the rebase opcodes.</summary>
    public uint RebaseOffset { get; set; }

    /// <summary>Gets or sets the size in bytes of the rebase opcodes.</summary>
    public uint RebaseSize { get; set; }

    /// <summary>Gets or sets the file offset of the binding opcodes.</summary>
    public uint BindOffset { get; set; }

    /// <summary>Gets or sets the size in bytes of the binding opcodes.</summary>
    public uint BindSize { get; set; }

    /// <summary>Gets or sets the file offset of the weak binding opcodes.</summary>
    public uint WeakBindOffset { get; set; }

    /// <summary>Gets or sets the size in bytes of the weak binding opcodes.</summary>
    public uint WeakBindSize { get; set; }

    /// <summary>Gets or sets the file offset of the lazy binding opcodes.</summary>
    public uint LazyBindOffset { get; set; }

    /// <summary>Gets or sets the size in bytes of the lazy binding opcodes.</summary>
    public uint LazyBindSize { get; set; }

    /// <summary>Gets or sets the file offset of the export trie.</summary>
    public uint ExportOffset { get; set; }

    /// <summary>Gets or sets the size in bytes of the export trie.</summary>
    public uint ExportSize { get; set; }

    /// <summary>
    /// Gets a value indicating whether the image relies on these streams alone, with no classic
    /// relocations to fall back on.
    /// </summary>
    public bool IsOnly => Type == MachOLoadCommandType.DyldInfoOnly;

    /// <inheritdoc />
    protected override void UpdateLayoutCore(MachOVisitorContext context) => Size = CommandSize;

    /// <inheritdoc />
    public override void UpdateFileOffsets(Func<uint, uint> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);
        if (RebaseOffset != 0) RebaseOffset = mapper(RebaseOffset);
        if (BindOffset != 0) BindOffset = mapper(BindOffset);
        if (WeakBindOffset != 0) WeakBindOffset = mapper(WeakBindOffset);
        if (LazyBindOffset != 0) LazyBindOffset = mapper(LazyBindOffset);
        if (ExportOffset != 0) ExportOffset = mapper(ExportOffset);
    }

    /// <inheritdoc />
    public override unsafe void Read(MachOReader reader)
    {
        if (!reader.TryReadData(sizeof(RawDyldInfoCommand), out RawDyldInfoCommand raw))
        {
            reader.Diagnostics.Error(DiagnosticId.MACHO_ERR_TruncatedLoadCommand, $"Truncated {Type} at 0x{Position:X}");
            return;
        }

        RebaseOffset = raw.RebaseOffset;
        RebaseSize = raw.RebaseSize;
        BindOffset = raw.BindOffset;
        BindSize = raw.BindSize;
        WeakBindOffset = raw.WeakBindOffset;
        WeakBindSize = raw.WeakBindSize;
        LazyBindOffset = raw.LazyBindOffset;
        LazyBindSize = raw.LazyBindSize;
        ExportOffset = raw.ExportOffset;
        ExportSize = raw.ExportSize;
    }

    /// <inheritdoc />
    public override void Write(MachOWriter writer)
    {
        writer.Write(new RawDyldInfoCommand
        {
            Cmd = (uint)Type,
            CmdSize = (uint)Size,
            RebaseOffset = RebaseOffset,
            RebaseSize = RebaseSize,
            BindOffset = BindOffset,
            BindSize = BindSize,
            WeakBindOffset = WeakBindOffset,
            WeakBindSize = WeakBindSize,
            LazyBindOffset = LazyBindOffset,
            LazyBindSize = LazyBindSize,
            ExportOffset = ExportOffset,
            ExportSize = ExportSize,
        });
    }

    /// <inheritdoc />
    protected override bool PrintMembers(StringBuilder builder)
    {
        builder.Append($"Type = {Type}, Rebase = 0x{RebaseSize:X}, Bind = 0x{BindSize:X}, LazyBind = 0x{LazyBindSize:X}, Export = 0x{ExportSize:X}");
        return true;
    }
}
