// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Text;
using LibObjectFile.Diagnostics;
using LibObjectFile.MachO.Internal;

namespace LibObjectFile.MachO;

/// <summary>
/// The symbol table load command (<c>LC_SYMTAB</c>), locating the symbol and string tables in
/// <c>__LINKEDIT</c>.
/// </summary>
/// <remarks>
/// <see cref="SymbolCount"/> counts entries rather than bytes, so the size of the table depends
/// on whether the image is 32- or 64-bit. <see cref="StringSize"/> is a byte count.
/// </remarks>
public sealed class MachOSymbolTableCommand : MachOLoadCommand
{
    /// <summary>
    /// The size of this command, which is fixed.
    /// </summary>
    public const uint CommandSize = 24;

    /// <summary>
    /// Gets or sets the file offset of the symbol table.
    /// </summary>
    public uint SymbolOffset { get; set; }

    /// <summary>
    /// Gets or sets the number of entries in the symbol table.
    /// </summary>
    public uint SymbolCount { get; set; }

    /// <summary>
    /// Gets or sets the file offset of the string table.
    /// </summary>
    public uint StringOffset { get; set; }

    /// <summary>
    /// Gets or sets the size in bytes of the string table.
    /// </summary>
    public uint StringSize { get; set; }

    /// <summary>
    /// Gets the size in bytes of one symbol table entry for the given image width.
    /// </summary>
    /// <param name="is64Bit">Whether the containing image is 64-bit.</param>
    /// <returns>16 for a 64-bit image, 12 for a 32-bit one.</returns>
    public static uint GetSymbolSize(bool is64Bit) => is64Bit ? 16u : 12u;

    /// <inheritdoc />
    protected override void UpdateLayoutCore(MachOVisitorContext context) => Size = CommandSize;

    /// <inheritdoc />
    public override void UpdateFileOffsets(Func<uint, uint> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);
        if (SymbolOffset != 0) SymbolOffset = mapper(SymbolOffset);
        if (StringOffset != 0) StringOffset = mapper(StringOffset);
    }

    /// <inheritdoc />
    public override unsafe void Read(MachOReader reader)
    {
        if (!reader.TryReadData(sizeof(RawSymtabCommand), out RawSymtabCommand raw))
        {
            reader.Diagnostics.Error(DiagnosticId.MACHO_ERR_TruncatedLoadCommand, $"Truncated LC_SYMTAB at 0x{Position:X}");
            return;
        }

        SymbolOffset = raw.SymbolOffset;
        SymbolCount = raw.SymbolCount;
        StringOffset = raw.StringOffset;
        StringSize = raw.StringSize;
    }

    /// <inheritdoc />
    public override void Write(MachOWriter writer)
    {
        writer.Write(new RawSymtabCommand
        {
            Cmd = (uint)Type,
            CmdSize = (uint)Size,
            SymbolOffset = SymbolOffset,
            SymbolCount = SymbolCount,
            StringOffset = StringOffset,
            StringSize = StringSize,
        });
    }

    /// <inheritdoc />
    protected override bool PrintMembers(StringBuilder builder)
    {
        builder.Append($"Symbols = {SymbolCount} at 0x{SymbolOffset:X}, Strings = 0x{StringSize:X} bytes at 0x{StringOffset:X}");
        return true;
    }
}
