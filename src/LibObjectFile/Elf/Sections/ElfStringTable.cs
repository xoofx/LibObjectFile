// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LibObjectFile.Collections;
using LibObjectFile.IO;

namespace LibObjectFile.Elf;

/// <summary>
/// A string table section with the type <see cref="ElfSectionType.StringTable"/>.
/// </summary>
public class ElfStringTable : ElfSection
{
    private Stream _stream;
    private readonly Dictionary<string, uint> _mapStringToIndex;
    private readonly Dictionary<uint, string> _mapIndexToString;

    public const string DefaultName = ".strtab";

    public const int DefaultCapacity = 256;

    public ElfStringTable() : this(DefaultName, DefaultCapacity)
    {
    }

    public ElfStringTable(string name, int capacityInBytes = DefaultCapacity) : base(ElfSectionType.StringTable)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (capacityInBytes < 0) throw new ArgumentOutOfRangeException(nameof(capacityInBytes));

        _stream = new MemoryStream(capacityInBytes);
        _stream.WriteByte(0);

        _mapStringToIndex = new Dictionary<string, uint>(StringComparer.Ordinal)
        {
            [string.Empty] = 0
        };
        _mapIndexToString = new Dictionary<uint, string>
        {
            [0] = string.Empty
        };

        Name = name;
    }

    // Internal constructor used used when reading
    internal ElfStringTable(bool unused) : base(ElfSectionType.StringTable)
    {
        _stream = Stream.Null;
        _mapStringToIndex = new Dictionary<string, uint>
        {
            [string.Empty] = 0
        };
        _mapIndexToString = new Dictionary<uint, string>
        {
            [0] = string.Empty
        };
    }

    protected override void UpdateLayoutCore(ElfVisitorContext context)
    {
        Size = (ulong)_stream.Length;
    }

    public override void Read(ElfReader reader)
    {
        reader.Position = Position;
        _stream = reader.ReadAsStream(Size);
    }

    public override void Write(ElfWriter writer)
    {
        _stream.Position = 0;
        _stream.CopyTo(writer.Stream);
    }

    private ElfString Create(string? text)
    {
        // Same as empty string
        if (string.IsNullOrEmpty(text)) return default;

        if (!_mapStringToIndex.TryGetValue(text, out uint index))
        {
            index = CreateIndex(text);
        }

        return new(index);
    }

    public bool TryResolve(ElfString name, out ElfString resolvedName)
    {
        string text = name.Value;
        if (name.Index == 0)
        {
            if (string.IsNullOrEmpty(text))
            {
                resolvedName = name;
                return true;
            }

            resolvedName = Create(text);
            return true;
        }

        if (!TryGetString(name.Index, out text))
        {
            resolvedName = default;
            return false;
        }

        resolvedName = new(text, name.Index);
        return true;
    }

    public ElfString Resolve(ElfString name)
    {
        if (!TryResolve(name, out var newName))
        {
            throw new ArgumentException($"Invalid string index {name}");
        }

        return newName;
    }

    public bool TryGetString(uint index, out string text)
    {
        if (index == 0)
        {
            text = string.Empty;
            return true;
        }

        if (_mapIndexToString.TryGetValue(index, out text!))
        {
            return true;
        }

        if (index >= _stream.Length)
        {
            return false;
        }

        _stream.Position = index;
        text = _stream.ReadStringUTF8NullTerminated();
        _mapIndexToString.Add(index, text);

        // Don't try to override an existing mapping
        if (!_mapStringToIndex.TryGetValue(text, out var existingIndex))
        {
            _mapStringToIndex.Add(text, index);
        }

        return true;
    }

    private uint CreateIndex(string text)
    {
        uint index = (uint)_stream.Length;
        _mapIndexToString.Add(index, text);
        _mapStringToIndex.Add(text, index);

        var reservedBytes = Encoding.UTF8.GetByteCount(text) + 1;
        using var buffer = TempSpan<byte>.Create(reservedBytes, out var span);
        Encoding.UTF8.GetEncoder().GetBytes(text, span, true);
        span[reservedBytes - 1] = 0;
        if (_stream.Position != index)
        {
            _stream.Position = index;
        }
        _stream.Write(span);

        return index;
    }
}