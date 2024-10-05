// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.IO;

namespace LibObjectFile.Elf;

/// <summary>
/// Equivalent of <see cref="ElfStreamSection"/> but used for shadow.
/// </summary>
public sealed class ElfStreamContentData : ElfContentData
{
    private Stream _stream;

    public ElfStreamContentData() : this(new MemoryStream())
    {
    }

    public ElfStreamContentData(Stream stream)
    {
        _stream = stream;
        Size = (ulong)stream.Length;
    }

    internal ElfStreamContentData(bool unused)
    {
        _stream = Stream.Null;
    }

    public Stream Stream
    {
        get => _stream;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _stream = value;
            Size = (ulong)value.Length;
        }
    }

    public override void Read(ElfReader reader)
    {
        reader.Position = Position;
        Stream = reader.ReadAsStream(Size);
    }

    public override void Write(ElfWriter writer)
    {
        writer.Write(Stream);
    }

    protected override void UpdateLayoutCore(ElfVisitorContext context)
    {
        Size = (ulong)Stream.Length;
    }
}