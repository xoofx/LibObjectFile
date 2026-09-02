// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using LibObjectFile.Diagnostics;
using LibObjectFile.MachO.Internal;

namespace LibObjectFile.MachO;

partial class MachOFile
{
    /// <summary>
    /// Reads the symbol table.
    /// </summary>
    /// <returns>
    /// The symbols in table order, or an empty list if the image has no <c>LC_SYMTAB</c>. The
    /// order matters, because <c>LC_DYSYMTAB</c> describes runs of it by index.
    /// </returns>
    /// <remarks>
    /// This decodes a snapshot rather than returning a live view. Writing the file does not carry
    /// changes made to the returned symbols back into it, since resizing the table would move
    /// everything after it in <c>__LINKEDIT</c>.
    /// </remarks>
    /// <exception cref="ObjectFileException">The table lies outside the content of this image.</exception>
    public IReadOnlyList<MachOSymbol> ReadSymbolTable()
    {
        var command = LoadCommands.OfType<MachOSymbolTableCommand>().FirstOrDefault();
        if (command is null || command.SymbolCount == 0) return [];

        var entrySize = MachOSymbolTableCommand.GetSymbolSize(Is64Bit);
        var entries = ReadFileBytes(command.SymbolOffset, command.SymbolCount * entrySize, "symbol table");
        var strings = ReadFileBytes(command.StringOffset, command.StringSize, "string table");

        var symbols = new List<MachOSymbol>((int)command.SymbolCount);
        for (var i = 0; i < command.SymbolCount; i++)
        {
            var span = entries.AsSpan((int)(i * entrySize));
            var symbol = Is64Bit ? ReadSymbol64(span) : ReadSymbol32(span);
            symbol.Name = ReadString(strings, symbol.NameOffset);
            symbols.Add(symbol);
        }

        return symbols;
    }

    /// <summary>
    /// Reads the indirect symbol table, which is what the stub and symbol pointer sections index
    /// into through their <c>reserved1</c> field.
    /// </summary>
    /// <returns>The symbol table indices, or an empty list if the image has no indirect table.</returns>
    /// <remarks>
    /// Two values are not indices: <see cref="IndirectSymbolLocal"/> and
    /// <see cref="IndirectSymbolAbsolute"/> mark entries the loader has nothing to bind.
    /// </remarks>
    /// <exception cref="ObjectFileException">The table lies outside the content of this image.</exception>
    public IReadOnlyList<uint> ReadIndirectSymbolTable()
    {
        var command = LoadCommands.OfType<MachODynamicSymbolTableCommand>().FirstOrDefault();
        if (command is null || command.IndirectSymbolCount == 0) return [];

        var bytes = ReadFileBytes(command.IndirectSymbolOffset, command.IndirectSymbolCount * MachODynamicSymbolTableCommand.IndirectSymbolEntrySize, "indirect symbol table");
        var indices = new uint[command.IndirectSymbolCount];
        for (var i = 0; i < indices.Length; i++)
        {
            indices[i] = BitConverter.ToUInt32(bytes, i * (int)MachODynamicSymbolTableCommand.IndirectSymbolEntrySize);
        }

        return indices;
    }

    /// <summary>The entry is a local symbol the loader does not bind (<c>INDIRECT_SYMBOL_LOCAL</c>).</summary>
    public const uint IndirectSymbolLocal = 0x80000000;

    /// <summary>The entry is an absolute symbol the loader does not bind (<c>INDIRECT_SYMBOL_ABS</c>).</summary>
    public const uint IndirectSymbolAbsolute = 0x40000000;

    private static unsafe MachOSymbol ReadSymbol64(ReadOnlySpan<byte> span)
    {
        var raw = System.Runtime.InteropServices.MemoryMarshal.Read<RawNList64>(span);
        return new MachOSymbol
        {
            NameOffset = raw.StringIndex,
            RawType = raw.Type,
            SectionIndex = raw.SectionIndex,
            Description = raw.Description,
            Value = raw.Value,
        };
    }

    private static unsafe MachOSymbol ReadSymbol32(ReadOnlySpan<byte> span)
    {
        var raw = System.Runtime.InteropServices.MemoryMarshal.Read<RawNList32>(span);
        return new MachOSymbol
        {
            NameOffset = raw.StringIndex,
            RawType = raw.Type,
            SectionIndex = raw.SectionIndex,
            Description = raw.Description,
            Value = raw.Value,
        };
    }

    private static string ReadString(byte[] strings, uint offset)
    {
        if (offset >= strings.Length) return string.Empty;

        var span = strings.AsSpan((int)offset);
        var end = span.IndexOf((byte)0);
        if (end < 0) end = span.Length;
        return Encoding.UTF8.GetString(span.Slice(0, end));
    }

    /// <summary>
    /// Reads a run of bytes at a file offset out of whichever content covers it.
    /// </summary>
    private byte[] ReadFileBytes(uint offset, uint length, string what)
    {
        if (length == 0) return [];

        foreach (var content in Content)
        {
            if (content is not MachOStreamContent stream || offset < content.Position) continue;

            var start = offset - content.Position;
            if (start + length > (ulong)stream.Content.Length) continue;

            var buffer = new byte[length];
            stream.Content.Position = (long)start;
            stream.Content.ReadExactly(buffer);
            return buffer;
        }

        var diagnostics = new DiagnosticBag();
        diagnostics.Error(DiagnosticId.MACHO_ERR_DataOutsideImage, $"The {what} at 0x{offset:X} for 0x{length:X} bytes is not covered by any content of this image.");
        throw new ObjectFileException($"The {what} at 0x{offset:X} for 0x{length:X} bytes is not covered by any content of this image.", diagnostics);
    }
}
